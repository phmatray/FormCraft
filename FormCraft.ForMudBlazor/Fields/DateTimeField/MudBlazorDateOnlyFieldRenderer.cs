namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the date-only field renderer.
/// </summary>
public class MudBlazorDateOnlyFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorDateOnlyFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        return underlyingType == typeof(DateOnly);
    }
}
