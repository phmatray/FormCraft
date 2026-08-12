namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the boolean field renderer.
/// </summary>
public class FluentUIBooleanFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUIBooleanFieldComponent<>);

    /// <inheritdoc />
    /// <remarks>
    /// <c>bool?</c> is accepted and rendered as an ordinary two-state control, with null shown as
    /// unchecked - the same treatment the MudBlazor adapter gives it. Fluent's three-state checkbox
    /// would represent null honestly, but adopting it here would make the two adapters disagree
    /// about what the same configuration means.
    /// </remarks>
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => fieldType == typeof(bool) || fieldType == typeof(bool?);
}
