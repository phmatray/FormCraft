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
        Options = GetAttribute<IEnumerable<SelectOption<TItem>>>("MultiSelectOptions") ?? Options;
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
