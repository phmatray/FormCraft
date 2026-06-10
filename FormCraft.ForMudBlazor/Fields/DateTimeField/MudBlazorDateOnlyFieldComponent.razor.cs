namespace FormCraft.ForMudBlazor;

public partial class MudBlazorDateOnlyFieldComponent<TModel>
{
    private DateTime? _localValue;

    public string? Format { get; set; } = "yyyy-MM-dd";
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool ShowClearButton { get; set; } = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _localValue = CurrentValue == default ? null : CurrentValue.ToDateTime(TimeOnly.MinValue);

        Format = GetAttribute("Format", "yyyy-MM-dd");
        MinDate = GetAttribute<DateTime?>("MinDate");
        MaxDate = GetAttribute<DateTime?>("MaxDate");
        ShowClearButton = GetAttribute("ShowClearButton", true);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        var currentDateValue = CurrentValue == default ? (DateTime?)null : CurrentValue.ToDateTime(TimeOnly.MinValue);
        if (currentDateValue != _localValue)
        {
            _localValue = currentDateValue;
        }
    }

    private async Task OnLocalValueChanged()
    {
        var value = _localValue.HasValue ? DateOnly.FromDateTime(_localValue.Value) : default;
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }
}
