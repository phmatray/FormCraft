namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the date/time field renderer.
/// </summary>
public class MudBlazorDateTimeFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorDateTimeFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        return underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateOnly) ||
               underlyingType == typeof(TimeOnly);
    }
}