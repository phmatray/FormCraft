namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the autocomplete field renderer.
/// </summary>
/// <remarks>
/// Configuration-driven, so it must be registered above the type-based renderers: an autocompleted
/// <c>string</c> field would otherwise match the text renderer first and lose its search entirely.
/// </remarks>
public class FluentUIAutocompleteFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUIAutocompleteFieldComponent<,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => field.AdditionalAttributes.ContainsKey("AutocompleteSearchFunc") ||
           field.AdditionalAttributes.ContainsKey("AutocompleteOptionProvider");
}
