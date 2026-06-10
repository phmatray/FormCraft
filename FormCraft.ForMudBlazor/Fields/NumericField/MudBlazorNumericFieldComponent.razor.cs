using System.Globalization;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorNumericFieldComponent<TModel, TValue>
{
    private TValue _localValue;

    public TValue? Min { get; set; }
    public TValue? Max { get; set; }
    public TValue? Step { get; set; }
    public string? Format { get; set; }
    public bool ShowSpinButtons { get; set; } = true;
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

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

        // Parity with the legacy render path: numeric input parsing is
        // culture-invariant unless an explicit Culture attribute is supplied.
        Culture = GetAttribute<CultureInfo>("Culture") ?? CultureInfo.InvariantCulture;
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

    private static TValue GetDefaultStep()
    {
        // Unbox-then-cast only succeeds when the boxed type matches TValue exactly,
        // so each floating type needs its own literal (0.1 boxed as double cannot be
        // unboxed as float, and 1 boxed as int cannot be unboxed as long/short/byte).
        if (typeof(TValue) == typeof(decimal))
            return (TValue)(object)0.01m;
        if (typeof(TValue) == typeof(double))
            return (TValue)(object)0.1d;
        if (typeof(TValue) == typeof(float))
            return (TValue)(object)0.1f;
        return (TValue)Convert.ChangeType(1, typeof(TValue));
    }

    private static TValue GetTypeMinValue()
    {
        var field = typeof(TValue).GetField("MinValue");
        return field != null ? (TValue)field.GetValue(null)! : default;
    }

    private static TValue GetTypeMaxValue()
    {
        var field = typeof(TValue).GetField("MaxValue");
        return field != null ? (TValue)field.GetValue(null)! : default;
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}