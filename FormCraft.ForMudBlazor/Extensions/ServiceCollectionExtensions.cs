using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.ForMudBlazor.Extensions;

/// <summary>
/// Provides extension methods for configuring MudBlazor UI framework support for FormCraft.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MudBlazor UI framework support to FormCraft.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <returns>The IServiceCollection for method chaining.</returns>
    /// <example>
    /// <code>
    /// // In Program.cs
    /// builder.Services.AddFormCraft();
    /// builder.Services.AddFormCraftMudBlazor();
    /// </code>
    /// </example>
    public static IServiceCollection AddFormCraftMudBlazor(this IServiceCollection services)
    {
        // Remove all existing IFieldRenderer registrations to ensure MudBlazor renderers take precedence
        var fieldRendererDescriptors = services.Where(s => s.ServiceType == typeof(IFieldRenderer)).ToList();
        foreach (var descriptor in fieldRendererDescriptors)
        {
            services.Remove(descriptor);
        }
        
        // Register MudBlazor UI framework adapter
        services.AddSingleton<IUIFrameworkAdapter, MudBlazorUIFrameworkAdapter>();
        
        // Register MudBlazor-specific renderers
        services.AddScoped<IFieldRenderer, MudBlazorTextFieldRenderer>();
        services.AddScoped<IFieldRenderer, MudBlazorNumericFieldRenderer>();
        services.AddScoped<IFieldRenderer, MudBlazorBooleanFieldRenderer>();
        services.AddScoped<IFieldRenderer, MudBlazorDateTimeFieldRenderer>();
        services.AddScoped<IFieldRenderer, MudBlazorSelectFieldRenderer>();
        services.AddScoped<IFieldRenderer, MudBlazorFileUploadFieldRenderer>();
        services.AddScoped<IFieldRenderer, MudBlazorColorPickerRenderer>();
        services.AddScoped<IFieldRenderer, MudBlazorRatingRenderer>();
        
        return services;
    }
}