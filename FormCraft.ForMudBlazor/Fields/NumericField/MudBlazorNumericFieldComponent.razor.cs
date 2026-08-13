using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorNumericFieldComponent<TModel, TValue>
{
    private TValue _localValue;

    /// <summary>
    /// This component binds <c>EffectiveAdornment</c> (#191), so the ShrinkLabel diagnostic judges
    /// it — a numeric field really does draw the configured adornment, and its warning is correct
    /// (#212).
    /// </summary>
    protected override Adornment? RenderedAdornment => EffectiveAdornment;

    /// <summary>
    /// The adornment click handler configured by <c>WithAdornment(..., onClick:)</c>, or null (#215).
    /// </summary>
    /// <remarks>
    /// Typed <c>Action&lt;TValue?&gt;</c> rather than the string overload's <c>Action&lt;string?&gt;</c>:
    /// that shape is right there only because the field's value is a string. Resolved defensively —
    /// <c>AdditionalAttributes</c> is untyped, so a value of another shape reads back as "no handler"
    /// rather than throwing at click time.
    /// </remarks>
    private Action<TValue?>? OnAdornmentClick =>
        GetAttribute<Action<TValue?>?>(MudBlazorFieldBuilderExtensions.AdornmentClickAttribute);

    /// <summary>
    /// The callback bound to MudBlazor, or <c>default</c> when no handler is configured (#216).
    /// </summary>
    /// <remarks>
    /// Returning <c>default</c> matters: binding a method group gives an <c>EventCallback</c> whose
    /// <c>HasDelegate</c> is always true, and MudBlazor draws a real <c>&lt;button&gt;</c> for that —
    /// turning a decorative icon into a focus stop for keyboard and screen-reader users.
    /// </remarks>
    private EventCallback<MouseEventArgs> AdornmentClick =>
        OnAdornmentClick is null
            ? default
            : EventCallback.Factory.Create<MouseEventArgs>(this, () => OnAdornmentClick(CurrentValue));

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
    }

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#298).
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

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