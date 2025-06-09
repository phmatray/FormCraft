using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Defines the contract for UI framework adapters that provide framework-specific rendering logic.
/// </summary>
public interface IUIFrameworkAdapter
{
    /// <summary>
    /// Gets the name of the UI framework (e.g., "MudBlazor", "FluentUI", "Bootstrap", "Tailwind").
    /// </summary>
    string FrameworkName { get; }
    
    /// <summary>
    /// Gets the text field renderer for this UI framework.
    /// </summary>
    IFieldRenderer TextFieldRenderer { get; }
    
    /// <summary>
    /// Gets the numeric field renderer for this UI framework.
    /// </summary>
    IFieldRenderer NumericFieldRenderer { get; }
    
    /// <summary>
    /// Gets the boolean field renderer for this UI framework.
    /// </summary>
    IFieldRenderer BooleanFieldRenderer { get; }
    
    /// <summary>
    /// Gets the date/time field renderer for this UI framework.
    /// </summary>
    IFieldRenderer DateTimeFieldRenderer { get; }
    
    /// <summary>
    /// Gets the select field renderer for this UI framework.
    /// </summary>
    IFieldRenderer SelectFieldRenderer { get; }
    
    /// <summary>
    /// Gets the file upload field renderer for this UI framework.
    /// </summary>
    IFieldRenderer FileUploadFieldRenderer { get; }
    
    /// <summary>
    /// Renders a form container with the specified content.
    /// </summary>
    RenderFragment RenderForm(RenderFragment content, string? cssClass = null);
    
    /// <summary>
    /// Renders a field group container.
    /// </summary>
    RenderFragment RenderFieldGroup(string? title, RenderFragment content, string? cssClass = null);
    
    /// <summary>
    /// Renders a submit button.
    /// </summary>
    RenderFragment RenderSubmitButton(string text, bool isSubmitting, EventCallback onClick, string? cssClass = null);
    
    /// <summary>
    /// Renders validation messages for a field.
    /// </summary>
    RenderFragment RenderValidationMessages(IEnumerable<string> messages);
}