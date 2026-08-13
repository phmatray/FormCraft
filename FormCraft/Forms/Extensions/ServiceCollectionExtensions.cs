using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FormCraft;

/// <summary>
/// Provides extension methods for configuring Dynamic Form Blazor services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="services">The IServiceCollection to add the services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers all necessary services for Dynamic Form Blazor in the dependency injection container.
        /// This includes field renderers for common data types and the field renderer service.
        /// </summary>
        /// <returns>The IServiceCollection for method chaining.</returns>
        /// <example>
        /// <code>
        /// // In Program.cs or Startup.cs
        /// builder.Services.AddFormCraft();
        /// </code>
        /// </example>
        /// <remarks>
        /// This method registers the following services:
        /// <list type="bullet">
        /// <item>IFieldRendererService - Coordinates field rendering</item>
        /// <item>Built-in field renderers for common types</item>
        /// </list>
        /// </remarks>
        public IServiceCollection AddFormCraft()
        {
            // Register field renderer service
            services.AddScoped<IFieldRendererService, FieldRendererService>();

            // Only register built-in field renderers if no UI framework adapter is registered.
            // This allows UI framework-specific renderers to take precedence.
            //
            // The question used to be asked as "is an IUIFrameworkAdapter registered?", which
            // happened to work only because AddFormCraftMudBlazor() registered one — that interface
            // had no consumers otherwise and was deleted in #279. Adapters now say so explicitly
            // through AdapterRegistration, so the test is about the thing it actually means.
            if (!AdapterRegistration.IsAdapterRegistered(services))
            {
                services.AddScoped<IFieldRenderer, StringFieldRenderer>();
                services.AddScoped<IFieldRenderer, IntFieldRenderer>();
                services.AddScoped<IFieldRenderer, DecimalFieldRenderer>();
                services.AddScoped<IFieldRenderer, DoubleFieldRenderer>();
                services.AddScoped<IFieldRenderer, BoolFieldRenderer>();
                services.AddScoped<IFieldRenderer, DateTimeFieldRenderer>();
                services.AddScoped<IFieldRenderer, FileUploadFieldRenderer>();
            }

            // Register security services (TryAdd so hosts can override with their own implementations).
            // AES-based DefaultEncryptionService is the default; the XOR-based BlazorEncryptionService
            // is only used on browser/WebAssembly where the AES APIs are unavailable.
            if (OperatingSystem.IsBrowser())
            {
                services.TryAddScoped<IEncryptionService, BlazorEncryptionService>();
            }
            else
            {
                services.TryAddScoped<IEncryptionService, DefaultEncryptionService>();
            }

            services.TryAddScoped<ICsrfTokenService, BlazorCsrfTokenService>();
            services.TryAddSingleton<IRateLimitService, InMemoryRateLimitService>();
            services.TryAddScoped<IAuditLogService, ConsoleAuditLogService>();

            return services;
        }
    }
}
