namespace FormCraft;

/// <summary>
/// Defines the contract for select/dropdown field components across different UI frameworks.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public interface ISelectFieldComponent<TModel, TValue> : IFieldComponent<TModel>
{
    /// <summary>
    /// Gets or sets the available options for selection.
    /// </summary>
    IEnumerable<SelectOption<TValue>> Options { get; set; }

    /// <summary>
    /// Gets or sets whether multiple selection is allowed.
    /// </summary>
    bool AllowMultiple { get; set; }

    /// <summary>
    /// Gets or sets whether the dropdown is searchable.
    /// </summary>
    bool IsSearchable { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text when no item is selected.
    /// </summary>
    string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets whether to show a clear button.
    /// </summary>
    bool ShowClearButton { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items that can be selected (for multi-select).
    /// </summary>
    int? MaxSelections { get; set; }

    /// <summary>
    /// Gets or sets whether to group options.
    /// </summary>
    bool GroupOptions { get; set; }
}