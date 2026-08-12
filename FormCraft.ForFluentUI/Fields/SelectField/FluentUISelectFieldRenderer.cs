namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the select field renderer.
/// </summary>
/// <remarks>
/// Stub: registered so the DI ordering is fixed from the start, but declines every field until its
/// component lands. See #260 Task 7.
/// </remarks>
public class FluentUISelectFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUITextFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field) => false;
}
