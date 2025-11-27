namespace FormCraft;

/// <summary>
/// LOV data provider that uses lambda functions for data retrieval.
/// Ideal for small datasets, in-memory collections, or simple use cases.
/// </summary>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
/// <example>
/// <code>
/// // Create from a collection
/// var provider = LambdaLovDataProvider&lt;Product&gt;.FromCollection(
///     () => products,
///     p => p.Id);
///
/// // Create with full async support
/// var provider = new LambdaLovDataProvider&lt;Product&gt;(
///     async (query, ct) => await productService.SearchAsync(query, ct),
///     async (key, ct) => await productService.GetByIdAsync((int)key, ct));
/// </code>
/// </example>
public class LambdaLovDataProvider<TItem> : ILovDataProvider<TItem>
{
    private readonly Func<LovQuery, CancellationToken, Task<LovDataResult<TItem>>> _dataFunc;
    private readonly Func<object, CancellationToken, Task<TItem?>>? _getByKeyFunc;

    /// <summary>
    /// Initializes a new instance of the LambdaLovDataProvider class.
    /// </summary>
    /// <param name="dataFunc">Function to retrieve items based on query.</param>
    /// <param name="getByKeyFunc">Optional function to retrieve a single item by key.</param>
    public LambdaLovDataProvider(
        Func<LovQuery, CancellationToken, Task<LovDataResult<TItem>>> dataFunc,
        Func<object, CancellationToken, Task<TItem?>>? getByKeyFunc = null)
    {
        _dataFunc = dataFunc ?? throw new ArgumentNullException(nameof(dataFunc));
        _getByKeyFunc = getByKeyFunc;
    }

    /// <summary>
    /// Creates a data provider from a synchronous collection factory.
    /// The provider will handle pagination, search, and sorting client-side.
    /// </summary>
    /// <param name="collectionFactory">Function that returns the collection of items.</param>
    /// <param name="keySelector">Function to extract the key from an item.</param>
    /// <param name="searchPredicate">Optional custom search predicate.</param>
    /// <returns>A configured LambdaLovDataProvider.</returns>
    public static LambdaLovDataProvider<TItem> FromCollection(
        Func<IEnumerable<TItem>> collectionFactory,
        Func<TItem, object> keySelector,
        Func<TItem, string, bool>? searchPredicate = null)
    {
        return new LambdaLovDataProvider<TItem>(
            (query, ct) =>
            {
                IEnumerable<TItem> items = collectionFactory();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(query.SearchText) && searchPredicate != null)
                {
                    var searchText = query.SearchText;
                    items = items.Where(item => searchPredicate(item, searchText));
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
                            items = items.Where(item =>
                            {
                                var propValue = property.GetValue(item);
                                return propValue != null && propValue.Equals(filterValue);
                            });
                        }
                    }
                }

                // Materialize for count before sorting/paging
                var itemsList = items.ToList();
                var totalCount = itemsList.Count;

                // Apply sorting
                IEnumerable<TItem> sortedItems = itemsList;
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

                return Task.FromResult(new LovDataResult<TItem>(pagedItems, totalCount));
            },
            (key, ct) =>
            {
                var item = collectionFactory()
                    .FirstOrDefault(i =>
                    {
                        var itemKey = keySelector(i);
                        return itemKey != null && itemKey.Equals(key);
                    });
                return Task.FromResult(item);
            });
    }

    /// <summary>
    /// Creates a data provider from an async data source with simplified signature.
    /// </summary>
    /// <param name="dataFunc">Async function to retrieve all items.</param>
    /// <param name="keySelector">Function to extract the key from an item.</param>
    /// <returns>A configured LambdaLovDataProvider.</returns>
    public static LambdaLovDataProvider<TItem> FromAsyncSource(
        Func<CancellationToken, Task<IEnumerable<TItem>>> dataFunc,
        Func<TItem, object> keySelector)
    {
        return new LambdaLovDataProvider<TItem>(
            async (query, ct) =>
            {
                var items = (await dataFunc(ct)).ToList();
                return LovDataResult<TItem>.FromCollection(items, query);
            },
            async (key, ct) =>
            {
                var items = await dataFunc(ct);
                return items.FirstOrDefault(i => keySelector(i)?.Equals(key) == true);
            });
    }

    /// <inheritdoc />
    public Task<LovDataResult<TItem>> GetItemsAsync(
        LovQuery query,
        CancellationToken cancellationToken = default)
    {
        return _dataFunc(query, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TItem?> GetItemByKeyAsync(
        object key,
        CancellationToken cancellationToken = default)
    {
        if (_getByKeyFunc == null)
        {
            return Task.FromResult<TItem?>(default);
        }

        return _getByKeyFunc(key, cancellationToken);
    }
}
