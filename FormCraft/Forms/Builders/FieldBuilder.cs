using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Provides a fluent API for configuring individual form fields with validation, display properties, and behavior.
/// </summary>
/// <typeparam name="TModel">The model type that the form binds to.</typeparam>
/// <typeparam name="TValue">The type of the field value.</typeparam>
/// <example>
/// <code>
/// builder.AddField(x => x.Email, field => field
///     .WithLabel("Email Address")
///     .WithPlaceholder("Enter your email")
///     .Required("Email is required")
///     .WithValidator(email => email.Contains("@"), "Invalid email format"));
/// </code>
/// </example>
public class FieldBuilder<TModel, TValue> where TModel : new()
{
    private readonly FormBuilder<TModel> _formBuilder;
    private readonly FieldConfiguration<TModel, TValue> _fieldConfiguration;

    internal FieldBuilder(FormBuilder<TModel> formBuilder, FieldConfiguration<TModel, TValue> fieldConfiguration)
    {
        _formBuilder = formBuilder;
        _fieldConfiguration = fieldConfiguration;
    }

    /// <summary>
    /// Gets the underlying field configuration for advanced scenarios.
    /// </summary>
    internal FieldConfiguration<TModel, TValue> Configuration => _fieldConfiguration;

    /// <summary>
    /// Sets the display label for the field.
    /// </summary>
    /// <param name="label">The text to display as the field label.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithLabel("First Name")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithLabel(string label)
    {
        _fieldConfiguration.Label = label;
        return this;
    }

