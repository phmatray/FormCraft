namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the select field renderer.
/// </summary>
public class MudBlazorSelectFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorSelectFieldComponent<,>);
    
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        // This renderer handles fields with SelectOptions in AdditionalAttributes
        return field.AdditionalAttributes.ContainsKey("Options");
    }
}