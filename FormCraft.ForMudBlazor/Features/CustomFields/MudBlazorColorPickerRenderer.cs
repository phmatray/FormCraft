namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the color picker field renderer.
/// </summary>
public class MudBlazorColorPickerRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorColorPickerComponent<>);
    
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(string) && 
               field.AdditionalAttributes.ContainsKey("color-picker");
    }
}