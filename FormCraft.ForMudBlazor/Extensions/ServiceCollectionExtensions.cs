using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.ForMudBlazor.Extensions;

/// <summary>
/// Provides extension methods for configuring MudBlazor UI framework support for FormCraft.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="services">The IServiceCollection to add the services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds MudBlazor UI framework support to FormCraft.
        /// </summary>
        /// <returns>The IServiceCollection for method chaining.</returns>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddFormCraft();
        /// builder.Services.AddFormCraftMudBlazor();
        /// </code>
        /// </example>
        public IServiceCollection AddFormCraftMudBlazor()
        {
            // Remove only the core library's default renderers; renderers registered
            // by the application (custom IFieldRenderer implementations) must survive
            // and keep precedence over the MudBlazor defaults below.
            var coreAssembly = typeof(IFieldRenderer).Assembly;
            var defaultRendererDescriptors = services
                .Where(s => s.ServiceType == typeof(IFieldRenderer) &&
                            s.ImplementationType?.Assembly == coreAssembly)
                .ToList();
            foreach (var descriptor in defaultRendererDescriptors)
            {
                services.Remove(descriptor);
            }

            // Register MudBlazor UI framework adapter
            services.AddSingleton<IUIFrameworkAdapter, MudBlazorUIFrameworkAdapter>();

            // Register MudBlazor-specific renderers. Renderer selection picks the FIRST
            // renderer whose CanRender matches, so configuration-driven renderers (LOV,
            // lookup, autocomplete, select, file upload) must be registered before the
            // generic type-based ones or a string/int field would always end up with
            // the text/numeric renderer regardless of its configuration.
            services.AddScoped<IFieldRenderer, MudBlazorLovFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorLookupFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorAutocompleteFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorSelectFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorMultipleFileUploadRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorFileUploadFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorDateTimeFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorBooleanFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorNumericFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorTextFieldRenderer>();
            // Note: MudBlazorColorPickerRenderer and MudBlazorRatingRenderer are custom renderers,
            // not IFieldRenderer implementations. They should be used via WithCustomRenderer().

            return services;
        }
    }
}