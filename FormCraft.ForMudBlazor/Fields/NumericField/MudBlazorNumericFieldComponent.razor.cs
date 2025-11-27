namespace FormCraft.ForMudBlazor;

public partial class MudBlazorNumericFieldComponent<TModel, TValue>
{
    private TValue _localValue;

    public TValue? Min { get; set; }
    public TValue? Max { get; set; }
    public TValue? Step { get; set; }
    public string? Format { get; set; }
    public bool ShowSpinButtons { get; set; } = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value (CurrentValue is TValue, not TValue? due to TValue? behavior with unconstrained generics)
        _localValue = CurrentValue is TValue val ? val : default!;

        Min = GetAttribute<TValue?>("Min");
        Max = GetAttribute<TValue?>("Max");
        Step = GetAttribute<TValue?>("Step") ?? GetDefaultStep();
        Format = GetAttribute<string>("Format");
        ShowSpinButtons = GetAttribute("ShowSpinButtons", true);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        var currentVal = CurrentValue is TValue val ? val : default!;
        if (!EqualityComparer<TValue>.Default.Equals(currentVal, _localValue))
        {
            _localValue = currentVal;
        }
    }

    private TValue GetDefaultStep()
    {
        // Default step values based on type
        if (typeof(TValue) == typeof(decimal) || typeof(TValue) == typeof(decimal?))
            return (TValue)(object)0.01m;
        if (typeof(TValue) == typeof(double) || typeof(TValue) == typeof(double?) ||
            typeof(TValue) == typeof(float) || typeof(TValue) == typeof(float?))
            return (TValue)(object)0.1;
        return (TValue)(object)1;
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}