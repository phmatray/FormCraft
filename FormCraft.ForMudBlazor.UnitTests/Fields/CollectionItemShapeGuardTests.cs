namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests for <see cref="CollectionItemShapeGuard"/> — the check that fails the build when a suite
/// re-declares a collection-item shape <see cref="CollectionItemFixture"/> already provides (#297).
/// </summary>
/// <remarks>
/// <para>
/// Three issues attacked the same defect before this guard existed, and each was found by a human
/// reading files. #205 extracted the fixture; #258 closed with a grep matching six <i>type names</i>,
/// so five suites expressing the same shapes under other names survived it invisibly; #282 found
/// those five by hand. The failure is silent by construction — a nested private copy <i>shadows</i>
/// the namespace-scope fixture model, so a half-finished migration still compiles and still passes.
/// The compiler is not the check, so this is.
/// </para>
/// <para>
/// ⚠️ <b>Half of this file exists to prove the guard can still FAIL.</b> "No offenders in this
/// assembly" is the assertion that matters in CI, and it is also the assertion that keeps passing
/// after the detection path silently breaks — invert one predicate and it is green forever, reporting
/// nothing, which is precisely the shape of #258's defect. So the offender pipeline is also run
/// against deliberately-offending inputs declared at the bottom of this file.
/// </para>
/// </remarks>
public class CollectionItemShapeGuardTests
{
    [Fact]
    public void No_Suite_Should_Re_Declare_A_Collection_Item_Shape_The_Fixture_Provides()
    {
        // Arrange & Act - the guard as CI runs it, over the whole assembly.
        var offenders = CollectionItemShapeGuard.FindOffenders(
            CollectionItemShapeGuard.TestAssemblyTypes().Where(t => t.DeclaringType != typeof(Offending)));

        // Assert - the message has to name the offender AND say what to do, because the reader is a
        // contributor who has just watched a green build turn red on a file they did not touch.
        offenders.ShouldBeEmpty(
            "These types re-declare a shape CollectionItemFixture already provides. Use the fixture's "
            + "model and item-form builder instead (#205, #258, #282). If a local copy is genuinely "
            + "warranted, pass it to FindOffenders' allowlist with the reason:\n  "
            + string.Join("\n  ", offenders.Select(o => o.Detail)));
    }

    [Fact]
    public void The_Guard_Should_Flag_A_Nested_Copy_Of_A_Fixture_Item_Shape()
    {
        // Arrange - the detection path, run against a known offender. Without this the assertion
        // above passes just as happily when FindOffenders has stopped finding anything at all.
        var universe = new[] { typeof(OrderModel), typeof(OrderItem), typeof(Offending.SecretsRoot), typeof(Offending.SecretRow) };

        // Act
        var offenders = CollectionItemShapeGuard.FindOffenders(universe);

        // Assert - SecretRow is a one-string row under another name, exactly what #258's grep missed
        offenders.ShouldNotBeEmpty();
        offenders.ShouldContain(o => o.Owner == typeof(Offending.SecretsRoot));
        offenders.Select(o => o.Detail).ShouldContain(d => d.Contains("SecretRow"));
    }

    [Fact]
    public void The_Guard_Should_Flag_A_Nested_Copy_Of_A_Fixture_ROOT()
    {
        // Arrange - the other half of the shadowing story, and the one the class doc actually
        // describes: a private OrderModel holding the fixture's own OrderItem. The item type is
        // shared, so an item-only rule sees nothing wrong; the ROOT is the copy.
        var universe = new[] { typeof(OrderModel), typeof(OrderItem), typeof(Offending.ShadowOrderModel) };

        // Act
        var offenders = CollectionItemShapeGuard.FindOffenders(universe);

        // Assert
        offenders.ShouldNotBeEmpty();
        offenders.Select(o => o.Detail).ShouldContain(d => d.Contains("ShadowOrderModel") && d.Contains("OrderModel"));
    }

