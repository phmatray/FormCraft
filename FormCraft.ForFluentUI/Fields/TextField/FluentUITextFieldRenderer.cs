namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the text field renderer.
/// </summary>
public class FluentUITextFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUITextFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => fieldType == typeof(string);
}
