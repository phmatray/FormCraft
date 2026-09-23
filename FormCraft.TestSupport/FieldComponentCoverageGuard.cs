using System.Reflection;

namespace FormCraft.TestSupport;

/// <summary>
/// One field-type component with neither a configuration-refresh parity row nor an explicit
/// "nothing to refresh" exemption (#349).
/// </summary>
/// <param name="Component">The component type missing coverage.</param>
public sealed record CoverageGap(Type Component)
{
    public override string ToString() => Component.Name;
}

/// <summary>
/// The rule <c>FieldConfigurationParityTests</c> is supposed to hold in each adapter — <i>every</i>
/// field-type component either has a parity row proving it honours the configuration-refresh hook
/// (<see cref="FieldComponentBase{TModel, TValue}.OnFieldConfigurationChanged"/>) or is named as
/// genuinely having nothing to refresh — expressed as something the build can check (#349).
/// </summary>
/// <remarks>
/// <para>
/// Coverage here has repeatedly been added by hand, component by component, and repeatedly missed
/// one: MudBlazor's lookup display text, its LOV selection, its password-visible flag, its own
/// diagnostic-emitted flag (#308); Fluent's lookup display text, its LOV selection, its
/// open/rows/search-text triple, its autocomplete options and selection (#336). Every one of those
/// was caught by a human review pass rather than by a test, because the refresh suites only ever
/// covered the component someone happened to write a test for. This guard turns "did we remember
/// every component" into a build failure instead of a review habit.
/// </para>
/// <para>
/// <b>Ownership is derived, never listed.</b> A "field-type component" is any concrete class whose
/// base type chain reaches the open generic <c>FieldComponentBase&lt;,&gt;</c> — the same hook every
/// per-field component in either adapter inherits (core's <c>FieldComponentBase</c>, specialised per
/// adapter by <c>MudBlazorFieldComponentBase</c> / <c>FluentUIFieldComponentBase</c>). A component
/// added tomorrow is in the universe by construction; nothing to remember, nothing to enrol.
/// </para>
/// <para>
/// <b>Two ways to satisfy the guard</b>, both supplied by the caller: <paramref
/// name="covered">covered</paramref> types, whose parity row is in the suite, and <paramref
/// name="exempt">exempt</paramref> types, which genuinely cache no configuration-derived state (the
/// three Fluent date components, and the shipped example custom renderers that never override the
/// hook at all). Both are asserted against by name at the call site — see
/// <c>FieldConfigurationParityTests</c> in each adapter — so an exemption is a deliberate, reviewed
/// claim rather than a silent omission.
/// </para>
/// </remarks>
public static class FieldComponentCoverageGuard
{
    /// <summary>
    /// Every concrete field-type component declared in <paramref name="assembly"/>.
    /// </summary>
    /// <remarks>
    /// A partially-unloadable assembly degrades to "scan what loaded" rather than throwing — a check
    /// meant to be trusted must not turn a dependency bump into an opaque type-load stack trace that
    /// says nothing about the rule it enforces (mirrors <c>CollectionItemShapeGuard.TestAssemblyTypes</c>).
    /// </remarks>
    public static IEnumerable<Type> FieldComponentTypes(Assembly assembly)
    {
        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }

        return types
            .Where(t => t is not null)
            .Select(t => t!)
            .Where(t => t.IsClass && !t.IsAbstract && DerivesFromFieldComponentBase(t));
    }

    /// <summary>
    /// Whether <paramref name="type"/>'s base-type chain reaches the open generic
    /// <c>FieldComponentBase&lt;,&gt;</c>. Walking the chain (rather than checking <c>BaseType</c>
    /// alone) is load-bearing: every real component sits two levels below it, behind its adapter's own
    /// <c>MudBlazorFieldComponentBase</c> / <c>FluentUIFieldComponentBase</c>.
    /// </summary>
    private static bool DerivesFromFieldComponentBase(Type type)
    {
        var openBase = typeof(FieldComponentBase<,>);

        for (var candidate = type.BaseType; candidate is not null; candidate = candidate.BaseType)
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openBase)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Every field-type component among <paramref name="componentTypes"/> that is neither
    /// <paramref name="covered"/> nor <paramref name="exempt"/>.
    /// </summary>
    /// <remarks>
    /// Takes its input rather than reading the assembly itself so it can be exercised with a
    /// <i>known-bad</i> set — a guard whose detection path never runs against an offender is a guard
    /// that can quietly stop detecting, which is exactly the failure mode #308 and #336 shipped and
    /// this whole issue exists to prevent.
    /// </remarks>
    public static IReadOnlyList<CoverageGap> FindGaps(
        IEnumerable<Type> componentTypes,
        IReadOnlySet<Type> covered,
        IReadOnlySet<Type> exempt) =>
        componentTypes
            .Where(t => !covered.Contains(t) && !exempt.Contains(t))
            .Select(t => new CoverageGap(t))
            .OrderBy(g => g.Component.Name, StringComparer.Ordinal)
            .ToList();
}
