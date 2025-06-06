using Microsoft.Extensions.DependencyInjection;
using DynamicFormBlazor.Forms.Rendering;

namespace DynamicFormBlazor.Forms.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDynamicForms(this IServiceCollection services)
    {
        services.AddScoped<IFieldRendererService, FieldRendererService>();
        services.AddScoped<IFieldRenderer, StringFieldRenderer>();
        services.AddScoped<IFieldRenderer, IntFieldRenderer>();
        services.AddScoped<IFieldRenderer, BoolFieldRenderer>();
        services.AddScoped<IFieldRenderer, DateTimeFieldRenderer>();
        
        return services;
    }
}