using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Provides context information needed for rendering a form field without model type.
/// </summary>
public interface IFieldRenderContext
{
    /// <summary>
    /// Gets the complete model instance containing the field being rendered.
    /// </summary>
    object Model { get; }

    /// <summary>
    /// Gets the field configuration containing metadata, attributes, and validation rules.
    /// </summary>
    object FieldConfiguration { get; }

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
    EventCallback OnFieldChanged { get; }
}

/// <summary>
/// Provides context information needed for rendering a form field.
/// </summary>
/// <typeparam name="TModel">The model type that contains the field being rendered.</typeparam>
public interface IFieldRenderContext<TModel> : IFieldRenderContext
{
    /// <summary>
    /// Gets the complete model instance containing the field being rendered.
    /// </summary>
    new TModel Model { get; }

    /// <summary>
    /// Gets the field configuration containing metadata, attributes, and validation rules.
    /// </summary>
    IFieldConfiguration<TModel, object> Field { get; }

    /// <summary>
    /// Gets the actual .NET type of the field value.
    /// </summary>
    new Type ActualFieldType { get; }

    /// <summary>
    /// Gets the current value of the field from the model.
    /// </summary>
    new object? CurrentValue { get; }

    /// <summary>
    /// Gets the callback to invoke when the field value changes.
    /// </summary>
    new EventCallback<object?> OnValueChanged { get; }

    /// <summary>
    /// Gets the callback to invoke when field dependencies change (for conditional visibility).
    /// </summary>
    EventCallback OnDependencyChanged { get; }
}