using Microsoft.Extensions.DependencyInjection;

namespace FormCraft;

/// <summary>
/// Records which UI adapter has claimed a container, and refuses a second one.
/// </summary>
/// <remarks>
/// <para>
/// The rule this enforces concerns two adapters at once, so it cannot live inside either of them: a
/// check written in one package only ever fires in one registration order. Until #279 exactly that
/// was true — <c>AddFormCraftFluentUI()</c> refused to register alongside MudBlazor while the
/// reverse order slipped through silently, producing a container that rendered some fields Material
/// and some Fluent with nothing to point at. Putting the rule in core makes it symmetric by
/// construction rather than by two packages remembering to agree.
/// </para>
/// <para>
/// Adapters identify themselves by a name they pass in, rather than being detected by scanning the
/// assemblies behind registered renderers. Detection by assembly name looks equivalent and is not:
/// a naming rule loose enough to match unknown adapters (<c>FormCraft.For*</c>) also matches an
/// application's own assembly, and a custom <see cref="IFieldRenderer"/> registered from, say,
/// <c>FormCraft.ForFluentUI.UnitTests</c> then reads as a rival adapter and blocks a legitimate
/// registration. Self-identification has no such failure mode and needs no reference between
/// adapter packages either.
/// </para>
/// </remarks>
public static class AdapterRegistration
{
    /// <summary>
    /// Marks a container as claimed by one UI adapter. Registered by
    /// <see cref="EnsureSingleAdapter"/>; not constructible from outside FormCraft.
    /// </summary>
    public sealed class AdapterMarker
    {
        internal AdapterMarker(string assemblyName) => AssemblyName = assemblyName;

        /// <summary>The simple assembly name of the adapter that claimed the container.</summary>
        public string AssemblyName { get; }
    }

    /// <summary>
    /// Throws when a <i>different</i> FormCraft UI adapter has already claimed
    /// <paramref name="services"/>, and otherwise records this one as the container's adapter. Call
    /// it first thing in every <c>AddFormCraft&lt;Framework&gt;()</c> method.
    /// </summary>
    /// <param name="services">The container being configured.</param>
    /// <param name="registeringAssemblyName">
    /// The simple assembly name of the adapter doing the registering (e.g.
    /// <c>"FormCraft.ForMudBlazor"</c>). Re-registering the same adapter is not a conflict with
    /// itself, so calling an <c>AddFormCraft&lt;Framework&gt;()</c> method twice stays legal.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// A different FormCraft adapter is already registered. Renderer selection is first-match-wins,
    /// so a container holding two adapters renders a form that is partly one framework and partly
    /// the other — with no exception and nothing to point at. Registration is the only place that
    /// can be caught.
    /// </exception>
    public static void EnsureSingleAdapter(IServiceCollection services, string registeringAssemblyName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(registeringAssemblyName);

        var claimed = services
            .Where(descriptor => descriptor.ServiceType == typeof(AdapterMarker))
            .Select(descriptor => (descriptor.ImplementationInstance as AdapterMarker)?.AssemblyName)
            .FirstOrDefault(name => name is not null);

        if (claimed is not null)
        {
            if (string.Equals(claimed, registeringAssemblyName, StringComparison.Ordinal))
            {
                // Same adapter registering again — idempotent, and not a conflict with itself.
                return;
            }

            throw new InvalidOperationException(
                $"{claimed} is already registered, so {registeringAssemblyName} cannot be added to " +
                "the same container. FormCraft UI adapters are mutually exclusive - register " +
                "exactly one of them. Renderer selection is first-match-wins, so a container " +
                "holding two adapters renders some fields with one framework and some with the " +
                "other, with no error to point at.");
        }

        services.AddSingleton(new AdapterMarker(registeringAssemblyName));
    }

    /// <summary>
    /// Whether a UI adapter has already claimed <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The container to inspect.</param>
    /// <returns><c>true</c> when an adapter has registered itself via <see cref="EnsureSingleAdapter"/>.</returns>
    /// <remarks>
    /// Core's <c>AddFormCraft()</c> uses this to decide whether to register its built-in renderers,
    /// which an adapter would only strip out again.
    /// </remarks>
    public static bool IsAdapterRegistered(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.Any(descriptor => descriptor.ServiceType == typeof(AdapterMarker));
    }
}
