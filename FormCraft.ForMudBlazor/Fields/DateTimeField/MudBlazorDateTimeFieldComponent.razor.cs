namespace FormCraft.ForMudBlazor;

public partial class MudBlazorDateTimeFieldComponent<TModel>
{
    private DateTime? _localValue;

    public DateTimeInputMode InputMode { get; set; } = DateTimeInputMode.Date;
    public string? Format { get; set; } = "yyyy-MM-dd";
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool ShowClearButton { get; set; } = true;
    public bool OpenOnFocus { get; set; } = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value
        _localValue = CurrentValue == default ? null : CurrentValue;

        // Load configuration from additional attributes
        InputMode = GetAttribute("InputMode", DateTimeInputMode.Date);
        Format = GetAttribute("Format", "yyyy-MM-dd");
        MinDate = GetAttribute<DateTime?>("MinDate");
        MaxDate = GetAttribute<DateTime?>("MaxDate");
        ShowClearButton = GetAttribute("ShowClearButton", true);
        OpenOnFocus = GetAttribute("OpenOnFocus", true);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        var currentDateValue = CurrentValue == default ? null : (DateTime?)CurrentValue;
        if (currentDateValue != _localValue)
        {
            _localValue = currentDateValue;
        }
    }

    private async Task OnLocalValueChanged()
    {
        var value = _localValue ?? default;
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }
}