using Microsoft.Extensions.DependencyInjection;

namespace FormCraft;

/// <summary>
/// Provides extension methods for configuring Dynamic Form Blazor services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all necessary services for Dynamic Form Blazor in the dependency injection container.
    /// This includes field renderers for common data types and the field renderer service.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
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
    public static IServiceCollection AddFormCraft(this IServiceCollection services)
    {
        // Register field renderer service
        services.AddScoped<IFieldRendererService, FieldRendererService>();

        // Register UI framework configuration
        services.AddSingleton<UIFrameworkConfiguration>();

        // Only register built-in field renderers if no UI framework adapter is registered
        // This allows UI framework-specific renderers to take precedence
        services.AddScoped<IFieldRenderer, StringFieldRenderer>();
        services.AddScoped<IFieldRenderer, IntFieldRenderer>();
        services.AddScoped<IFieldRenderer, DecimalFieldRenderer>();
        services.AddScoped<IFieldRenderer, DoubleFieldRenderer>();
        services.AddScoped<IFieldRenderer, BoolFieldRenderer>();
        services.AddScoped<IFieldRenderer, DateTimeFieldRenderer>();
        services.AddScoped<IFieldRenderer, FileUploadFieldRenderer>();

        // Register security services
        services.AddScoped<IEncryptionService, BlazorEncryptionService>();
        services.AddScoped<ICsrfTokenService, BlazorCsrfTokenService>();
        services.AddSingleton<IRateLimitService, InMemoryRateLimitService>();
        services.AddScoped<IAuditLogService, ConsoleAuditLogService>();

        return services;
    }
}