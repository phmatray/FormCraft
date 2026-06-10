namespace FormCraft.ForMudBlazor;

public partial class MudBlazorTimeOnlyFieldComponent<TModel>
{
    private TimeSpan? _localValue;

    public bool ShowClearButton { get; set; } = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _localValue = CurrentValue == default ? null : CurrentValue.ToTimeSpan();

        ShowClearButton = GetAttribute("ShowClearButton", true);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        var currentTimeValue = CurrentValue == default ? (TimeSpan?)null : CurrentValue.ToTimeSpan();
        if (currentTimeValue != _localValue)
        {
            _localValue = currentTimeValue;
        }
    }


    /// <summary>
    /// Whether the bound model property is a nullable value type. Nullable fields
    /// round-trip a cleared picker as null instead of default (#150).
    /// </summary>
    private bool IsNullableField => Nullable.GetUnderlyingType(Context.ActualFieldType) != null;

    private async Task OnLocalValueChanged()
    {
        var value = _localValue.HasValue ? TimeOnly.FromTimeSpan(_localValue.Value) : default;
        SetValueWithoutNotification(value);

        object? notifiedValue = _localValue.HasValue
            ? TimeOnly.FromTimeSpan(_localValue.Value)
            : IsNullableField ? null : default(TimeOnly);
        await Context.OnValueChanged.InvokeAsync(notifiedValue);
    }
}
