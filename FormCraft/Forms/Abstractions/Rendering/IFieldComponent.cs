namespace FormCraft;

/// <summary>
/// Defines the contract for UI framework-agnostic field components.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
public interface IFieldComponent<TModel>
{
    /// <summary>
    /// Gets or sets the field render context containing all necessary information for rendering.
    /// </summary>
    IFieldRenderContext<TModel> Context { get; set; }
}