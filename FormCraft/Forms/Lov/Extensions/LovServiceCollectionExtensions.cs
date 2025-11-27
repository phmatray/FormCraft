using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FormCraft;

/// <summary>
/// Extension methods for registering LOV (List of Values) services in the dependency injection container.
/// </summary>
public static class LovServiceCollectionExtensions
{
    /// <summary>
    /// Adds LOV (List of Values) services to the service collection.
    /// This enables modal table selection for lookup fields.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure LOV options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Basic registration
    /// services.AddFormCraftLov();
    ///
    /// // With configuration
    /// services.AddFormCraftLov(options =>
    /// {
    ///     options.ConfigurationPath = "wwwroot/lov-config";
    ///     options.DefaultSearchDebounceMs = 300;
    ///     options.AllowedBaseUrls.Add("https://api.example.com");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddFormCraftLov(
        this IServiceCollection services,
        Action<LovServiceOptions>? configure = null)
    {
        var options = new LovServiceOptions();
        configure?.Invoke(options);

        // Register options
        services.Configure<LovServiceOptions>(opt =>
        {
            opt.ConfigurationPath = options.ConfigurationPath;
            opt.EnableHotReload = options.EnableHotReload;
            opt.DefaultSearchDebounceMs = options.DefaultSearchDebounceMs;
            opt.DefaultPageSize = options.DefaultPageSize;
            opt.AllowedBaseUrls = options.AllowedBaseUrls;
        });

        // Register core services
        services.TryAddScoped<ILovDataProviderFactory, LovDataProviderFactory>();
        services.TryAddScoped<ILovMappingProcessor, LovMappingProcessor>();

        return services;
    }

    /// <summary>
    /// Registers a typed LOV data service in the DI container.
    /// </summary>
    /// <typeparam name="TService">The service implementation type.</typeparam>
    /// <typeparam name="TItem">The type of items the service provides.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddLovDataService&lt;CustomerLovService, Customer&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddLovDataService<TService, TItem>(
        this IServiceCollection services)
        where TService : class, ILovDataService<TItem>
    {
        services.AddScoped<TService>();
        services.AddScoped<ILovDataService<TItem>>(sp => sp.GetRequiredService<TService>());
        services.AddScoped<ILovDataProvider<TItem>>(sp => sp.GetRequiredService<TService>());
        return services;
    }

    /// <summary>
    /// Registers an LOV data service with a factory function.
    /// </summary>
    /// <typeparam name="TItem">The type of items the service provides.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the service.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLovDataService<TItem>(
        this IServiceCollection services,
        Func<IServiceProvider, ILovDataProvider<TItem>> factory)
    {
        services.AddScoped(factory);
        return services;
    }
}

/// <summary>
/// Configuration options for LOV services.
/// </summary>
public class LovServiceOptions
{
    /// <summary>
    /// Gets or sets the path to the directory containing LOV JSON configuration files.
    /// Default is "wwwroot/lov-config".
    /// </summary>
    public string ConfigurationPath { get; set; } = "wwwroot/lov-config";

    /// <summary>
    /// Gets or sets whether to enable automatic reload when configuration files change.
    /// Default is true.
    /// </summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// Gets or sets the default debounce delay for search in milliseconds.
    /// Default is 300ms.
    /// </summary>
    public int DefaultSearchDebounceMs { get; set; } = 300;

    /// <summary>
    /// Gets or sets the default page size for data loading.
    /// Default is 50.
    /// </summary>
    public int DefaultPageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the list of allowed base URLs for LOV REST endpoints.
    /// This is used for security validation.
    /// </summary>
    public List<string> AllowedBaseUrls { get; set; } = [];
}
