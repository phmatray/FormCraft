using System.Reflection;
using System.Runtime.CompilerServices;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// One re-declared collection-item shape, and why it was reported.
/// </summary>
/// <param name="Owner">The type declaring the collection.</param>
/// <param name="Detail">A message naming what collided with what.</param>
internal sealed record ShapeOffence(Type Owner, string Detail)
{
    public override string ToString() => Detail;
}

/// <summary>
/// The rule <see cref="CollectionItemFixture"/> is supposed to hold — <i>no test file declares a
/// private copy of a collection-item shape the fixture provides</i> — expressed as something the
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
/// nested <c>private class OrderItem</c> <i>shadows</i> the namespace-scope one the fixture declares,
/// so a half-finished migration compiles clean and passes green. There is no compile error to lean on
/// — #282 had to enumerate per-suite test counts from the built dll to prove its migration had landed.
/// </para>
/// <para>
/// <b>Ownership is derived, never listed.</b> A shared model is one declared at <i>namespace scope</i>;
/// a copy is always <c>private</c> and therefore <i>nested</i> inside a test class — that is what makes
/// it shadow. So the guard asks "is this type nested?" rather than consulting a roster of known fixture
/// types. A hand-maintained roster would reproduce #258's exact defect one level up: add a seventh model
/// to the fixture, forget to enrol it, and the guard reports <i>the fixture itself</i> as an offender
/// while telling the author to "use the fixture".
/// </para>
/// <para>
/// <b>Both halves of the shape are checked.</b> An <i>item</i> copy (<c>List&lt;MyRow&gt;</c> where
/// <c>MyRow</c> matches <see cref="OrderItem"/>'s shape) and a <i>root</i> copy (a private
/// <c>OrderModel</c> holding <c>List&lt;OrderItem&gt;</c>) are the same mistake seen from two ends, and
/// the root half is the one the shadowing story above is actually about.
/// </para>
/// <para>
/// <b>What this deliberately does not catch.</b> The match is exact, so a <i>drifted</i> copy —
/// <c>{ string ProductName; int Quantity }</c> where the fixture has <c>{ string }</c> — signatures
/// differently and passes. That is a real gap and an accepted one: the alternative is a fuzzy
/// subset rule whose false positives would land on every unrelated model, and drift is caught by the
/// migration issues (#205, #258, #282) rather than by this. Equally, <c>"String"</c> is the commonest
/// row shape imaginable, so an unrelated single-string row <i>will</i> be flagged one day — that is
/// what the <c>allowed</c> parameter on <see cref="FindOffenders"/> is for, and why it is exercised
/// by a test rather than merely offered.
/// </para>
/// </remarks>
internal static class CollectionItemShapeGuard
{
    /// <summary>
    /// A type's shape as a multiset of its public instance property types — order-insensitive, and
    /// deliberately blind to names.
    /// </summary>
    /// <remarks>
    /// Names are excluded because names are what #258's check compared, and comparing them is what let
    /// <c>Credential</c> and <c>VaultEntry</c> pass while modelling <see cref="OrderItem"/>'s exact
    /// shape. Ordering is normalised so a copy cannot evade the guard by reordering its members, and
    /// inherited properties are included so a one-line base class cannot hide the shape either.
    /// </remarks>
    internal static string ShapeSignature(Type itemType) =>
        string.Join(
            ", ",
            itemType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Select(p => FriendlyName(p.PropertyType))
                .OrderBy(name => name, StringComparer.Ordinal));

    /// <summary>
    /// The item type behind every <c>List&lt;T&gt;</c> property <paramref name="type"/> exposes —
    /// empty when it exposes none, i.e. when it is not a collection root at all.
    /// </summary>
    /// <remarks>
    /// Inherited properties count: a root whose <c>List&lt;T&gt;</c> comes from a base class is still a
    /// collection root, and excluding it would let one base class disable the guard.
    /// </remarks>
    internal static IEnumerable<Type> CollectionItemTypes(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.PropertyType)
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            .Select(t => t.GetGenericArguments()[0]);

    /// <summary>
    /// Whether <paramref name="type"/> is a <i>shared</i> model — declared in the fixture's own
    /// assembly, <c>FormCraft.TestSupport</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Re-keyed in #343 from "namespace scope vs nested" to this. The old rule — <c>DeclaringType is
    /// null</c> — conflated two different things that happened to coincide as long as the fixture and
    /// every suite that might copy it lived in one assembly: "declared by the fixture" and "declared
    /// at namespace scope, wherever that is". They stopped coinciding the moment a second test
    /// assembly (Fluent UI's) could declare its own namespace-scope, public model — which the old rule
    /// would have called shared for the same reason it called the fixture's own models shared, and so
    /// would never have flagged as an offender at all.
    /// </para>
    /// <para>
    /// Deriving ownership from the declaring assembly rather than a hand-maintained roster means a
    /// model added to the fixture tomorrow is shared by construction — nothing to remember, nothing to
    /// enrol — which is the property the old rule had and this one keeps.
    /// </para>
    /// </remarks>
    internal static bool IsSharedShape(Type type) => type.Assembly == typeof(CollectionItemFixture).Assembly;

