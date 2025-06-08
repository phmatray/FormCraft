using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Defines a custom renderer for form fields.
/// </summary>
public interface ICustomFieldRenderer
{
    /// <summary>
    /// Renders the custom field component.
    /// </summary>
    /// <param name="context">The field render context containing all necessary information.</param>
    /// <returns>A RenderFragment representing the custom field.</returns>
    RenderFragment Render(IFieldRenderContext context);

    /// <summary>
    /// Gets the type of value this renderer handles.
    /// </summary>
    Type ValueType { get; }
}

/// <summary>
/// Defines a typed custom renderer for form fields.
/// </summary>
/// <typeparam name="TValue">The type of the field value.</typeparam>
public interface ICustomFieldRenderer<TValue> : ICustomFieldRenderer
{
    /// <summary>
    /// Gets the type of value this renderer handles.
    /// </summary>
    new Type ValueType => typeof(TValue);
}

/// <summary>
/// Base class for custom field renderers with common functionality.
/// </summary>
/// <typeparam name="TValue">The type of the field value.</typeparam>
public abstract class CustomFieldRendererBase<TValue> : ICustomFieldRenderer<TValue>
{
    /// <inheritdoc cref="ICustomFieldRenderer.ValueType"/>
    public Type ValueType => typeof(TValue);

    /// <summary>
    /// Renders the custom field component.
    /// </summary>
    public abstract RenderFragment Render(IFieldRenderContext context);

    /// <summary>
    /// Gets the current value from the context.
    /// </summary>
    protected TValue? GetValue(IFieldRenderContext context)
    {
        return context.CurrentValue is TValue typedValue ? typedValue : default;
    }

    /// <summary>
    /// Invokes the value changed callback with the new value.
    /// </summary>
    protected async Task SetValue(IFieldRenderContext context, TValue? value)
    {
        await context.OnValueChanged.InvokeAsync(value);
    }
}