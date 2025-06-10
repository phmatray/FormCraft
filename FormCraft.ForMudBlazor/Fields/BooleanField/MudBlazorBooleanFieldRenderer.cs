namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the boolean field renderer.
/// </summary>
public class MudBlazorBooleanFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorBooleanFieldComponent<>);
    
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(bool) || fieldType == typeof(bool?);
    }
}