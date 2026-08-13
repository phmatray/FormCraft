namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the lookup field renderer.
/// </summary>
/// <remarks>
/// Matches the same <c>LookupDataProvider</c> attribute the MudBlazor adapter's renderer matches, so
/// a configuration built with either package's <c>.AsLookup(...)</c> renders under either adapter.
/// </remarks>
public class FluentUILookupFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUILookupFieldComponent<,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => field.AdditionalAttributes.ContainsKey("LookupDataProvider");
}
