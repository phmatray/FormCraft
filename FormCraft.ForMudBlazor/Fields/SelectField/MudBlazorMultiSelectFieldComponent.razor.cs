namespace FormCraft.ForMudBlazor;

/// <summary>
/// Renders a multi-selection dropdown for fields configured with the
/// MultiSelectOptions attribute (e.g. via the AsMultiSelect builder extension).
/// </summary>
/// <typeparam name="TModel">The model type containing the field.</typeparam>
/// <typeparam name="TItem">The type of an individual option value.</typeparam>
public partial class MudBlazorMultiSelectFieldComponent<TModel, TItem>
{
    private IReadOnlyCollection<TItem> _selectedValues = [];

    /// <summary>
    /// Gets or sets the options available for selection.
    /// </summary>
    public IEnumerable<SelectOption<TItem>> Options { get; set; } = new List<SelectOption<TItem>>();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _selectedValues = CurrentValue?.ToList() ?? [];
    }

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#298).
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        // Falls back to an empty list rather than to the CURRENT value of Options. `?? Options` reads
        // as "keep the default" and is that on first load, but on a reload it means "keep the previous
        // field's options" — the patch-not-reload trap (#298), and a nastier instance of it than most,
        // because the user would be offered choices from a field that is no longer on screen.
        Options = GetAttribute<IEnumerable<SelectOption<TItem>>>("MultiSelectOptions")
                  ?? new List<SelectOption<TItem>>();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local selection when the model changes externally
        var current = (CurrentValue ?? []).ToList();
        if (!current.SequenceEqual(_selectedValues))
        {
            _selectedValues = current;
        }
    }

    private async Task OnSelectedValuesChanged(IReadOnlyCollection<TItem>? values)
    {
        _selectedValues = values?.ToList() ?? [];
        SetValueWithoutNotification(_selectedValues);
        await Context.OnValueChanged.InvokeAsync(_selectedValues);
    }
}
