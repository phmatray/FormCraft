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
    /// Ignores <see cref="LovQuery.SearchText"/> (no predicate to apply it with); applies
    /// <see cref="LovQuery.Context"/> filtering and <see cref="LovQuery.SortDefinitions"/> exactly
    /// as the 3-argument overload does.
    /// </summary>
    /// <param name="items">The complete collection of items.</param>
    /// <param name="query">The query to apply for context filtering, sorting and pagination.</param>
    /// <returns>A filtered, sorted, and paginated LovDataResult.</returns>
    public static LovDataResult<TItem> FromCollection(
        IEnumerable<TItem> items,
        LovQuery query)
        => FromCollection(items, query, null);

    /// <summary>
    /// Creates a result from a collection, useful for in-memory data sources — the shared
    /// search → context → sort → page pipeline that <see cref="LambdaLovDataProvider{TItem}"/>
    /// and <c>LovBuilder&lt;TModel, TValue, TItem&gt;.WithDataSource(Func&lt;IEnumerable&lt;TItem&gt;&gt;)</c>
    /// (#472) both route through.
    /// </summary>
    /// <param name="items">The complete collection of items.</param>
    /// <param name="query">
    /// The query to apply for search filtering, context filtering, sorting and pagination.
    /// </param>
    /// <param name="searchPredicate">
    /// Optional predicate matching an item against <see cref="LovQuery.SearchText"/>. When null,
    /// a non-blank <see cref="LovQuery.SearchText"/> is accepted but ignored.
    /// </param>
    /// <returns>A filtered, sorted, and paginated LovDataResult.</returns>
    public static LovDataResult<TItem> FromCollection(
        IEnumerable<TItem> items,
        LovQuery query,
        Func<TItem, string, bool>? searchPredicate)
    {
        var filteredItems = items;

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.SearchText) && searchPredicate != null)
        {
            var searchText = query.SearchText;
            filteredItems = filteredItems.Where(item => searchPredicate(item, searchText));
        }

        // Apply context filters (for cascading)
        foreach (var kvp in query.Context)
        {
            if (kvp.Value != null)
            {
                var property = typeof(TItem).GetProperty(kvp.Key);
                if (property != null)
                {
                    var filterValue = kvp.Value;
                    filteredItems = filteredItems.Where(item =>
                    {
                        var propValue = property.GetValue(item);
                        return propValue != null && propValue.Equals(filterValue);
                    });
                }
            }
        }

        // Materialize for count before sorting/paging
        var allItems = filteredItems.ToList();
        var totalCount = allItems.Count;

        // Apply sorting
        IEnumerable<TItem> sortedItems = allItems;
        foreach (var sort in query.SortDefinitions)
        {
            var property = typeof(TItem).GetProperty(sort.PropertyName);
            if (property != null)
            {
                sortedItems = sort.Descending
                    ? sortedItems.OrderByDescending(item => property.GetValue(item))
                    : sortedItems.OrderBy(item => property.GetValue(item));
            }
        }

        // Apply pagination
        var pagedItems = sortedItems
            .Skip(query.StartIndex)
            .Take(query.Count)
            .ToList();

        return new LovDataResult<TItem>(pagedItems, totalCount);
    }
}
