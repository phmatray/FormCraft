namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the <see cref="TimeOnly"/> field renderer.
/// </summary>
public class FluentUITimeOnlyFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUITimeOnlyFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        return underlyingType == typeof(TimeOnly);
    }
}
