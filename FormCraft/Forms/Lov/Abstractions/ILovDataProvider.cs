namespace FormCraft;

/// <summary>
/// Provides data for List of Values (LOV) fields with support for async loading,
/// search, pagination, and filtering.
/// </summary>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
/// <example>
/// <code>
/// // Implementing a custom LOV data provider
/// public class CustomerLovDataProvider : ILovDataProvider&lt;Customer&gt;
/// {
///     private readonly ICustomerService _customerService;
///
///     public CustomerLovDataProvider(ICustomerService customerService)
///     {
///         _customerService = customerService;
///     }
///
///     public async Task&lt;LovDataResult&lt;Customer&gt;&gt; GetItemsAsync(
///         LovQuery query, CancellationToken ct)
///     {
///         var customers = await _customerService.SearchAsync(
///             query.SearchText,
///             query.StartIndex,
///             query.Count,
///             ct);
///
///         return new LovDataResult&lt;Customer&gt;(customers.Items, customers.TotalCount);
///     }
///
///     public async Task&lt;Customer?&gt; GetItemByKeyAsync(object key, CancellationToken ct)
///     {
///         if (key is int id)
///             return await _customerService.GetByIdAsync(id, ct);
///         return null;
///     }
/// }
/// </code>
/// </example>
public interface ILovDataProvider<TItem>
{
    /// <summary>
    /// Retrieves items based on the query parameters.
    /// This method is called when loading data for the LOV grid.
    /// </summary>
    /// <param name="query">The query parameters including search, pagination, and filters.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A result containing the items and total count.</returns>
    Task<LovDataResult<TItem>> GetItemsAsync(
        LovQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single item by its key value.
    /// This is used for displaying the selected value when the modal is closed.
    /// </summary>
    /// <param name="key">The key value of the item to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The item if found, otherwise null.</returns>
    Task<TItem?> GetItemByKeyAsync(
        object key,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface for LOV data providers that should be registered in dependency injection.
/// Implement this interface in your application services for DI-based data providers.
/// </summary>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
/// <example>
/// <code>
/// // Register in DI
/// services.AddScoped&lt;ILovDataService&lt;Employee&gt;, EmployeeLovService&gt;();
///
/// // Use in form configuration
/// .AddField(x => x.EmployeeId, field => field
///     .AsLov&lt;Model, int, Employee&gt;(lov => lov
///         .WithDataService&lt;ILovDataService&lt;Employee&gt;&gt;()))
/// </code>
/// </example>
public interface ILovDataService<TItem> : ILovDataProvider<TItem>
{
}
