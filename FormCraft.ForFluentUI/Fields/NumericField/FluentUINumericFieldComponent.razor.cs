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
    /// The bounds and ARIA state to splat onto the input, carrying only the keys this field
    /// actually configured.
    /// </summary>
    /// <remarks>
    /// Splatted rather than bound one parameter at a time because Fluent types <c>Min</c>,
    /// <c>Max</c> and <c>Step</c> as <c>TValue</c> - a non-nullable value type here, which has no
    /// spare value meaning "unset". Binding them directly would force every field to declare bounds
    /// it never asked for, so an unconfigured bound is instead simply absent from this dictionary
    /// and Fluent keeps its own default.
    /// </remarks>
    private Dictionary<string, object> ExtraAttributes { get; } = [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _localValue = CurrentValue;

        AddIfConfigured("Min");
        AddIfConfigured("Max");
        AddIfConfigured("Step");

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

    private void AddIfConfigured(string key)
    {
        if (GetAttribute<TValue?>(key) is { } value)
        {
            ExtraAttributes[key] = value;
        }
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}
