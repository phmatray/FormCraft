namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a non-nullable numeric field as a Fluent UI number input.
/// </summary>
/// <remarks>
/// The nullable counterpart is <see cref="FluentUINullableNumericFieldComponent{TModel, TValue}"/>;
/// <see cref="FluentUINumericFieldRenderer"/> chooses between them. They are separate components
/// rather than one closed over <c>TValue?</c> because a nullable field must display an empty input
/// for null and round-trip it back, instead of coercing it to <c>default(TValue)</c> (#150).
/// </remarks>
public partial class FluentUINumericFieldComponent<TModel, TValue> where TValue : struct
{
    private TValue _localValue;

    /// <summary>
    /// The ARIA state to splat onto the input. Bounds used to share this dictionary too (#348): a
    /// key is only ever <i>added</i> by a splat, so a field that stopped configuring a bound left
    /// the previous field's value in place on <c>FluentNumberInput</c> — Blazor retains a component
    /// parameter that a later render stops supplying. <c>Min</c>/<c>Max</c>/<c>Step</c> are now real
    /// parameters (below) assigned unconditionally instead.
    /// </summary>
    private Dictionary<string, object> ExtraAttributes { get; } = [];

    private TValue? Min { get; set; }
    private TValue? Max { get; set; }
    private TValue? Step { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _localValue = CurrentValue;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#335).
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        Min = GetAttribute<TValue?>("Min");
        Max = GetAttribute<TValue?>("Max");
        Step = GetAttribute<TValue?>("Step");

        ExtraAttributes.Clear();
        if (AriaRequired is { } ariaRequired)
        {
            ExtraAttributes["aria-required"] = ariaRequired;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!EqualityComparer<TValue>.Default.Equals(CurrentValue, _localValue))
        {
            _localValue = CurrentValue;
        }
    }

    /// <summary>
    /// <c>FluentNumberInput.Min</c>/<c>Max</c>/<c>Step</c> are <c>TValue</c>, not <c>TValue?</c>, so
    /// an unconfigured bound has to be supplied explicitly rather than left unset — matching Fluent's
    /// own per-type defaults (its constructor sets exactly these values, decompiled under #348)
    /// keeps an unbounded field genuinely unbounded instead of clamping it to <c>default(TValue)</c>.
    /// Reflection, not a hand-maintained switch, so a numeric type FormCraft adds later needs no
    /// entry here — mirrors <c>MudBlazorNumericFieldComponent.GetTypeMinValue()</c>.
    /// </summary>
    private static TValue GetTypeMinValue()
    {
        var field = typeof(TValue).GetField("MinValue");
        return field != null ? (TValue)field.GetValue(null)! : default;
    }

    /// <inheritdoc cref="GetTypeMinValue"/>
    private static TValue GetTypeMaxValue()
    {
        var field = typeof(TValue).GetField("MaxValue");
        return field != null ? (TValue)field.GetValue(null)! : default;
    }

    /// <summary>
    /// Fluent's own default step for every supported numeric type is <c>1</c> (decompiled under
    /// #348), so this reverts to that rather than to MudBlazor's smaller floating-point defaults.
    /// </summary>
    private static TValue GetDefaultStep() => (TValue)Convert.ChangeType(1, typeof(TValue));

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}
