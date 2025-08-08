using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Defines the configuration contract for individual form fields, including display properties, validation rules, and behavior.
/// This interface provides comprehensive field metadata that controls how fields are rendered and validated.
/// </summary>
/// <typeparam name="TModel">The model type that contains this field.</typeparam>
/// <typeparam name="TValue">The type of the field's value.</typeparam>
/// <example>
/// <code>
/// // Creating a custom field configuration
/// public class MyFieldConfiguration : IFieldConfiguration&lt;MyModel, string&gt;
/// {
///     public string FieldName => "Username";
///     public Expression&lt;Func&lt;MyModel, string&gt;&gt; ValueExpression => x => x.Username;
///     public string Label { get; set; } = "Username";
///     public bool IsRequired { get; set; } = true;
///     // ... other properties
/// }
/// </code>
/// </example>
public interface IFieldConfiguration<TModel, TValue>
{
    /// <summary>
    /// Gets the name of the property on the model that this field represents.
    /// This corresponds to the property name extracted from the ValueExpression.
    /// </summary>
    string FieldName { get; }

    /// <summary>
    /// Gets the lambda expression that identifies which property on the model this field binds to.
    /// This expression is used for model binding, validation, and value retrieval.
    /// </summary>
    Expression<Func<TModel, TValue>> ValueExpression { get; }

    /// <summary>
    /// Gets or sets the display label for the field that appears in the UI.
    /// This is typically shown above or beside the input control.
    /// </summary>
    string? Label { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text displayed inside empty input fields to guide user input.
    /// </summary>
    string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets optional help text that provides additional guidance about the field.
    /// This is typically displayed below the input control or as a tooltip.
    /// </summary>
    string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes to apply to the field container for custom styling.
    /// </summary>
    string? CssClass { get; set; }

    /// <summary>
    /// Gets or sets whether this field is required and must have a value for the form to be valid.
    /// Required fields typically show visual indicators and validation messages when empty.
    /// </summary>
    bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether this field is visible in the form.
    /// Hidden fields are not rendered but may still participate in validation and model binding.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets whether this field is disabled and cannot be edited by the user.
    /// Disabled fields are typically rendered with a grayed-out appearance.
    /// </summary>
    bool IsDisabled { get; set; }

    /// <summary>
    /// Gets or sets whether this field is read-only and displays its value without allowing changes.
    /// Read-only fields can be focused but not modified.
    /// </summary>
    bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the display order of this field relative to other fields in the form.
    /// Fields are typically rendered in ascending order of this value.
    /// </summary>
    int Order { get; set; }

    /// <summary>
    /// Gets a dictionary of additional attributes that can be passed to the rendered field component.
    /// This allows for field-specific configuration beyond the standard properties.
    /// </summary>
    /// <example>
    /// <code>
    /// // Adding custom attributes for a slider
    /// fieldConfig.AdditionalAttributes["Min"] = 0;
    /// fieldConfig.AdditionalAttributes["Max"] = 100;
    /// fieldConfig.AdditionalAttributes["Step"] = 5;
    /// </code>
    /// </example>
    Dictionary<string, object> AdditionalAttributes { get; }

    /// <summary>
    /// Gets or sets the HTML5 input type for the field (e.g., "text", "email", "tel", "number", "date").
    /// This property is used to specify the type attribute of HTML input elements.
    /// </summary>
    string? InputType { get; set; }

    /// <summary>
    /// Gets the list of validators that will be applied to this field's value.
    /// Validators are executed in the order they appear in this list.
    /// </summary>
    List<IFieldValidator<TModel, TValue>> Validators { get; }

    /// <summary>
    /// Gets the list of dependencies that define how this field reacts to changes in other fields.
    /// These dependencies enable conditional behavior and field interactions.
    /// </summary>
    List<IFieldDependency<TModel>> Dependencies { get; }

    /// <summary>
    /// Gets or sets a function that determines whether this field should be visible based on the current model state.
    /// When specified, this takes precedence over the IsVisible property.
    /// </summary>
    /// <example>
    /// <code>
    /// // Show spouse name field only if user is married
    /// fieldConfig.VisibilityCondition = model => model.IsMarried;
    /// </code>
    /// </example>
    Func<TModel, bool>? VisibilityCondition { get; set; }

    /// <summary>
    /// Gets or sets a function that determines whether this field should be disabled based on the current model state.
    /// When specified, this takes precedence over the IsDisabled property.
    /// </summary>
    /// <example>
    /// <code>
    /// // Disable field based on user role
    /// fieldConfig.DisabledCondition = model => !model.User.IsAdmin;
    /// </code>
    /// </example>
    Func<TModel, bool>? DisabledCondition { get; set; }

    /// <summary>
    /// Gets or sets a custom Razor template for rendering this field.
    /// When specified, this template is used instead of the default field renderer.
    /// The template receives an IFieldContext with all necessary data for rendering.
    /// </summary>
    /// <example>
    /// <code>
    /// fieldConfig.CustomTemplate = context => builder =>
    /// {
    ///     builder.OpenElement(0, "div");
    ///     builder.AddAttribute(1, "class", "custom-field");
    ///     builder.AddContent(2, context.Configuration.Label);
    ///     builder.CloseElement();
    /// };
    /// </code>
    /// </example>
    RenderFragment<IFieldContext<TModel, TValue>>? CustomTemplate { get; set; }

    /// <summary>
    /// Gets or sets the custom field renderer type for this field.
    /// When specified, an instance of this renderer type is used instead of the default field renderer.
    /// The type must implement ICustomFieldRenderer&lt;TValue&gt;.
    /// </summary>
    /// <example>
    /// <code>
    /// // Using a custom color picker renderer
    /// fieldConfig.CustomRendererType = typeof(ColorPickerRenderer);
    /// </code>
    /// </example>
    Type? CustomRendererType { get; set; }
}