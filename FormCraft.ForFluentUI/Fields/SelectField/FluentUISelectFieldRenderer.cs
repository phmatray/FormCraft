namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the select field renderer.
/// </summary>
/// <remarks>
/// Selection is configuration-driven rather than type-driven: any field carrying options is a
/// select, whatever its CLR type. That is why <c>AddFormCraftFluentUI()</c> registers this renderer
/// ahead of the type-based ones - registered after the text renderer, a <c>string</c> field with
/// options would match text first and its options would silently never render.
/// </remarks>
public class FluentUISelectFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUISelectFieldComponent<,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => field.AdditionalAttributes.ContainsKey("Options");
}
