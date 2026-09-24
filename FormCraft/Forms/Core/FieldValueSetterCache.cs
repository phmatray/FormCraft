using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace FormCraft;

/// <summary>
/// Caches the compiled value setter of each field configuration, so writing a field's value through
/// a custom template's <c>ValueChanged</c> emits IL for its expression at most once rather than once
/// per write. Mirrors <see cref="FieldValueGetterCache{TModel}"/> — same cache shape, same
/// per-instance key — but assigns through the expression instead of reading it (#396).
/// </summary>
/// <typeparam name="TModel">The model the cached setters write to.</typeparam>
/// <remarks>
/// <para>
/// Keyed by configuration <b>instance</b>, exactly like <see cref="FieldValueGetterCache{TModel}"/>,
/// for the same reasons: two configurations over the same property never share an entry, and an
/// entry lives no longer than the configuration it describes.
/// </para>
/// <para>
/// <see cref="IFieldConfiguration{TModel, TValue}.ValueExpression"/> reads as
/// <c>Convert(model.Nested.Value, typeof(object))</c> once erased to <c>object</c> by
/// <see cref="FieldConfigurationWrapper{TModel, TValue}"/> — a shape that compiles fine as a
/// <em>read</em> (<see cref="FieldValueGetterCache{TModel}"/> just calls <c>.Compile()</c> on it),
/// but cannot be assigned to directly: a <c>Convert</c> node is not an lvalue.
/// <see cref="GetOrCompile"/> therefore unwraps a leading <c>Convert</c> to reach the real
/// member-access chain before building the assignment, and falls back to the body as-is for a
/// configuration whose <c>ValueExpression</c> was never wrapped this way.
/// </para>
/// <para>
/// The returned delegate also converts the incoming boxed value to the target member's type via
/// <see cref="Convert.ChangeType(object, Type)"/> before assigning — the same coercion both
/// adapters' <c>UpdateFieldValue</c> used to perform themselves against a <c>PropertyInfo</c> found
/// by <c>typeof(TModel).GetProperty(fieldName)</c>. That lookup only ever resolved a direct
/// top-level property (<c>fieldName</c> was, before #437, the expression's last member — #330's own root cause,
/// mirrored on the write side by #396), so moving the coercion here — sourced from the resolved
/// member's actual type instead of a name-based reflection lookup — is what makes it work for any
/// depth of member-access chain instead of only a top-level one, with identical behaviour for the
/// top-level case (the resolved type is the same either way).
/// </para>
/// <para>
/// A null intermediate in a nested path (e.g. <c>x =&gt; x.Nested.Value</c> when <c>Nested</c> is
/// <see langword="null"/>) throws <see cref="NullReferenceException"/> from the compiled assignment
/// itself, exactly like the read side. This cache does not catch it — reporting it is the caller's
/// job, the same way <see cref="FieldValueGetterCache{TModel}"/>'s own callers already do for reads.
/// </para>
/// </remarks>
public static class FieldValueSetterCache<TModel>
{
    private static readonly ConditionalWeakTable<IFieldConfiguration<TModel, object>, Action<TModel, object?>> Cache = new();

    /// <summary>
    /// Returns the field's compiled value setter, compiling it on first use and reusing it thereafter.
    /// </summary>
    /// <param name="field">The field configuration whose value expression to compile.</param>
    /// <returns>
    /// A delegate that assigns a value — converted to the target member's type — to
    /// <paramref name="field"/>'s bound member on a model instance.
    /// </returns>
    public static Action<TModel, object?> GetOrCompile(IFieldConfiguration<TModel, object> field)
        => Cache.GetValue(field, static configuration => Compile(configuration.ValueExpression));

    private static Action<TModel, object?> Compile(Expression<Func<TModel, object>> valueExpression)
    {
        var innerBody = valueExpression.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : valueExpression.Body;

        var valueParameter = Expression.Parameter(typeof(object), "value");
        var assign = Expression.Assign(innerBody, Expression.Convert(valueParameter, innerBody.Type));
        var rawSetter = Expression.Lambda<Action<TModel, object?>>(
            assign,
            valueExpression.Parameters[0],
            valueParameter).Compile();

        var targetType = Nullable.GetUnderlyingType(innerBody.Type) ?? innerBody.Type;

        return (model, value) =>
        {
            var convertedValue = value;
            if (value != null && value.GetType() != targetType)
            {
                try
                {
                    convertedValue = Convert.ChangeType(value, targetType);
                }
                catch
                {
                    // If conversion fails, use the value as-is - the assignment below surfaces the
                    // resulting mismatch itself, matching what property.SetValue used to do.
                }
            }

            rawSetter(model, convertedValue);
        };
    }
}
