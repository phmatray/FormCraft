namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the <see cref="DateTime"/> field renderer.
/// </summary>
public class FluentUIDateTimeFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUIDateTimeFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        return underlyingType == typeof(DateTime);
    }
}
