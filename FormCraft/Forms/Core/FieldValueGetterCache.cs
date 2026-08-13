using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace FormCraft;

/// <summary>
/// Caches the compiled value getter of each field configuration, so reading a field's value emits IL
/// for its expression at most once rather than once per render (#269) and once per validation (#312).
/// </summary>
/// <typeparam name="TModel">The model the cached getters read from.</typeparam>
/// <remarks>
/// <para>
/// Keyed by configuration <b>instance</b>, which buys two properties: two configurations over the same
/// property never share an entry, and an entry lives no longer than the configuration it describes, so
/// nothing is held alive artificially.
/// </para>
/// <para>
/// What is cached is the <b>getter</b>, never the value it returns. The delegate takes the model as its
/// parameter, so every caller still reads the model afresh — caching a value here would freeze each
/// field at its first-read content, and would make validation judge a field the user has already
/// corrected.
/// </para>
/// <para>
/// This is deliberately a shared helper rather than a memo on <see cref="FieldConfigurationWrapper{TModel, TValue}" />.
/// Every call site — the renderer service and both validators — holds an
/// <see cref="IFieldConfiguration{TModel, TValue}" />, not the concrete wrapper, because
/// <c>IFormConfiguration.Fields</c> is a list of the interface. A property on the wrapper would
/// therefore be unreachable from all of them without either widening that public interface or a
/// type test against unknown generic arguments; keying off the instance costs neither.
/// </para>
/// <para>
/// The entry is never invalidated, so this assumes a configuration's <c>ValueExpression</c> keeps
/// reading the same member for that configuration's lifetime. That is the fluent builder's
/// immutable-after-<c>Build()</c> contract, and <see cref="FieldConfiguration{TModel, TValue}" />
/// enforces it by assigning the expression in its constructor. A caller that hands
/// <see cref="IFieldRendererService.RenderField" /> a configuration which re-targets its expression
/// after first use would keep seeing the original member; use a separate configuration instance per
/// binding instead.
/// </para>
/// <para>
/// <b>This is the only such cache — deliberately.</b> #269 introduced an equivalent one private to
/// <see cref="FieldRendererService" />; #312 retired it in favour of this one rather than leave two
/// mechanisms for one concept. Because both paths key off the same configuration instance, a field
/// that is rendered <i>and</i> validated now compiles its getter once in total, not once per path.
/// The alternative considered and rejected was widening
/// <see cref="IFieldConfiguration{TModel, TValue}" /> with a compiled-getter member: it would let each
/// configuration own its memo, but it is a public API change binding on every external implementer,
/// for no behaviour a per-instance key does not already provide.
/// </para>
/// </remarks>
internal static class FieldValueGetterCache<TModel>
{
    private static readonly ConditionalWeakTable<IFieldConfiguration<TModel, object>, Func<TModel, object>> Cache = new();

    /// <summary>
    /// Returns the field's compiled value getter, compiling it on first use and reusing it thereafter.
    /// </summary>
    /// <param name="field">The field configuration whose value expression to compile.</param>
    /// <returns>A delegate reading the field's value from a model instance.</returns>
    internal static Func<TModel, object> GetOrCompile(IFieldConfiguration<TModel, object> field)
        => Cache.GetValue(field, static configuration => configuration.ValueExpression.Compile());
}
