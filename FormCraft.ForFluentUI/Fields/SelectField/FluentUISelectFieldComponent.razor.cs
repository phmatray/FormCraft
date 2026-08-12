namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a field configured with <c>.WithOptions(...)</c> as a Fluent UI select.
/// </summary>
public partial class FluentUISelectFieldComponent<TModel, TValue>
{
    private TValue? _localValue;

    /// <summary>The options this field offers.</summary>
    private IEnumerable<SelectOption<TValue>> Options { get; set; } = [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _localValue = CurrentValue;
        Options = ResolveOptions();
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
    /// Resolves the configured options into <see cref="SelectOption{TValue}"/> instances.
    /// </summary>
    /// <remarks>
    /// Accepts an exactly-typed sequence as well as any other enumerable whose elements expose
    /// Value/Label properties - options typed with the underlying value type for a nullable
    /// <c>TValue</c>, for instance. This mirrors the MudBlazor adapter's resolution so the same
    /// <c>.WithOptions(...)</c> call behaves identically on both.
    /// </remarks>
    private IEnumerable<SelectOption<TValue>> ResolveOptions()
    {
        if (!Context.Field.AdditionalAttributes.TryGetValue("Options", out var optionsObj) || optionsObj is null)
        {
            return Options;
        }

        if (optionsObj is IEnumerable<SelectOption<TValue>> typedOptions)
        {
            return typedOptions;
        }

        if (optionsObj is not System.Collections.IEnumerable rawOptions)
        {
            return Options;
        }

        var converted = new List<SelectOption<TValue>>();
        foreach (var option in rawOptions)
        {
            if (option is null)
            {
                continue;
            }

            var optionType = option.GetType();
            var valueProperty = optionType.GetProperty("Value");
            var labelProperty = optionType.GetProperty("Label");
            if (valueProperty is null || labelProperty is null)
            {
                continue;
            }

            var rawValue = valueProperty.GetValue(option);
            var value = rawValue is TValue typedValue ? typedValue : default;
            var label = labelProperty.GetValue(option)?.ToString() ?? string.Empty;
            converted.Add(new SelectOption<TValue>(value!, label));
        }

        return converted;
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}
