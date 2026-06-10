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

    private async Task OnLocalValueChanged()
    {
        var value = _localValue.HasValue ? TimeOnly.FromTimeSpan(_localValue.Value) : default;
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }
}
