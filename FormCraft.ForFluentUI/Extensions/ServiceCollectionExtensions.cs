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
        /// Thrown when the MudBlazor adapter is <b>already</b> registered.
        /// </exception>
        /// <remarks>
        /// ⚠️ The guard is one-directional: it inspects the container at the moment it runs, so
        /// calling <c>AddFormCraftFluentUI()</c> and <b>then</b> <c>AddFormCraftMudBlazor()</c>
        /// throws nothing and produces exactly the mixed container it exists to prevent. Closing
        /// that needs a matching check in the MudBlazor package.
        /// </remarks>
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
            // whose CanRender matches, so ORDER IS BEHAVIOUR, not tidiness:
            //
            //  1. Configuration-driven renderers (LOV, lookup, autocomplete, multi-select, select)
            //     come first. A string field carrying options or a search function is still a
            //     string, so registered after the text renderer it would silently render as a
            //     plain text box with its configuration ignored.
            //  2. The multiple-file renderer precedes the single-file one: a
            //     List<IBrowserFile> field satisfies both predicates.
            //  3. The type-based block comes last, most specific first.
            services.AddScoped<IFieldRenderer, FluentUILovFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUILookupFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUIAutocompleteFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUIMultiSelectFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUISelectFieldRenderer>();
            services.AddScoped<IFieldRenderer, FluentUIMultipleFileUploadRenderer>();
            services.AddScoped<IFieldRenderer, FluentUIFileUploadFieldRenderer>();
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
