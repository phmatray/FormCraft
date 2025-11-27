namespace FormCraft.ForMudBlazor;

public partial class MudBlazorDateTimeFieldComponent<TModel>
{
    public DateTimeInputMode InputMode { get; set; } = DateTimeInputMode.Date;
    public string? Format { get; set; } = "yyyy-MM-dd";
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool ShowClearButton { get; set; } = true;
    public bool OpenOnFocus { get; set; } = true;
    private DateTime? DateValue => CurrentValue == default ? null : CurrentValue;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Load configuration from additional attributes
        InputMode = GetAttribute("InputMode", DateTimeInputMode.Date);
        Format = GetAttribute("Format", "yyyy-MM-dd");
        MinDate = GetAttribute<DateTime?>("MinDate");
        MaxDate = GetAttribute<DateTime?>("MaxDate");
        ShowClearButton = GetAttribute("ShowClearButton", true);
        OpenOnFocus = GetAttribute("OpenOnFocus", true);
    }

    private async Task HandleDateChanged(DateTime? date)
    {
        var value = date ?? default;
        // Update local state FIRST to prevent race condition during parent re-render
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }
}