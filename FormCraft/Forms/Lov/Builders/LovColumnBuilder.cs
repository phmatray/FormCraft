namespace FormCraft;

/// <summary>
/// Builder for configuring individual columns in the LOV grid.
/// </summary>
/// <typeparam name="TItem">The type of items in the LOV.</typeparam>
public class LovColumnBuilder<TItem>
{
    private readonly LovColumnDefinition<TItem> _column;

    internal LovColumnBuilder(LovColumnDefinition<TItem> column)
    {
        _column = column;
    }

    /// <summary>
    /// Sets the column width.
    /// </summary>
    /// <param name="width">The width value (e.g., "100px", "20%", "auto").</param>
    /// <returns>The LovColumnBuilder instance for method chaining.</returns>
    public LovColumnBuilder<TItem> Width(string width)
    {
        _column.Width = width;
        return this;
    }

    /// <summary>
    /// Disables sorting for this column.
    /// </summary>
    /// <returns>The LovColumnBuilder instance for method chaining.</returns>
    public LovColumnBuilder<TItem> NotSortable()
    {
        _column.Sortable = false;
        return this;
    }

    /// <summary>
    /// Disables filtering for this column.
    /// </summary>
    /// <returns>The LovColumnBuilder instance for method chaining.</returns>
    public LovColumnBuilder<TItem> NotFilterable()
    {
        _column.Filterable = false;
        return this;
    }

    /// <summary>
    /// Sets a format string for displaying values.
    /// </summary>
    /// <param name="format">The format string (e.g., "C2" for currency, "N0" for numbers).</param>
    /// <returns>The LovColumnBuilder instance for method chaining.</returns>
    public LovColumnBuilder<TItem> Format(string format)
    {
        _column.Format = format;
        return this;
    }

    /// <summary>
    /// Sets a custom cell template for rendering values.
    /// </summary>
    /// <param name="template">Function that takes an item and returns the display string.</param>
    /// <returns>The LovColumnBuilder instance for method chaining.</returns>
    public LovColumnBuilder<TItem> Template(Func<TItem, string> template)
    {
        _column.CellTemplate = template;
        return this;
    }
}
