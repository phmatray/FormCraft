using Microsoft.AspNetCore.Components;
using FormCraft.Forms.Core;

namespace FormCraft.Forms.Rendering;

/// <summary>
/// Defines the contract for field renderers that generate UI components for specific field types.
/// </summary>
/// <example>
/// <code>
/// public class CustomFieldRenderer : IFieldRenderer
/// {
///     public bool CanRender(Type fieldType, IFieldConfiguration&lt;object, object&gt; field)
///     {
///         return fieldType == typeof(string) && field.Attributes.ContainsKey("CustomInput");
///     }
///     
///     public RenderFragment Render&lt;TModel&gt;(IFieldRenderContext&lt;TModel&gt; context)
///     {
///         return builder =>
///         {
///             builder.OpenElement(0, "div");
///             builder.AddContent(1, "Custom field renderer for: " + context.Field.FieldName);
///             builder.CloseElement();
///         };
///     }
/// }
/// </code>
/// </example>
public interface IFieldRenderer
{
    /// <summary>
    /// Determines whether this renderer can handle the specified field type and configuration.
    /// </summary>
    /// <param name="fieldType">The .NET type of the field value.</param>
    /// <param name="field">The field configuration containing attributes and metadata.</param>
    /// <returns>True if this renderer can handle the field type, false otherwise.</returns>
    bool CanRender(Type fieldType, IFieldConfiguration<object, object> field);
    
    /// <summary>
    /// Generates the UI component for the field as a Blazor RenderFragment.
    /// </summary>
    /// <typeparam name="TModel">The model type that contains the field.</typeparam>
    /// <param name="context">The render context containing the field configuration, current value, and callbacks.</param>
    /// <returns>A RenderFragment that generates the appropriate UI component for the field.</returns>
    RenderFragment Render<TModel>(IFieldRenderContext<TModel> context);
}

/// <summary>
/// Provides context information needed for rendering a form field.
/// </summary>
/// <typeparam name="TModel">The model type that contains the field being rendered.</typeparam>
public interface IFieldRenderContext<TModel>
{
    /// <summary>
    /// Gets the complete model instance containing the field being rendered.
    /// </summary>
    TModel Model { get; }
    
    /// <summary>
    /// Gets the field configuration containing metadata, attributes, and validation rules.
    /// </summary>
    IFieldConfiguration<TModel, object> Field { get; }
    
    /// <summary>
    /// Gets the actual .NET type of the field value.
    /// </summary>
    Type ActualFieldType { get; }
    
    /// <summary>
    /// Gets the current value of the field from the model.
    /// </summary>
    object? CurrentValue { get; }
    
    /// <summary>
    /// Gets the callback to invoke when the field value changes.
    /// </summary>
    EventCallback<object?> OnValueChanged { get; }
    
    /// <summary>
    /// Gets the callback to invoke when field dependencies change (for conditional visibility).
    /// </summary>
    EventCallback OnDependencyChanged { get; }
}

/// <summary>
/// Default implementation of the field render context.
/// </summary>
/// <typeparam name="TModel">The model type that contains the field being rendered.</typeparam>
public class FieldRenderContext<TModel> : IFieldRenderContext<TModel>
{
    /// <inheritdoc />
    public TModel Model { get; init; } = default!;
    
    /// <inheritdoc />
    public IFieldConfiguration<TModel, object> Field { get; init; } = default!;
    
    /// <inheritdoc />
    public Type ActualFieldType { get; init; } = default!;
    
    /// <inheritdoc />
    public object? CurrentValue { get; init; }
    
    /// <inheritdoc />
    public EventCallback<object?> OnValueChanged { get; init; }
    
    /// <inheritdoc />
    public EventCallback OnDependencyChanged { get; init; }
}