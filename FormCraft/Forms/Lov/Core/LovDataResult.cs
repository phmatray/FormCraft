namespace FormCraft;

/// <summary>
/// Represents the result of a List of Values (LOV) data query,
/// containing the items and total count for pagination.
/// </summary>
/// <typeparam name="TItem">The type of items in the result.</typeparam>
public class LovDataResult<TItem>
{
    /// <summary>
    /// Gets or sets the list of items returned by the query.
    /// </summary>
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the total count of all items matching the query criteria.
    /// This is used for pagination to know the total number of pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets whether there are more items available beyond the current page.
    /// </summary>
    public bool HasMore => Items.Count < TotalCount;

    /// <summary>
    /// Initializes a new instance of the LovDataResult class.
    /// </summary>
    public LovDataResult() { }

    /// <summary>
    /// Initializes a new instance of the LovDataResult class with items and total count.
    /// </summary>
    /// <param name="items">The items for this page.</param>
    /// <param name="totalCount">The total count of all matching items.</param>
    public LovDataResult(IReadOnlyList<TItem> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Creates an empty result with no items.
    /// </summary>
    /// <returns>An empty LovDataResult.</returns>
    public static LovDataResult<TItem> Empty() => new([], 0);

    /// <summary>
    /// Creates a result from a collection, useful for in-memory data sources.
    /// </summary>
    /// <param name="items">The complete collection of items.</param>
    /// <param name="query">The query to apply for pagination.</param>
    /// <returns>A paginated LovDataResult.</returns>
    public static LovDataResult<TItem> FromCollection(
        IEnumerable<TItem> items,
        LovQuery query)
    {
        var allItems = items.ToList();
        var pagedItems = allItems
            .Skip(query.StartIndex)
            .Take(query.Count)
            .ToList();

        return new LovDataResult<TItem>(pagedItems, allItems.Count);
    }
}
