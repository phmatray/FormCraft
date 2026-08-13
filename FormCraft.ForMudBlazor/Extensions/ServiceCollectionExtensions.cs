using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.ForMudBlazor.Extensions;

/// <summary>
/// Provides extension methods for configuring MudBlazor UI framework support for FormCraft.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// This adapter's own assembly name, handed to
    /// <see cref="AdapterRegistration.EnsureSingleAdapter"/> so the scan excludes it and a repeated
    /// <c>AddFormCraftMudBlazor()</c> is not read as a conflict with itself.
    /// </summary>
    private const string ThisAdapterAssembly = "FormCraft.ForMudBlazor";

    /// <param name="services">The IServiceCollection to add the services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds MudBlazor UI framework support to FormCraft.
        /// </summary>
        /// <returns>The IServiceCollection for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when another FormCraft UI adapter is already registered.
        /// </exception>
        /// <remarks>
        /// The guard is symmetric since #279: both adapters call the same
        /// <see cref="AdapterRegistration.EnsureSingleAdapter"/> in core, so either registration
        /// order fails identically. Before that it existed only in the Fluent package, so this
        /// order — Fluent first, then MudBlazor — silently produced a half-Material form.
        /// </remarks>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddFormCraft();
        /// builder.Services.AddFormCraftMudBlazor();
        /// </code>
        /// </example>
        public IServiceCollection AddFormCraftMudBlazor()
        {
            // Fail here rather than render a container holding two adapters, which resolves
            // first-match-wins and produces a form that is partly Material and partly the other
            // framework, with no error to point at.
            AdapterRegistration.EnsureSingleAdapter(services, ThisAdapterAssembly);

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

            // Register MudBlazor-specific renderers. Renderer selection picks the FIRST
            // renderer whose CanRender matches, so configuration-driven renderers (LOV,
            // lookup, autocomplete, select, file upload) must be registered before the
            // generic type-based ones or a string/int field would always end up with
            // the text/numeric renderer regardless of its configuration.
            services.AddScoped<IFieldRenderer, MudBlazorLovFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorLookupFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorAutocompleteFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorMultiSelectFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorSelectFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorMultipleFileUploadRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorFileUploadFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorDateTimeFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorDateOnlyFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorTimeOnlyFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorBooleanFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorNumericFieldRenderer>();
            services.AddScoped<IFieldRenderer, MudBlazorTextFieldRenderer>();
            // Note: MudBlazorColorPickerRenderer and MudBlazorRatingRenderer are custom renderers,
            // not IFieldRenderer implementations. They should be used via WithCustomRenderer().

            return services;
        }
    }
}
