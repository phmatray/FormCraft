namespace FormCraft;

/// <summary>
/// Represents a page of lookup results with total count for pagination.
/// </summary>
/// <typeparam name="TItem">The type of the lookup item.</typeparam>
public class LookupResult<TItem>
{
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public IEnumerable<TItem> Items { get; set; } = Enumerable.Empty<TItem>();

    /// <summary>
    /// Gets or sets the total number of items matching the query.
    /// </summary>
    public int TotalCount { get; set; }
}
