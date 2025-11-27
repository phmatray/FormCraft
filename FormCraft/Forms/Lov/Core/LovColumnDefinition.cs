namespace FormCraft;

/// <summary>
/// Defines a column in the LOV selection grid.
/// </summary>
/// <typeparam name="TItem">The type of items in the LOV.</typeparam>
public class LovColumnDefinition<TItem>
{
    /// <summary>
    /// Gets or sets the column header text displayed in the grid.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name for this column.
    /// Used for sorting and filtering operations.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function to extract the column value from an item.
    /// </summary>
    public Func<TItem, object?> ValueSelector { get; set; } = _ => null;

    /// <summary>
    /// Gets or sets whether this column is sortable.
    /// Default is true.
    /// </summary>
    public bool Sortable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this column supports filtering.
    /// Default is true.
    /// </summary>
    public bool Filterable { get; set; } = true;

    /// <summary>
    /// Gets or sets the column width (e.g., "100px", "20%").
    /// If null, the column will auto-size.
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// Gets or sets the format string for displaying values (e.g., "C2" for currency).
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets a custom cell template for advanced rendering.
    /// When set, this template is used instead of the default value display.
    /// </summary>
    public Func<TItem, string>? CellTemplate { get; set; }

    /// <summary>
    /// Initializes a new instance of the LovColumnDefinition class.
    /// </summary>
    public LovColumnDefinition() { }

    /// <summary>
    /// Initializes a new instance of the LovColumnDefinition class with a header and value selector.
    /// </summary>
    /// <param name="header">The column header text.</param>
    /// <param name="valueSelector">The function to extract values.</param>
    /// <param name="propertyName">The property name for sorting/filtering.</param>
    public LovColumnDefinition(string header, Func<TItem, object?> valueSelector, string propertyName = "")
    {
        Header = header;
        ValueSelector = valueSelector;
        PropertyName = propertyName;
    }
}