    /// <summary>
    /// Every type declared in <paramref name="assembly"/> <b>and</b> in the fixture's own assembly,
    /// including nested and non-public ones, minus the compiler's own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Takes the caller's assembly explicitly (#343) rather than reading its own — each adapter suite
    /// runs the guard over itself, so a hard-coded <c>typeof(CollectionItemShapeGuard).Assembly</c>
    /// would always report on whichever assembly happens to declare this guard, never on the caller's.
    /// </para>
    /// <para>
    /// Always includes the fixture's assembly too, union'd in: <c>Assembly.GetTypes()</c> only ever
    /// returns types <i>declared</i> in the assembly it is called on, and every comparison below needs
    /// the fixture's own shared models in the universe to compare a candidate copy against — scanning
    /// the caller alone would find plenty of local candidates and nothing shared to match them to,
    /// which is silence, not a passing check.
    /// </para>
    /// <para>
    /// The nested/non-public part is load-bearing: every model this guard exists to catch is a
    /// <c>private class</c> inside a test class, so a scan of public top-level types would find none of
    /// them. Iterator state machines, lambda display classes and async builders carry
    /// <see cref="CompilerGeneratedAttribute"/> and are dropped as noise.
    /// </para>
    /// <para>
    /// A partially-unloadable assembly degrades to "scan what loaded" rather than throwing: a check
    /// meant to be trusted must not turn a dependency bump into an opaque type-load stack trace that
    /// says nothing about the rule it enforces.
    /// </para>
    /// </remarks>
    /// <param name="assembly">The consuming test assembly to scan — pass the caller's own.</param>
    internal static IEnumerable<Type> TestAssemblyTypes(Assembly assembly)
    {
        return new[] { assembly, typeof(CollectionItemFixture).Assembly }
            .Distinct()
            .SelectMany(TypesOf);
    }

    private static IEnumerable<Type> TypesOf(Assembly assembly)
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
            .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));
    }

    /// <summary>
    /// Every re-declared collection-item shape among <paramref name="types"/>: a nested model whose
    /// item shape, or whose whole collection composition, already exists at namespace scope.
    /// </summary>
    /// <remarks>
    /// Takes its input rather than reading the assembly so it can be exercised with a <i>known-bad</i>
    /// set. A guard whose detection path is never run against an offender is a guard that can quietly
    /// stop detecting — which is the failure #258 shipped and this whole issue exists to prevent, so
    /// asserting "no offenders in this assembly" is only half a test.
    /// </remarks>
    /// <param name="types">The universe to scan.</param>
    /// <param name="allowed">
    /// Models that may keep a local shape despite matching, each justified where it is declared. Empty
    /// by default: an exception should be a deliberate act at the call site, not a standing grant.
    /// </param>
    internal static IReadOnlyList<ShapeOffence> FindOffenders(
        IEnumerable<Type> types,
        IReadOnlySet<Type>? allowed = null)
    {
        var roots = types
            .Select(type => (Type: type, Items: CollectionItemTypes(type).ToList()))
            .Where(candidate => candidate.Items.Count > 0)
            .ToList();

        // Computed once. Recomputing a signature per comparison is quadratic in
        // (collection roots x shared models) and grows exactly as the suite does.
        var sharedItemSignatures = roots
            .Where(root => IsSharedShape(root.Type))
            .SelectMany(root => root.Items)
            .Where(IsSharedShape)
            .Select(ShapeSignature)
            .ToHashSet(StringComparer.Ordinal);

        // Grouped, not ToDictionary'd: several shared roots legitimately share a composition —
        // OrderModel and NamedOrderModel are both "one collection of a single-string item" — so
        // keying them one-to-one throws on the duplicate. Naming all of them is also the more useful
        // message, since the reader wants to know which shared root to adopt.
        var sharedCompositions = roots
            .Where(root => IsSharedShape(root.Type))
            .GroupBy(root => Composition(root.Items), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => string.Join(
                    " or ",
                    group.Select(root => root.Type.Name).OrderBy(name => name, StringComparer.Ordinal)),
                StringComparer.Ordinal);

        var offences = new List<ShapeOffence>();

        foreach (var (owner, items) in roots)
        {
            if (IsSharedShape(owner) || allowed?.Contains(owner) == true)
            {
                continue;
            }

            foreach (var item in items.Where(i => !IsSharedShape(i)).Distinct())
            {
                var signature = ShapeSignature(item);
                if (sharedItemSignatures.Contains(signature))
                {
                    offences.Add(new ShapeOffence(
                        owner,
                        $"{Describe(owner)} holds List<{item.Name}>, whose shape ({signature}) is "
                        + "already a CollectionItemFixture item type"));
                }
            }

            if (sharedCompositions.TryGetValue(Composition(items), out var sharedRoot))
            {
                offences.Add(new ShapeOffence(
                    owner,
                    $"{Describe(owner)} is a collection root whose composition "
                    + $"({Composition(items)}) is already modelled by CollectionItemFixture.{sharedRoot}"));
            }
        }

        return offences.OrderBy(o => o.Detail, StringComparer.Ordinal).ToList();
    }

    /// <summary>A root's shape: the multiset of its collections' item signatures.</summary>
    private static string Composition(IEnumerable<Type> items) =>
        string.Join(" + ", items.Select(ShapeSignature).OrderBy(s => s, StringComparer.Ordinal));

    private static string Describe(Type type) =>
        type.DeclaringType is null ? type.Name : $"{type.DeclaringType.Name}.{type.Name}";

    private static string FriendlyName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return $"{FriendlyName(underlying)}?";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        // Without this, every closed generic collapses to the same token — List<int> and List<string>
        // would both read "List`1" and compare equal, which is a shape check that cannot see types.
        var name = type.Name[..type.Name.IndexOf('`')];
        var arguments = string.Join(", ", type.GetGenericArguments().Select(FriendlyName));

        return $"{name}<{arguments}>";
    }
}
