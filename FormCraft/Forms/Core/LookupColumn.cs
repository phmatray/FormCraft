namespace FormCraft;

/// <summary>
/// Defines a column in a lookup table dialog.
/// </summary>
/// <typeparam name="TItem">The type of the lookup item.</typeparam>
public class LookupColumn<TItem>
{
    /// <summary>
    /// Gets or sets the column header title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function that extracts the column value from an item.
    /// </summary>
    public Func<TItem, object?> ValueSelector { get; set; } = _ => null;

    /// <summary>
    /// Gets or sets whether the column is sortable.
    /// </summary>
    public bool Sortable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the column is filterable.
    /// </summary>
    public bool Filterable { get; set; } = true;
}
