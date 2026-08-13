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
        // DateOnly/TimeOnly have dedicated renderers; this component binds DateTime.
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        return underlyingType == typeof(DateTime);
    }
}
