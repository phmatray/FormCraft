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
    /// builder.Services.AddDynamicForms();
    /// </code>
    /// </example>
    /// <remarks>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item>IFieldRendererService - Coordinates field rendering</item>
    /// <item>StringFieldRenderer - Handles text input fields</item>
    /// <item>IntFieldRenderer - Handles numeric input fields</item>
    /// <item>DecimalFieldRenderer - Handles decimal input fields</item>
    /// <item>DoubleFieldRenderer - Handles double/float input fields</item>
    /// <item>BoolFieldRenderer - Handles checkbox and switch fields</item>
    /// <item>DateTimeFieldRenderer - Handles date and time picker fields</item>
    /// <item>FileUploadFieldRenderer - Handles file upload fields</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddDynamicForms(this IServiceCollection services)
    {
        services.AddScoped<IFieldRendererService, FieldRendererService>();
        services.AddScoped<IFieldRenderer, StringFieldRenderer>();
        services.AddScoped<IFieldRenderer, IntFieldRenderer>();
        services.AddScoped<IFieldRenderer, DecimalFieldRenderer>();
        services.AddScoped<IFieldRenderer, DoubleFieldRenderer>();
        services.AddScoped<IFieldRenderer, BoolFieldRenderer>();
        services.AddScoped<IFieldRenderer, DateTimeFieldRenderer>();
        services.AddScoped<IFieldRenderer, FileUploadFieldRenderer>();

        return services;
    }
}