    /// <summary>
    /// Sets the placeholder text that appears in empty form fields.
    /// </summary>
    /// <param name="placeholder">The placeholder text to display.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithPlaceholder("Enter your email address")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithPlaceholder(string placeholder)
    {
        _fieldConfiguration.Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// Sets help text that provides additional guidance to users about the field.
    /// </summary>
    /// <param name="helpText">The help text to display below the field.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithHelpText("We'll never share your email with anyone else")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithHelpText(string helpText)
    {
        _fieldConfiguration.HelpText = helpText;
        return this;
    }

    /// <summary>
    /// Adds a CSS class to the field container for custom styling.
    /// </summary>
    /// <param name="cssClass">The CSS class name to add to the field.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithCssClass("highlighted-field")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithCssClass(string cssClass)
    {
        _fieldConfiguration.CssClass = cssClass;
        return this;
    }

    /// <summary>
    /// Makes the field required and adds validation to ensure it has a value.
    /// </summary>
    /// <param name="errorMessage">Custom error message to display when validation fails. If null, a default message is used.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .Required("Please enter your name")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> Required(string? errorMessage = null)
    {
        _fieldConfiguration.IsRequired = true;
        _fieldConfiguration.Validators.Add(new RequiredValidator<TModel, TValue>(errorMessage));
        return this;
    }

    /// <summary>
    /// Sets whether the field is disabled (non-interactive).
    /// </summary>
    /// <param name="disabled">True to disable the field, false to enable it. Default is true.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .Disabled(true)
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> Disabled(bool disabled = true)
    {
        _fieldConfiguration.IsDisabled = disabled;
        return this;
    }

    /// <summary>
    /// Sets whether the field is read-only (visible but not editable).
    /// </summary>
    /// <param name="readOnly">True to make the field read-only, false to make it editable. Default is true.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .ReadOnly(true)
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> ReadOnly(bool readOnly = true)
    {
        _fieldConfiguration.IsReadOnly = readOnly;
        return this;
    }

    /// <summary>
    /// Makes the field conditionally visible based on the model state.
    /// </summary>
    /// <param name="condition">A function that takes the model and returns true if the field should be visible.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .VisibleWhen(model => !string.IsNullOrEmpty(model.Country))
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> VisibleWhen(Func<TModel, bool> condition)
    {
        _fieldConfiguration.VisibilityCondition = condition;
        return this;
    }

    /// <summary>
    /// Makes the field conditionally disabled based on the model state.
    /// </summary>
    /// <param name="condition">A function that takes the model and returns true if the field should be disabled.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .DisabledWhen(model => model.IsReadOnlyMode)
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> DisabledWhen(Func<TModel, bool> condition)
    {
        _fieldConfiguration.DisabledCondition = condition;
        return this;
    }

    /// <summary>
    /// Adds a custom HTML attribute to the field element.
    /// </summary>
    /// <param name="name">The attribute name (e.g., "data-testid", "aria-label").</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithAttribute("data-testid", "email-field")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithAttribute(string name, object value)
    {
        _fieldConfiguration.AdditionalAttributes[name] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple custom HTML attributes to the field element.
    /// </summary>
    /// <param name="attributes">A dictionary of attribute names and values to add.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithAttributes(new Dictionary&lt;string, object&gt; 
    /// {
    ///     { "data-testid", "email-field" },
    ///     { "aria-label", "Email address input" }
    /// })
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithAttributes(Dictionary<string, object> attributes)
    {
        foreach (var attr in attributes)
        {
            _fieldConfiguration.AdditionalAttributes[attr.Key] = attr.Value;
        }
        return this;
    }

    /// <summary>
    /// Adds a custom validator that implements IFieldValidator to the field.
    /// </summary>
    /// <param name="validator">The validator instance to add.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithValidator(new EmailValidator())
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithValidator(IFieldValidator<TModel, TValue> validator)
    {
        _fieldConfiguration.Validators.Add(validator);
        return this;
    }

    /// <summary>
    /// Adds a custom validation function with an error message.
    /// </summary>
    /// <param name="validation">A function that takes the field value and returns true if valid, false if invalid.</param>
    /// <param name="errorMessage">The error message to display when validation fails.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithValidator(value => value.Length >= 8, "Password must be at least 8 characters")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithValidator(Func<TValue, bool> validation, string errorMessage)
    {
        _fieldConfiguration.Validators.Add(new CustomValidator<TModel, TValue>(validation, errorMessage));
        return this;
    }

    /// <summary>
    /// Adds an asynchronous validation function with an error message.
    /// </summary>
    /// <param name="validation">An async function that takes the field value and returns true if valid, false if invalid.</param>
    /// <param name="errorMessage">The error message to display when validation fails.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithAsyncValidator(async value => await CheckUsernameAvailability(value), "Username is already taken")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithAsyncValidator(Func<TValue, Task<bool>> validation, string errorMessage)
    {
        _fieldConfiguration.Validators.Add(new AsyncValidator<TModel, TValue>(validation, errorMessage));
        return this;
    }

    /// <summary>
    /// Sets the HTML5 input type for the field (e.g., "text", "email", "tel", "number", "date").
    /// </summary>
    /// <param name="inputType">The HTML5 input type to use for this field.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithInputType("email")
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithInputType(string inputType)
    {
        _fieldConfiguration.InputType = inputType;
        return this;
    }

    /// <summary>
    /// Creates a dependency on another field, executing an action when the dependency changes.
    /// </summary>
    /// <typeparam name="TDependsOn">The type of the field this field depends on.</typeparam>
    /// <param name="dependsOnExpression">An expression identifying the field this field depends on.</param>
    /// <param name="onChanged">An action to execute when the dependency field changes. Receives the model and the new value.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .DependsOn(x => x.Country, (model, country) => {
    ///     if (string.IsNullOrEmpty(country)) {
    ///         model.State = null;
    ///     }
    /// })
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> DependsOn<TDependsOn>(
        Expression<Func<TModel, TDependsOn>> dependsOnExpression,
        Action<TModel, TDependsOn> onChanged)
    {
        var dependency = new FieldDependency<TModel, TDependsOn>(dependsOnExpression, onChanged);
        _fieldConfiguration.Dependencies.Add(dependency);
        _formBuilder.AddFieldDependency(_fieldConfiguration.FieldName, dependency);
        return this;
    }

    /// <summary>
    /// Provides a custom Blazor template for rendering the field instead of the default renderer.
    /// </summary>
    /// <param name="template">A render fragment that defines how to display the field.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithCustomTemplate(context => 
    ///     @&lt;div class="custom-field"&gt;
    ///         &lt;label&gt;@context.Label&lt;/label&gt;
    ///         &lt;input @bind="context.Value" /&gt;
    ///     &lt;/div&gt;)
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithCustomTemplate(RenderFragment<IFieldContext<TModel, TValue>> template)
    {
        _fieldConfiguration.CustomTemplate = template;
        return this;
    }

    /// <summary>
    /// Sets the display order of the field within the form. Lower numbers appear first.
    /// </summary>
    /// <param name="order">The order value for this field.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithOrder(1) // This field will appear first
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithOrder(int order)
    {
        _fieldConfiguration.Order = order;
        return this;
    }
    
    /// <summary>
    /// Sets a custom field renderer for this field.
    /// </summary>
    /// <param name="renderer">The custom field renderer instance.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithCustomRenderer(new FileUploadFieldRenderer())
    /// </code>
    /// </example>
    public FieldBuilder<TModel, TValue> WithCustomRenderer(IFieldRenderer renderer)
    {
        _fieldConfiguration.CustomRendererType = renderer.GetType();
        _fieldConfiguration.AdditionalAttributes["CustomRendererInstance"] = renderer;
        return this;
    }

}