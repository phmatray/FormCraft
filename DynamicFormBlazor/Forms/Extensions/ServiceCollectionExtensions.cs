using Microsoft.Extensions.DependencyInjection;

namespace DynamicFormBlazor.Forms.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDynamicForms(this IServiceCollection services)
    {
        // Register any services needed for the dynamic forms
        return services;
    }
}