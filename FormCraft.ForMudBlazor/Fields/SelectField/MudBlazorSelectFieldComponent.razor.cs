namespace FormCraft.ForMudBlazor;

public partial class MudBlazorSelectFieldComponent<TModel, TValue>
{
    private TValue? _localValue;

    public IEnumerable<SelectOption<TValue>> Options { get; set; } = new List<SelectOption<TValue>>();
    public bool AllowMultiple { get; set; }
    public bool IsSearchable { get; set; }
    public new string? Placeholder { get; set; }
    public bool ShowClearButton { get; set; }
    public int? MaxSelections { get; set; }
    public bool GroupOptions { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value
        _localValue = CurrentValue;

        var options = GetAttribute<IEnumerable<SelectOption<TValue>>>("Options");
        if (options != null)
        {
            Options = options;
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        if (!EqualityComparer<TValue>.Default.Equals(CurrentValue, _localValue))
        {
            _localValue = CurrentValue;
        }
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}