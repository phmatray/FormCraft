using Microsoft.FluentUI.AspNetCore.Components;
namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a field configured with <c>.AsAutocomplete(...)</c> as a Fluent UI autocomplete.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
/// <typeparam name="TValue">The field's value type.</typeparam>
public partial class FluentUIAutocompleteFieldComponent<TModel, TValue>
{
    private IEnumerable<SelectOption<TValue>> _options = [];
    private SelectOption<TValue>? _selectedOption;
    private Func<string, CancellationToken, Task<IEnumerable<SelectOption<TValue>>>>? _searchFunc;
    private object? _optionProvider;

    /// <summary>
    /// How long Fluent waits after a keystroke before raising the search, from
    /// <c>.AsAutocomplete(debounceMs: ...)</c>.
    /// </summary>
    private int DebounceMs => GetAttribute("AutocompleteDebounceMs", 300);

    /// <summary>
    /// The shortest search text that triggers a lookup, from
    /// <c>.AsAutocomplete(minCharacters: ...)</c>.
    /// </summary>
    private int MinCharacters => GetAttribute("AutocompleteMinCharacters", 1);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        SyncSelectedOption();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#335).
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        _searchFunc = GetAttribute<Func<string, CancellationToken, Task<IEnumerable<SelectOption<TValue>>>>>(
            "AutocompleteSearchFunc");
        _optionProvider = GetAttribute<object>("AutocompleteOptionProvider");

        // Both are results of the configuration above rather than of the value, so they belong to the
        // field that produced them. _options is the previous field's last result set, which the
        // dropdown would keep offering until a fresh search replaced it; _selectedOption is what the
        // box displays, and SyncSelectedOption only rebuilds it when the VALUE differs — so two
        // fields whose values compare equal but whose labels differ would leave the previous field's
        // label on screen.
        _options = [];
        _selectedOption = null;

        SyncSelectedOption();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Re-sync when the model changes from outside this component - a value provider, a
        // dependency, a form reset. Building the option only in OnInitialized left the box showing
        // the previous selection after any of those, while the model already held the new value.
        // FluentUIMultiSelectFieldComponent re-syncs for the same reason.
        SyncSelectedOption();
    }

    private void SyncSelectedOption()
    {
        if (CurrentValue is null)
        {
            _selectedOption = null;
            return;
        }

        if (_selectedOption is null || !EqualityComparer<TValue>.Default.Equals(_selectedOption.Value, CurrentValue))
        {
            _selectedOption = new SelectOption<TValue>(CurrentValue, DisplayFor(CurrentValue));
        }
    }

    /// <summary>
    /// Runs the configured search and hands the results back on the event args, which is how
    /// Fluent's autocomplete collects them.
    /// </summary>
    /// <remarks>
    /// Assigning <c>args.Items</c> is the contract - returning a value would be ignored. Below
    /// <see cref="MinCharacters"/> the list is emptied rather than left untouched, so a user
    /// deleting back to one character does not keep seeing the previous, wider result set.
    /// </remarks>
    private async Task HandleOptionsSearchAsync(OptionsSearchEventArgs<SelectOption<TValue>> args)
    {
        var text = args.Text ?? string.Empty;

        if (text.Length < MinCharacters)
        {
            _options = [];
            args.Items = _options;
            return;
        }

        _options = await SearchAsync(text, CancellationToken.None);
        args.Items = _options;
    }

    private async Task<IEnumerable<SelectOption<TValue>>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        if (_searchFunc is not null)
        {
            return await _searchFunc(text, cancellationToken);
        }

        if (_optionProvider is null)
        {
            return [];
        }

        // The provider is stored as object because IOptionProvider<TModel, TValue> is closed over
        // the model type the builder saw, which this component cannot name statically.
        var providerType = typeof(IOptionProvider<,>).MakeGenericType(typeof(TModel), typeof(TValue));
        var searchMethod = providerType.GetMethod("SearchAsync");
        if (searchMethod is null)
        {
            return [];
        }

        var task = (Task<IEnumerable<SelectOption<TValue>>>)searchMethod.Invoke(
            _optionProvider,
            [text, Context.Model, cancellationToken])!;

        return await task;
    }

    private async Task HandleSelectedItemChangedAsync(SelectOption<TValue>? option)
    {
        _selectedOption = option;
        var value = option is null ? default : option.Value;

        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }

    /// <summary>
    /// The text shown for an already-selected value, via <c>.AsAutocomplete(toStringFunc: ...)</c>
    /// when supplied.
    /// </summary>
    private string DisplayFor(TValue value)
    {
        var toStringFunc = GetAttribute<Func<TValue, string>>("AutocompleteToStringFunc");
        return toStringFunc is not null
            ? toStringFunc(value)
            : value?.ToString() ?? string.Empty;
    }
}
