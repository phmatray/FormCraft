using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.ForFluentUI.Extensions;

/// <summary>
/// Provides extension methods for configuring Fluent UI Blazor support for FormCraft.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The assembly name of the MudBlazor adapter, used to detect a conflicting registration
    /// without taking a reference on that package.
    /// </summary>
    private const string MudBlazorAdapterAssembly = "FormCraft.ForMudBlazor";

    /// <param name="services">The IServiceCollection to add the services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Fluent UI Blazor (v5) UI framework support to FormCraft.
        /// </summary>
        /// <returns>The IServiceCollection for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the MudBlazor adapter is already registered.
        /// </exception>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddFormCraft();
        /// builder.Services.AddFormCraftFluentUI();
        /// </code>
        /// </example>
        public IServiceCollection AddFormCraftFluentUI()
        {
            // Renderer selection picks the FIRST IFieldRenderer whose CanRender matches, so a
            // container holding two adapters silently renders a form that is partly Material and
            // partly Fluent - no exception, just wrong output. Fail at registration instead.
            // Detected by assembly NAME so this package takes no reference on the MudBlazor one.
            if (services.Any(s => s.ServiceType == typeof(IFieldRenderer) &&
                                  s.ImplementationType?.Assembly.GetName().Name == MudBlazorAdapterAssembly))
            {
                throw new InvalidOperationException(
                    "FormCraft.ForMudBlazor is already registered. AddFormCraftMudBlazor() and " +
                    "AddFormCraftFluentUI() are mutually exclusive - register exactly one of them.");
            }

            // Remove only the core library's default renderers; renderers registered by the
            // application (custom IFieldRenderer implementations) must survive and keep precedence
            // over the Fluent UI defaults below.
            var coreAssembly = typeof(IFieldRenderer).Assembly;
            var defaultRendererDescriptors = services
                .Where(s => s.ServiceType == typeof(IFieldRenderer) &&
                            s.ImplementationType?.Assembly == coreAssembly)
                .ToList();
            foreach (var descriptor in defaultRendererDescriptors)
            {
                services.Remove(descriptor);
            }

            // Register Fluent UI-specific renderers. Renderer selection picks the FIRST renderer
            // whose CanRender matches, so configuration-driven renderers (select) must be
            // registered before the generic type-based ones or a string field carrying options
            // would always end up with the text renderer. Follow-ups adding LOV, lookup and
            // autocomplete must insert them above the type-based block too.
            services.AddScoped<IFieldRenderer, FluentUISelectFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUIDateTimeFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUIDateOnlyFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUITimeOnlyFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUIBooleanFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUINumericFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUITextFieldRenderer>();

            return services;
        }
    }
}
