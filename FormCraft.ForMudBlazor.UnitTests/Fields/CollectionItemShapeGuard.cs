using System.Reflection;
using System.Runtime.CompilerServices;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// The rule <see cref="CollectionItemFixture"/> is supposed to hold — <i>no test file declares a
/// private copy of a collection-item model the fixture provides</i> — expressed as something the
/// build can check (#297).
/// </summary>
/// <remarks>
/// <para>
/// Three issues attacked this defect before the rule was mechanical, and each found instances the
/// previous one's check could not see. #205 extracted the fixture because every suite carried its own
/// copies and they had drifted. #258 migrated eleven suites and closed with a grep over <b>six type
/// names</b>; a suite modelling the same shape under a different vocabulary — <c>Credential.Secret</c>,
/// <c>VaultEntry.Secret</c>, <c>MixedRow.Name</c> — was invisible to it. #282 found those five by hand.
/// </para>
/// <para>
/// ⚠️ <b>The failure mode is silent, which is why a test rather than review has to catch it.</b> A
/// nested <c>private class OrderItem</c> <i>shadows</i> the namespace-scope one this fixture declares,
/// so a half-finished migration compiles clean and passes green. There is no compile error to lean on
/// — #282 had to enumerate per-suite test counts from the built dll to prove its migration had landed.
/// </para>
/// <para>
/// <b>The allowlist is the feature, not the escape hatch.</b> A suite that genuinely needs a local
/// model adds it to <see cref="AllowedLocalModels"/> <i>with a reason</i>, which turns "why does this
/// copy exist?" from an archaeology question into a line a reviewer reads. That is exactly what #258
/// lacked: its check encoded which names it had already migrated, never what the rule was.
/// </para>
/// </remarks>
internal static class CollectionItemShapeGuard
{
    /// <summary>
    /// The models that may legitimately declare a collection-item shape of their own, each with the
    /// reason it is not a copy. Adding an entry is a deliberate act; a reviewer should be able to
    /// judge it from the reason alone.
    /// </summary>
    /// <remarks>
    /// Keyed by <see cref="Type"/> rather than by name so a rename cannot silently orphan an entry
    /// and re-hide a copy — the exact class of failure #258's name-based grep represents.
    /// </remarks>
    internal static readonly IReadOnlyDictionary<Type, string> AllowedLocalModels =
        new Dictionary<Type, string>
        {
            [typeof(CollectionNumericTypeTests).GetNestedType("NumericsRow", BindingFlags.NonPublic)!] =
                "Seven numeric types in one row (int/decimal/double/float/long/short/byte). That breadth " +
                "IS the suite's subject (#209): RenderItemField dispatched on four types while the " +
                "numeric renderer accepted eight, so an item field of the other four rendered no frames " +
                "at all. The fixture supplies two of the seven, and absorbing the rest is the " +
                "row-combinator #282 rejected as approach C.",
        };

    /// <summary>
    /// A type's shape as a multiset of its public instance property types — order-insensitive, and
    /// deliberately blind to names.
    /// </summary>
    /// <remarks>
    /// Names are excluded because names are what #258's check compared, and comparing them is what let
    /// <c>Credential</c> and <c>VaultEntry</c> pass while modelling <c>OrderItem</c>'s exact shape.
    /// Ordering is normalised so a copy cannot evade the guard by declaring its members in a different
    /// sequence.
    /// </remarks>
    internal static string ShapeSignature(Type itemType) =>
        string.Join(
            ", ",
            itemType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Select(p => FriendlyName(p.PropertyType))
                .OrderBy(name => name, StringComparer.Ordinal));

    /// <summary>
    /// Every type declared in the test assembly, including nested and non-public ones, minus the
    /// compiler's own.
    /// </summary>
    /// <remarks>
    /// The nested/non-public part is load-bearing: every model this guard exists to catch is a
    /// <c>private class</c> inside a test class, so a scan of public top-level types would find none
    /// of them. Iterator state machines, lambda display classes and async builders are filtered out —
    /// they carry <see cref="CompilerGeneratedAttribute"/> and would otherwise arrive as noise.
    /// </remarks>
    internal static IEnumerable<Type> TestAssemblyTypes() =>
        typeof(CollectionItemShapeGuard).Assembly
            .GetTypes()
            .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));

    /// <summary>
    /// The item type of the first <c>List&lt;T&gt;</c> property <paramref name="type"/> declares, or
    /// <c>null</c> when it declares none — i.e. when it is not a collection root at all.
    /// </summary>
    internal static IEnumerable<Type> CollectionItemTypes(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(p => p.PropertyType)
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            .Select(t => t.GetGenericArguments()[0])
            .Distinct();

    /// <summary>The item types <see cref="CollectionItemFixture"/> itself provides.</summary>
    internal static IReadOnlyList<Type> FixtureItemTypes { get; } =
    [
        typeof(OrderItem),
        typeof(BasketLine),
        typeof(AppointmentSlot),
        typeof(PricedLine),
        typeof(NamedOrderItem),
        typeof(MixedItem),
    ];

    /// <summary>
    /// Whether <paramref name="type"/> is declared by the fixture itself — the one file allowed to
    /// declare these shapes.
    /// </summary>
    internal static bool IsFixtureOwned(Type type) => FixtureItemTypes.Contains(type);

    private static string FriendlyName(Type type) =>
        Nullable.GetUnderlyingType(type) is { } underlying
            ? $"{underlying.Name}?"
            : type.Name;
}
