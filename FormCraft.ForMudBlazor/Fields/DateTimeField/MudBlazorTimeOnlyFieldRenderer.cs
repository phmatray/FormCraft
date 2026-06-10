namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the time-only field renderer.
/// </summary>
public class MudBlazorTimeOnlyFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorTimeOnlyFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        return underlyingType == typeof(TimeOnly);
    }
}
