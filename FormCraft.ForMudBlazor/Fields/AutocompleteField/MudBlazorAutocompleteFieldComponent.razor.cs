namespace FormCraft.ForMudBlazor;

public partial class MudBlazorAutocompleteFieldComponent<TModel, TValue>
{
    private Func<string, CancellationToken, Task<IEnumerable<SelectOption<TValue>>>>? _searchFunc;
    private object? _optionProvider;
    private int _debounceMs = 300;
    private int _minCharacters = 1;
    private Func<TValue, string> _toStringFunc = v => v?.ToString() ?? string.Empty;

    /// <summary>
    /// Cached lookup from display string back to TValue for MudAutocomplete results.
    /// </summary>
    private readonly Dictionary<string, TValue> _valueLookup = new();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _searchFunc = GetAttribute<Func<string, CancellationToken, Task<IEnumerable<SelectOption<TValue>>>>>("AutocompleteSearchFunc");
        _optionProvider = GetAttribute<object>("AutocompleteOptionProvider");
        _debounceMs = GetAttribute("AutocompleteDebounceMs", 300);
        _minCharacters = GetAttribute("AutocompleteMinCharacters", 1);

        var customToString = GetAttribute<Func<TValue, string>>("AutocompleteToStringFunc");
        if (customToString != null)
        {
            _toStringFunc = customToString;
        }
    }

    private async Task<IEnumerable<TValue>> SearchAsync(string searchText, CancellationToken cancellationToken)
    {
        IEnumerable<SelectOption<TValue>> options;

        if (_searchFunc != null)
        {
            options = await _searchFunc(searchText ?? string.Empty, cancellationToken);
        }
        else if (_optionProvider != null)
        {
            // Use reflection to call SearchAsync on the IOptionProvider<TModel, TValue>
            var providerType = typeof(IOptionProvider<,>).MakeGenericType(typeof(TModel), typeof(TValue));
            var searchMethod = providerType.GetMethod("SearchAsync");
            if (searchMethod != null)
            {
                var task = (Task<IEnumerable<SelectOption<TValue>>>)searchMethod.Invoke(
                    _optionProvider,
                    new object?[] { searchText ?? string.Empty, Context.Model, cancellationToken })!;
                options = await task;
            }
            else
            {
                return Enumerable.Empty<TValue>();
            }
        }
        else
        {
            return Enumerable.Empty<TValue>();
        }

        var optionsList = options.ToList();

        // Build the display-to-value lookup and set up ToString
        _valueLookup.Clear();
        foreach (var option in optionsList)
        {
            var displayStr = _toStringFunc(option.Value);
            _valueLookup[displayStr] = option.Value;
        }

        // If no custom toString was provided, use label-based display
        var customToString = GetAttribute<Func<TValue, string>>("AutocompleteToStringFunc");
        if (customToString == null)
        {
            // Build a value-to-label map for display
            var labelMap = optionsList.ToDictionary(o => o.Value!, o => o.Label);
            _toStringFunc = v =>
            {
                if (v == null) return string.Empty;
                return labelMap.TryGetValue(v, out var label) ? label : v.ToString() ?? string.Empty;
            };
        }

        return optionsList.Select(o => o.Value);
    }
}
