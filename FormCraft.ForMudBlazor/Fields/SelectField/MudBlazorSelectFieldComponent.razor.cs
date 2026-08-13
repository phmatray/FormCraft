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
    }

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#298).
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        Placeholder = Context.Field.Placeholder;

        // Cleared BEFORE resolving, because ResolveOptions returns the current Options for a field
        // that configures none (and for one whose value is not enumerable). That reads as "keep the
        // default" and is exactly that on first load — but on a reload it means "keep the previous
        // field's options", which would offer the user choices from a field no longer on screen
        // (#298). Clearing first makes both fallbacks yield the empty default they intend.
        Options = new List<SelectOption<TValue>>();
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
