namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the text field renderer.
/// </summary>
public class MudBlazorTextFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorTextFieldComponent<>);
    
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field) => fieldType == typeof(string);
}