namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the autocomplete field renderer.
/// </summary>
public class MudBlazorAutocompleteFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorAutocompleteFieldComponent<,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        // This renderer handles fields with AutocompleteSearchFunc or AutocompleteOptionProvider
        return field.AdditionalAttributes.ContainsKey("AutocompleteSearchFunc") ||
               field.AdditionalAttributes.ContainsKey("AutocompleteOptionProvider");
    }
}
