using System.Globalization;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Renders a nullable numeric field (int?, decimal?, double?, ...) bound to
/// MudNumericField&lt;TValue?&gt; so that a null model value displays as an empty
/// input and clearing the input writes null back to the model instead of
/// coercing it to default(TValue) (#150).
/// </summary>
/// <typeparam name="TModel">The model type containing the field.</typeparam>
/// <typeparam name="TValue">The underlying (non-nullable) numeric type.</typeparam>
public partial class MudBlazorNullableNumericFieldComponent<TModel, TValue>
    where TValue : struct
{
    private TValue? _localValue;

    /// <summary>
    /// This component binds <c>EffectiveAdornment</c> (#191), so the ShrinkLabel diagnostic judges
    /// it — see the sibling non-nullable component (#212).
    /// </summary>
    protected override Adornment? RenderedAdornment => EffectiveAdornment;

    /// <inheritdoc cref="INumericFieldComponent{TModel, TValue}.Min" />
    public TValue? Min { get; set; }

    /// <inheritdoc cref="INumericFieldComponent{TModel, TValue}.Max" />
    public TValue? Max { get; set; }

    /// <inheritdoc cref="INumericFieldComponent{TModel, TValue}.Step" />
    public TValue? Step { get; set; }

    /// <inheritdoc cref="INumericFieldComponent{TModel, TValue}.Format" />
    public string? Format { get; set; }

    /// <inheritdoc cref="INumericFieldComponent{TModel, TValue}.ShowSpinButtons" />
    public bool ShowSpinButtons { get; set; } = true;

    /// <summary>
    /// Culture used to parse and format the numeric input. Invariant unless an
    /// explicit Culture attribute is supplied, matching the non-nullable component.
    /// </summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _localValue = CurrentValue;

        Min = GetAttribute<TValue?>("Min");
        Max = GetAttribute<TValue?>("Max");
        Step = GetAttribute<TValue?>("Step") ?? GetDefaultStep();
        Format = GetAttribute<string>("Format");
        ShowSpinButtons = GetAttribute("ShowSpinButtons", true);
        Culture = GetAttribute<CultureInfo>("Culture") ?? CultureInfo.InvariantCulture;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        if (!EqualityComparer<TValue?>.Default.Equals(CurrentValue, _localValue))
        {
            _localValue = CurrentValue;
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

        // A cleared input must reach the model as null, not default(TValue).
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}
