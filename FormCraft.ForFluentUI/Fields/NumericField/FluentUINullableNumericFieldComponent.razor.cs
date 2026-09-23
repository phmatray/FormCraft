namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a nullable numeric field (<c>int?</c>, <c>decimal?</c>, ...) as a Fluent UI number input.
/// </summary>
/// <remarks>
/// Exists so <c>null</c> displays as an empty input and round-trips back to the model rather than
/// being coerced to <c>default(TValue)</c> — a null <c>int?</c> must not become <c>0</c> (#150).
/// <see cref="FluentUINumericFieldRenderer"/> selects it for nullable field types.
/// </remarks>
public partial class FluentUINullableNumericFieldComponent<TModel, TValue> where TValue : struct
{
    private TValue? _localValue;

    /// <summary>
    /// The ARIA state to splat onto the input - see the sibling component for why bounds moved off
    /// this dictionary onto real parameters (#348).
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

        if (!EqualityComparer<TValue?>.Default.Equals(CurrentValue, _localValue))
        {
            _localValue = CurrentValue;
        }
    }

    /// <summary>
    /// <c>FluentNumberInput&lt;TValue?&gt;.Min</c> still has to be supplied a real value rather than
    /// <c>null</c> when unconfigured: Fluent's own bounds check
    /// (<c>Comparer&lt;TValue?&gt;.Default.Compare</c>) treats <c>null</c> as lower than any value, so
    /// a <c>null</c> <c>Max</c> would compare as "exceeded" by every input and silently clamp every
    /// entered value to <c>null</c> (decompiled under #348) — see the sibling component for the same
    /// reflection-based defaults. Resolved once per closed <c>TValue</c> into a <c>static readonly</c>
    /// field rather than recomputed on every render, the same as the sibling component.
    /// </summary>
    private static readonly TValue TypeMinValue = ResolveTypeBound("MinValue");

    /// <inheritdoc cref="TypeMinValue"/>
    private static readonly TValue TypeMaxValue = ResolveTypeBound("MaxValue");

    /// <inheritdoc cref="FluentUINumericFieldComponent{TModel, TValue}.DefaultStep"/>
    private static readonly TValue DefaultStep = (TValue)Convert.ChangeType(1, typeof(TValue));

    private static TValue ResolveTypeBound(string fieldName)
    {
        var field = typeof(TValue).GetField(fieldName);
        return field != null ? (TValue)field.GetValue(null)! : default;
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}
