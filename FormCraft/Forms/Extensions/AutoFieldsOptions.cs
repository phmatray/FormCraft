using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Options that control how <c>AddFieldsAuto()</c> generates fields from a model type.
/// Supports including/excluding properties and customizing individual generated fields.
/// </summary>
/// <typeparam name="TModel">The model type the form binds to.</typeparam>
/// <example>
/// <code>
/// FormBuilder&lt;CustomerModel&gt;.Create()
///     .AddFieldsAuto(options => options
///         .Exclude(x => x.InternalId)
///         .ConfigureField(x => x.Name, field => field
///             .WithLabel("Customer Name")
///             .Required()))
///     .Build();
/// </code>
/// </example>
public class AutoFieldsOptions<TModel> where TModel : new()
{
    internal HashSet<string> IncludedProperties { get; } = new(StringComparer.Ordinal);

    internal HashSet<string> ExcludedProperties { get; } = new(StringComparer.Ordinal);

    internal Dictionary<string, object> FieldConfigurators { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Restricts generation to the specified property names. When at least one
    /// property is included, all other properties are skipped.
    /// </summary>
    /// <param name="propertyNames">The names of the properties to generate fields for.</param>
    /// <returns>The options instance for method chaining.</returns>
    public AutoFieldsOptions<TModel> Include(params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            IncludedProperties.Add(name);
        }

        return this;
    }

    /// <summary>
    /// Restricts generation to the specified properties. When at least one
    /// property is included, all other properties are skipped.
    /// </summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">An expression identifying the property (e.g., x =&gt; x.Name).</param>
    /// <returns>The options instance for method chaining.</returns>
    public AutoFieldsOptions<TModel> Include<TValue>(Expression<Func<TModel, TValue>> property)
    {
        IncludedProperties.Add(GetPropertyName(property));
        return this;
    }

    /// <summary>
    /// Excludes the specified property names from generation.
    /// </summary>
    /// <param name="propertyNames">The names of the properties to skip.</param>
    /// <returns>The options instance for method chaining.</returns>
    public AutoFieldsOptions<TModel> Exclude(params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            ExcludedProperties.Add(name);
        }

        return this;
    }

    /// <summary>
    /// Excludes the specified property from generation.
    /// </summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">An expression identifying the property (e.g., x =&gt; x.InternalId).</param>
    /// <returns>The options instance for method chaining.</returns>
    public AutoFieldsOptions<TModel> Exclude<TValue>(Expression<Func<TModel, TValue>> property)
    {
        ExcludedProperties.Add(GetPropertyName(property));
        return this;
    }

    /// <summary>
    /// Registers a callback that customizes the generated field for the specified property.
    /// The callback runs after the automatic configuration (label, input type, validation),
    /// so it can override any generated setting.
    /// </summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="property">An expression identifying the property (e.g., x =&gt; x.Name).</param>
    /// <param name="configure">A callback that customizes the generated field.</param>
    /// <returns>The options instance for method chaining.</returns>
    public AutoFieldsOptions<TModel> ConfigureField<TValue>(
        Expression<Func<TModel, TValue>> property,
        Action<FieldBuilder<TModel, TValue>> configure)
    {
        FieldConfigurators[GetPropertyName(property)] = configure;
        return this;
    }

    /// <summary>
    /// Registers a callback that customizes the generated field for the specified property name.
    /// The <typeparamref name="TValue"/> type argument must match the property type exactly
    /// (including nullability), otherwise the callback is ignored.
    /// </summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="configure">A callback that customizes the generated field.</param>
    /// <returns>The options instance for method chaining.</returns>
    public AutoFieldsOptions<TModel> ConfigureField<TValue>(
        string propertyName,
        Action<FieldBuilder<TModel, TValue>> configure)
    {
        FieldConfigurators[propertyName] = configure;
        return this;
    }

    private static string GetPropertyName<TValue>(Expression<Func<TModel, TValue>> property)
    {
        var body = property.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary
            ? unary.Operand
            : property.Body;

        if (body is MemberExpression member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException(
            "The expression must reference a property on the model (e.g., x => x.Name).",
            nameof(property));
    }
}
