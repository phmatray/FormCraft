namespace FormCraft;

/// <summary>
/// Wrapper that resolves an ILovDataProvider from the DI container at runtime.
/// This allows for lazy resolution and scoped service usage.
/// </summary>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
internal class ServiceLovDataProvider<TItem> : ILovDataProvider<TItem>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Type _serviceType;

    /// <summary>
    /// Initializes a new instance of the ServiceLovDataProvider class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="serviceType">The type of the data provider service to resolve.</param>
    public ServiceLovDataProvider(IServiceProvider serviceProvider, Type serviceType)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }

    /// <summary>
    /// Gets the underlying data provider from the service container.
    /// </summary>
    /// <returns>The resolved data provider.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service cannot be resolved or doesn't implement ILovDataProvider.
    /// </exception>
    private ILovDataProvider<TItem> GetProvider()
    {
        var service = _serviceProvider.GetService(_serviceType);

        if (service is ILovDataProvider<TItem> provider)
        {
            return provider;
        }

        throw new InvalidOperationException(
            $"Service '{_serviceType.Name}' could not be resolved or does not implement " +
            $"ILovDataProvider<{typeof(TItem).Name}>. " +
            $"Ensure the service is registered in the DI container.");
    }

    /// <inheritdoc />
    public Task<LovDataResult<TItem>> GetItemsAsync(
        LovQuery query,
        CancellationToken cancellationToken = default)
    {
        return GetProvider().GetItemsAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TItem?> GetItemByKeyAsync(
        object key,
        CancellationToken cancellationToken = default)
    {
        return GetProvider().GetItemByKeyAsync(key, cancellationToken);
    }
}

/// <summary>
/// Factory for creating LOV data providers from various sources.
/// </summary>
public interface ILovDataProviderFactory
{
    /// <summary>
    /// Creates a data provider based on the LOV configuration.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the list.</typeparam>
    /// <typeparam name="TValue">The type of the selected value.</typeparam>
    /// <param name="configuration">The LOV configuration.</param>
    /// <returns>A configured data provider.</returns>
    ILovDataProvider<TItem> Create<TItem, TValue>(ILovConfiguration<TItem, TValue> configuration);
}

/// <summary>
/// Default implementation of ILovDataProviderFactory.
/// </summary>
public class LovDataProviderFactory : ILovDataProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the LovDataProviderFactory class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public LovDataProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public ILovDataProvider<TItem> Create<TItem, TValue>(ILovConfiguration<TItem, TValue> configuration)
    {
        // Priority 1: Direct data provider delegate
        if (configuration.DataProvider != null)
        {
            return new LambdaLovDataProvider<TItem>(
                configuration.DataProvider,
                null);
        }

        // Priority 2: Service type from DI
        if (configuration.DataProviderServiceType != null)
        {
            return new ServiceLovDataProvider<TItem>(
                _serviceProvider,
                configuration.DataProviderServiceType);
        }

        // Priority 3: JSON configuration (would require ILovConfigurationProvider)
        if (!string.IsNullOrEmpty(configuration.JsonConfigId))
        {
            // This would be handled by the REST client and JSON config provider
            // For now, throw a descriptive error
            throw new InvalidOperationException(
                $"JSON configuration '{configuration.JsonConfigId}' requires " +
                "ILovRestClient to be configured. Call AddFormCraftLov() in your service registration.");
        }

        throw new InvalidOperationException(
            "No data source configured for LOV. Use WithDataSource(), WithDataService<T>(), or WithJsonConfig().");
    }
}
