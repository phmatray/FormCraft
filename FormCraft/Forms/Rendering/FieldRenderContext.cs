using Microsoft.AspNetCore.Components;

namespace FormCraft;

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

    /// <inheritdoc cref="IFieldRenderContext.ActualFieldType"/>
    public Type ActualFieldType { get; init; } = default!;

    /// <inheritdoc cref="IFieldRenderContext.CurrentValue"/>
    public object? CurrentValue { get; init; }

    /// <inheritdoc cref="IFieldRenderContext.OnValueChanged"/>
    public EventCallback<object?> OnValueChanged { get; init; }

    /// <inheritdoc />
    public EventCallback OnDependencyChanged { get; init; }

    // Explicit interface implementations for non-generic interface
    object IFieldRenderContext.Model => Model!;
    object IFieldRenderContext.FieldConfiguration => Field;
    EventCallback IFieldRenderContext.OnFieldChanged => OnDependencyChanged;
}