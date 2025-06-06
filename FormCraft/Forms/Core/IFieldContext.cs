using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// Provides context information for rendering custom field templates within forms.
/// This interface is used when creating custom field templates that need access to field state,
/// validation, and form context.
/// </summary>
/// <typeparam name="TModel">The model type that contains the field.</typeparam>
/// <typeparam name="TValue">The type of the field value.</typeparam>
/// <example>
/// <code>
/// // Using in a custom field template
/// RenderFragment&lt;IFieldContext&lt;MyModel, string&gt;&gt; customTemplate = context => builder =>
/// {
///     builder.OpenElement(0, "div");
///     builder.AddAttribute(1, "class", context.FieldCssClass);
///     
///     builder.OpenElement(2, "label");
///     builder.AddContent(3, context.Configuration.Label);
///     builder.CloseElement();
///     
///     builder.OpenElement(4, "input");
///     builder.AddAttribute(5, "type", "text");
///     builder.AddAttribute(6, "value", context.Value);
///     builder.AddAttribute(7, "onchange", EventCallback.Factory.Create&lt;ChangeEventArgs&gt;(this, 
///         e => context.ValueChanged.InvokeAsync((string)e.Value)));
///     builder.CloseElement();
///     
///     if (!context.IsValid)
///     {
///         builder.OpenElement(8, "div");
///         builder.AddAttribute(9, "class", "validation-message");
///         foreach (var message in context.ValidationMessages)
///         {
///             builder.AddContent(10, message);
///         }
///         builder.CloseElement();
///     }
///     
///     builder.CloseElement();
/// };
/// </code>
/// </example>
public interface IFieldContext<TModel, TValue>
{
    /// <summary>
    /// Gets the complete model instance containing the field.
    /// </summary>
    TModel Model { get; }
    
    /// <summary>
    /// Gets the field configuration containing metadata, attributes, and validation rules.
    /// </summary>
    IFieldConfiguration<TModel, TValue> Configuration { get; }
    
    /// <summary>
    /// Gets the Blazor EditContext for form validation and state management.
    /// </summary>
    EditContext EditContext { get; }
    
    /// <summary>
    /// Gets or sets the current value of the field.
    /// </summary>
    TValue Value { get; set; }
    
    /// <summary>
    /// Gets the callback to invoke when the field value changes.
    /// </summary>
    EventCallback<TValue> ValueChanged { get; }
    
    /// <summary>
    /// Gets the Blazor FieldIdentifier for this field, used for validation message association.
    /// </summary>
    FieldIdentifier FieldIdentifier { get; }
    
    /// <summary>
    /// Gets the current validation messages for this field.
    /// </summary>
    IEnumerable<string> ValidationMessages { get; }
    
    /// <summary>
    /// Gets a value indicating whether the field is currently valid (has no validation errors).
    /// </summary>
    bool IsValid { get; }
    
    /// <summary>
    /// Gets the CSS class string to apply to the field, including validation state classes.
    /// </summary>
    string FieldCssClass { get; }
}