    [Fact]
    public void The_Allowlist_Should_Suppress_A_Deliberate_Local_Copy()
    {
        // Arrange - the escape hatch, exercised. An allowlist that nothing ever suppresses is dead
        // code that a regression could break unnoticed, so it is tested against a real offender
        // rather than left to be believed.
        var universe = new[] { typeof(OrderModel), typeof(OrderItem), typeof(Offending.SecretsRoot), typeof(Offending.SecretRow) };

        // Act
        var withoutAllowlist = CollectionItemShapeGuard.FindOffenders(universe);
        var withAllowlist = CollectionItemShapeGuard.FindOffenders(
            universe,
            new HashSet<Type> { typeof(Offending.SecretsRoot) });

        // Assert - same input, and the allowlist is the only difference
        withoutAllowlist.ShouldNotBeEmpty();
        withAllowlist.ShouldBeEmpty();
    }

    [Fact]
    public void The_Guard_Should_Not_Flag_A_Genuinely_Different_Shape()
    {
        // Arrange - the negative control. A guard that flagged everything would satisfy the three
        // tests above while making the assembly-wide assertion unsatisfiable. CollectionNumericTypeTests'
        // seven-numeric row (#209) is the real instance of a shape the fixture does not provide.
        var universe = new[] { typeof(OrderModel), typeof(OrderItem), typeof(Offending.SevenNumericsRoot), typeof(Offending.SevenNumericsRow) };

        // Act
        var offenders = CollectionItemShapeGuard.FindOffenders(universe);

        // Assert
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void ShapeSignature_Should_Ignore_Declaration_Order()
    {
        // Arrange & Act - the signature is a multiset, not a sequence. Two rows carrying the same
        // member types in a different order are the same shape, and a signature that said otherwise
        // would let a copy through simply by reordering its properties.
        var declaredOneWay = CollectionItemShapeGuard.ShapeSignature(typeof(OrderedOneWay));
        var declaredTheOther = CollectionItemShapeGuard.ShapeSignature(typeof(OrderedTheOther));

        // Assert
        declaredOneWay.ShouldBe(declaredTheOther);
    }

    [Fact]
    public void ShapeSignature_Should_Match_For_Structurally_Identical_Types()
    {
        // Arrange & Act - the whole point: NAMES must not enter the signature. This is precisely what
        // #258's grep could not see — CredentialsModel/Credential and VaultModel/VaultEntry modelled
        // OrderItem's shape under two other vocabularies and passed a name-based check.
        var one = CollectionItemShapeGuard.ShapeSignature(typeof(SingleStringA));
        var other = CollectionItemShapeGuard.ShapeSignature(typeof(SingleStringB));

        // Assert
        one.ShouldBe(other);
    }

    [Fact]
    public void ShapeSignature_Should_Tell_Different_Shapes_Apart()
    {
        // Arrange & Act - the negative control. A signature that collapsed everything to one value
        // would pass both tests above while making the guard fire on every model in the assembly.
        var single = CollectionItemShapeGuard.ShapeSignature(typeof(SingleStringA));
        var pair = CollectionItemShapeGuard.ShapeSignature(typeof(OrderedOneWay));

        // Assert
        single.ShouldNotBe(pair);
    }

    [Fact]
    public void ShapeSignature_Should_See_Through_A_Base_Class()
    {
        // Arrange & Act - inherited members count. If they did not, one `: RowBase` would empty a
        // copy's signature and disable the guard for it — a one-line bypass of the whole check.
        var inherited = CollectionItemShapeGuard.ShapeSignature(typeof(InheritsSingleString));
        var declared = CollectionItemShapeGuard.ShapeSignature(typeof(SingleStringA));

        // Assert
        inherited.ShouldBe(declared);
    }

    [Fact]
    public void ShapeSignature_Should_Tell_Closed_Generics_Apart()
    {
        // Arrange & Act - a name-only rendering collapses every closed generic to "List`1", so
        // List<int> and List<string> would compare equal and two unrelated shapes would match.
        var ints = CollectionItemShapeGuard.ShapeSignature(typeof(HoldsIntList));
        var strings = CollectionItemShapeGuard.ShapeSignature(typeof(HoldsStringList));

        // Assert
        ints.ShouldNotBe(strings);
    }

    [Fact]
    public void ShapeSignature_Should_Describe_The_Fixtures_Own_Item_Types()
    {
        // Arrange & Act - the signatures the guard actually compares against. Pinned literally so a
        // member quietly added to a fixture model shows up here rather than silently widening what
        // the guard treats as "a shape the fixture provides".
        var signatures = new[]
            {
                typeof(OrderItem), typeof(BasketLine), typeof(AppointmentSlot),
                typeof(PricedLine), typeof(NamedOrderItem), typeof(MixedItem),
            }
            .ToDictionary(t => t.Name, CollectionItemShapeGuard.ShapeSignature);

        // Assert
        signatures.Count.ShouldBe(6);
        signatures["OrderItem"].ShouldBe("String");
        signatures["AppointmentSlot"].ShouldBe("DateTime");
        signatures["PricedLine"].ShouldBe("Decimal");
        signatures["NamedOrderItem"].ShouldBe("String");
        signatures["BasketLine"].ShouldBe("Boolean, Int32");
        signatures["MixedItem"].ShouldBe("Boolean, DateTime, Int32, String");
    }

    [Fact]
    public void CollectionItemTypes_Should_Find_The_Item_Type_Behind_Every_List_Property()
    {
        // Arrange & Act - a root with two collections must yield BOTH, or a copy hidden behind a
        // second property would never be scanned.
        var one = CollectionItemShapeGuard.CollectionItemTypes(typeof(OrderModel)).ToList();
        var two = CollectionItemShapeGuard.CollectionItemTypes(typeof(Offending.TwoDifferentCollections)).ToList();
        var none = CollectionItemShapeGuard.CollectionItemTypes(typeof(SingleStringA)).ToList();

        // Assert
        one.ShouldBe(new[] { typeof(OrderItem) });
        two.ShouldBe(new[] { typeof(OrderItem), typeof(BasketLine) });
        none.ShouldBeEmpty();
    }

    [Fact]
    public void IsSharedShape_Should_Separate_Namespace_Scope_From_Nested()
    {
        // Arrange & Act - the ownership rule, and the reason there is no hand-maintained roster of
        // fixture types: a copy is `private`, which in C# means nested, which is what lets it shadow.
        // A roster would misreport the fixture itself the day a seventh model joined it.
        CollectionItemShapeGuard.IsSharedShape(typeof(OrderItem)).ShouldBeTrue();
        CollectionItemShapeGuard.IsSharedShape(typeof(TwoCollectionModel)).ShouldBeTrue();
        CollectionItemShapeGuard.IsSharedShape(typeof(SingleStringA)).ShouldBeFalse();
    }

    private class OrderedOneWay
    {
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }

    private class OrderedTheOther
    {
        public int Quantity { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private class SingleStringA
    {
        public string ProductName { get; set; } = string.Empty;
    }

    private class SingleStringB
    {
        public string Secret { get; set; } = string.Empty;
    }

    private abstract class SingleStringBase
    {
        public string Inherited { get; set; } = string.Empty;
    }

    private sealed class InheritsSingleString : SingleStringBase;

    private class HoldsIntList
    {
        public List<int> Values { get; set; } = new();
    }

    private class HoldsStringList
    {
        public List<string> Values { get; set; } = new();
    }

    /// <summary>
    /// Deliberately-offending shapes, used only as <c>FindOffenders</c> input by the tests above.
    /// </summary>
    /// <remarks>
    /// They are excluded from the assembly-wide run by name — the guard is supposed to flag them, and
    /// a check that had to ignore its own fixtures implicitly would be one filter away from ignoring
    /// a real one. Keeping them under a single nested container makes that exclusion one predicate
    /// rather than a growing list.
    /// </remarks>
    private static class Offending
    {
        internal sealed class SecretRow
        {
            public string Secret { get; set; } = string.Empty;
        }

        internal sealed class SecretsRoot
        {
            public List<SecretRow> Items { get; set; } = new();
        }

        internal sealed class ShadowOrderModel
        {
            public List<OrderItem> Items { get; set; } = new();
        }

        internal sealed class SevenNumericsRow
        {
            public int AsInt { get; set; }

            public decimal AsDecimal { get; set; }

            public double AsDouble { get; set; }

            public float AsFloat { get; set; }

            public long AsLong { get; set; }

            public short AsShort { get; set; }

            public byte AsByte { get; set; }
        }

        internal sealed class SevenNumericsRoot
        {
            public List<SevenNumericsRow> Rows { get; set; } = new();
        }

        internal sealed class TwoDifferentCollections
        {
            public List<OrderItem> Orders { get; set; } = new();

            public List<BasketLine> Lines { get; set; } = new();
        }
    }
}
