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


    /// <summary>
    /// Whether the bound model property is a nullable value type. Nullable fields
    /// round-trip a cleared picker as null instead of default (#150).
    /// </summary>
    private bool IsNullableField => Nullable.GetUnderlyingType(Context.ActualFieldType) != null;

    private async Task OnLocalValueChanged()
    {
        var value = _localValue.HasValue ? DateOnly.FromDateTime(_localValue.Value) : default;
        SetValueWithoutNotification(value);

        object? notifiedValue = _localValue.HasValue
            ? DateOnly.FromDateTime(_localValue.Value)
            : IsNullableField ? null : default(DateOnly);
        await Context.OnValueChanged.InvokeAsync(notifiedValue);
    }
}
