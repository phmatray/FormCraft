namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the <see cref="DateOnly"/> field renderer.
/// </summary>
public class FluentUIDateOnlyFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUIDateOnlyFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        return underlyingType == typeof(DateOnly);
    }
}
