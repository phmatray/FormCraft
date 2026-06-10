namespace FormCraft.ForMudBlazor;

public partial class MudBlazorSelectFieldComponent<TModel, TValue>
{
    private TValue? _localValue;

    public IEnumerable<SelectOption<TValue>> Options { get; set; } = new List<SelectOption<TValue>>();
    public bool AllowMultiple { get; set; }
    public bool IsSearchable { get; set; }
    public new string? Placeholder { get; set; }
    public bool ShowClearButton { get; set; }
    public int? MaxSelections { get; set; }
    public bool GroupOptions { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value
        _localValue = CurrentValue;

        Placeholder = Context.Field.Placeholder;
        Options = ResolveOptions();
    }

    /// <summary>
    /// Resolves the configured options into <see cref="SelectOption{TValue}"/> instances.
    /// Accepts an exactly-typed IEnumerable&lt;SelectOption&lt;TValue&gt;&gt; as well as any
    /// other enumerable whose elements expose Value/Label properties (e.g. options typed
    /// with the underlying value type for a nullable TValue), matching the conversion the
    /// legacy render path performed via reflection.
    /// </summary>
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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        if (!EqualityComparer<TValue>.Default.Equals(CurrentValue, _localValue))
        {
            _localValue = CurrentValue;
        }
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}