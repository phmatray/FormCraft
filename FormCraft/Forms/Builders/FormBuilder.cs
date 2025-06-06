using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Provides a fluent API for building dynamic forms with type safety and validation support.
/// </summary>
/// <typeparam name="TModel">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
/// <example>
/// <code>
/// var formConfig = FormBuilder&lt;ContactModel&gt;
///     .Create()
///     .AddField(x => x.FirstName)
///         .WithLabel("First Name")
///         .Required("First name is required")
///     .AddField(x => x.Email)
///         .WithEmailValidation()
///     .Build();
/// </code>
/// </example>
public class FormBuilder<TModel> where TModel : new()
{
    private readonly FormConfiguration<TModel> _configuration = new();
    private int _fieldOrder;
    
    /// <summary>
    /// Creates a new instance of the form builder.
    /// </summary>
    /// <returns>A new FormBuilder instance for the specified model type.</returns>
    public static FormBuilder<TModel> Create() => new();
    
    /// <summary>
    /// Adds a field to the form configuration using a strongly-typed expression.
    /// </summary>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <param name="expression">A lambda expression that identifies the property on the model (e.g., x => x.FirstName).</param>
    /// <returns>A FieldBuilder instance for configuring the field's properties and validation.</returns>
    /// <example>
    /// <code>
    /// builder.AddField(x => x.Email)
    ///     .WithLabel("Email Address")
    ///     .Required("Email is required")
    ///     .WithEmailValidation();
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> AddField<TValue>(Expression<Func<TModel, TValue>> expression)
    {
        var fieldConfig = new FieldConfiguration<TModel, TValue>(expression)
        {
            Order = _fieldOrder++
        };
        
        // Cast to object version for storage
        var objectConfig = new FieldConfigurationWrapper<TModel, TValue>(fieldConfig);
        _configuration.Fields.Add(objectConfig);
        
        return new FieldBuilder<TModel, TValue>(this, fieldConfig);
    }
    
    /// <summary>
    /// Sets the layout style for the entire form.
    /// </summary>
    /// <param name="layout">The form layout to use (Vertical, Horizontal, Inline, or Grid).</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.WithLayout(FormLayout.Horizontal);
    /// </code>
    /// </example>
    public FormBuilder<TModel> WithLayout(FormLayout layout)
    {
        _configuration.Layout = layout;
        return this;
    }
    
    /// <summary>
    /// Adds a CSS class to the form container element.
    /// </summary>
    /// <param name="cssClass">The CSS class name to add to the form.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.WithCssClass("my-custom-form");
    /// </code>
    /// </example>
    public FormBuilder<TModel> WithCssClass(string cssClass)
    {
        _configuration.CssClass = cssClass;
        return this;
    }
    
    /// <summary>
    /// Configures whether to display a validation summary showing all form errors.
    /// </summary>
    /// <param name="show">True to show the validation summary, false to hide it. Default is true.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.ShowValidationSummary(true);
    /// </code>
    /// </example>
    public FormBuilder<TModel> ShowValidationSummary(bool show = true)
    {
        _configuration.ShowValidationSummary = show;
        return this;
    }
    
    /// <summary>
    /// Configures whether to show indicators (like asterisks) next to required fields.
    /// </summary>
    /// <param name="show">True to show required indicators, false to hide them. Default is true.</param>
    /// <param name="indicator">The text/symbol to display for required fields. Default is "*".</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.ShowRequiredIndicator(true, "•");
    /// </code>
    /// </example>
    public FormBuilder<TModel> ShowRequiredIndicator(bool show = true, string indicator = "*")
    {
        _configuration.ShowRequiredIndicator = show;
        _configuration.RequiredIndicator = indicator;
        return this;
    }
    
    /// <summary>
    /// Builds and returns the final form configuration that can be used with DynamicFormComponent.
    /// </summary>
    /// <returns>An IFormConfiguration instance containing all configured fields and settings.</returns>
    /// <example>
    /// <code>
    /// var config = builder.Build();
    /// </code>
    /// </example>
    public IFormConfiguration<TModel> Build() => _configuration;
    
    internal void AddFieldDependency(string fieldName, IFieldDependency<TModel> dependency)
    {
        if (!_configuration.FieldDependencies.ContainsKey(fieldName))
        {
            _configuration.FieldDependencies[fieldName] = new List<IFieldDependency<TModel>>();
        }
        
        _configuration.FieldDependencies[fieldName].Add(dependency);
    }
}

