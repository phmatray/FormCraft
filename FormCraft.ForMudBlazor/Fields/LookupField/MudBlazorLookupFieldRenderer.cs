namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the lookup table field renderer.
/// </summary>
public class MudBlazorLookupFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorLookupFieldComponent<,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        // This renderer handles fields with LookupDataProvider in AdditionalAttributes
        return field.AdditionalAttributes.ContainsKey("LookupDataProvider");
    }
}
