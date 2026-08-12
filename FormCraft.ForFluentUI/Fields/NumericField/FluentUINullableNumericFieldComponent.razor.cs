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
    /// The bounds and ARIA state to splat onto the input - see the sibling component for why these
    /// are splatted rather than bound individually.
    /// </summary>
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

        if (!EqualityComparer<TValue?>.Default.Equals(CurrentValue, _localValue))
        {
            _localValue = CurrentValue;
        }
    }

    private void AddIfConfigured(string key)
    {
        // Boxed as TValue? so it binds to the input's own nullable Min/Max/Step parameters.
        if (GetAttribute<TValue?>(key) is { } value)
        {
            ExtraAttributes[key] = (TValue?)value;
        }
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}
