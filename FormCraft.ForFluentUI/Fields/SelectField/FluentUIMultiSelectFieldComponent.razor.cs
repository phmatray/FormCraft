namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a field configured with <c>.AsMultiSelect(...)</c> as a multiple-selection Fluent select.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
/// <typeparam name="TItem">The type of an individual option value.</typeparam>
public partial class FluentUIMultiSelectFieldComponent<TModel, TItem>
{
    private IEnumerable<SelectOption<TItem>> _selectedOptions = [];

    /// <summary>The options this field offers.</summary>
    private IEnumerable<SelectOption<TItem>> Options { get; set; } = [];

    /// <inheritdoc />
    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#335).
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        // Falls back to an EMPTY list rather than to the current value of Options. `?? Options` reads
        // as "keep the default" and is that on first load, but on a reload it means "keep the previous
        // field's options" — offering the user choices from a field no longer on screen (#335).
        Options = GetAttribute<IEnumerable<SelectOption<TItem>>>("MultiSelectOptions")?.ToList()
                  ?? [];

        // Recomputed here as well as in OnInitialized: the selection is projected THROUGH Options, so
        // it is stale the moment they change.
        _selectedOptions = OptionsFor(CurrentValue);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Re-sync when the model is changed from outside the component (a dependency, a reset).
        var current = (CurrentValue ?? []).ToList();
        if (!current.SequenceEqual(_selectedOptions.Select(o => o.Value)))
        {
            _selectedOptions = OptionsFor(current);
        }
    }

    /// <summary>
    /// Maps the model's values onto the option instances Fluent's select holds.
    /// </summary>
    /// <remarks>
    /// Fluent compares selected items by option instance, so handing it freshly-built
    /// <see cref="SelectOption{T}"/> objects would leave the dropdown showing nothing selected even
    /// though the model carries values. Matching against <see cref="Options"/> by value returns the
    /// instances the list itself is bound to. A value with no matching option is dropped rather
    /// than invented: it cannot be displayed or deselected, so showing it would be a dead entry.
    /// </remarks>
    private IEnumerable<SelectOption<TItem>> OptionsFor(IEnumerable<TItem>? values)
    {
        if (values is null)
        {
            return [];
        }

        var wanted = values.ToList();
        return Options
            .Where(option => wanted.Contains(option.Value))
            .ToList();
    }

    private async Task HandleSelectedItemsChangedAsync(IEnumerable<SelectOption<TItem>>? options)
    {
        _selectedOptions = options?.ToList() ?? [];
        var values = _selectedOptions.Select(o => o.Value).ToList();

        SetValueWithoutNotification(values);
        await Context.OnValueChanged.InvokeAsync(values);
    }
}
