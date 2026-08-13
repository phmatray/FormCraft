using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.ForFluentUI.Extensions;

/// <summary>
/// Provides extension methods for configuring Fluent UI Blazor support for FormCraft.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// This adapter's own assembly name, handed to
    /// <see cref="AdapterRegistration.EnsureSingleAdapter"/> so the scan excludes it and a repeated
    /// <c>AddFormCraftFluentUI()</c> is not read as a conflict with itself.
    /// </summary>
    private const string ThisAdapterAssembly = "FormCraft.ForFluentUI";

    /// <param name="services">The IServiceCollection to add the services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Fluent UI Blazor (v5) UI framework support to FormCraft.
        /// </summary>
        /// <returns>The IServiceCollection for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when another FormCraft UI adapter is already registered.
        /// </exception>
        /// <remarks>
        /// The guard is symmetric since #279: both adapters call the same
        /// <see cref="AdapterRegistration.EnsureSingleAdapter"/> in core, so either registration
        /// order fails identically. It used to live only here, which meant
        /// <c>AddFormCraftFluentUI()</c> followed by <c>AddFormCraftMudBlazor()</c> threw nothing
        /// and produced exactly the mixed container it exists to prevent.
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
            // The rule lives in core (#279) and is called from both adapters, so it fires in either
            // registration order; a copy here caught only Mud-then-Fluent.
            AdapterRegistration.EnsureSingleAdapter(services, ThisAdapterAssembly);

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
