using Microsoft.AspNetCore.Components;
using FormCraft.Forms.Core;

namespace FormCraft.Forms.Abstractions;

/// <summary>
/// Provides field rendering services by coordinating multiple field renderers to generate appropriate UI components.
/// </summary>
public interface IFieldRendererService
{
    /// <summary>
    /// Renders a form field by selecting the appropriate field renderer and generating the UI component.
    /// </summary>
    /// <typeparam name="TModel">The model type that contains the field.</typeparam>
    /// <param name="model">The model instance containing the field value.</param>
    /// <param name="field">The field configuration with metadata and validation rules.</param>
    /// <param name="onValueChanged">Callback to invoke when the field value changes.</param>
    /// <param name="onDependencyChanged">Callback to invoke when field dependencies change.</param>
    /// <returns>A RenderFragment that generates the appropriate UI component for the field.</returns>
    RenderFragment RenderField<TModel>(TModel model, IFieldConfiguration<TModel, object> field, 
        EventCallback<object?> onValueChanged, EventCallback onDependencyChanged);
}