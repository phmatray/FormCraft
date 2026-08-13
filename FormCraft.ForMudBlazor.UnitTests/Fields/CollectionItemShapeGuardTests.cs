namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests for <see cref="CollectionItemShapeGuard"/> — the check that fails the build when a suite
/// re-declares a collection-item shape <see cref="CollectionItemFixture"/> already provides (#297).
/// </summary>
/// <remarks>
/// Three issues attacked the same defect before this guard existed, and each was found by a human
/// reading files. #205 extracted the fixture; #258 closed with a grep matching six <i>type names</i>,
/// so five suites expressing the same shapes under other names survived it invisibly; #282 found
/// those five by hand. The failure is silent by construction — a nested private copy <i>shadows</i>
/// the namespace-scope fixture model, so a half-finished migration still compiles and still passes.
/// The compiler is not the check, so this is.
/// </remarks>
public class CollectionItemShapeGuardTests
{
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
    public void ShapeSignature_Should_Describe_The_Fixtures_Own_Item_Types()
    {
        // Arrange & Act - the signatures the guard actually compares against. Pinned literally so a
        // member quietly added to a fixture model shows up here rather than silently widening what
        // the guard treats as "a shape the fixture provides".
        var signatures = CollectionItemShapeGuard.FixtureItemTypes
            .ToDictionary(t => t.Name, CollectionItemShapeGuard.ShapeSignature);

        // Assert
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
        // Arrange & Act - the other half of the guard's input. A root with two collections yields
        // both item types, which is what lets the guard see past a model that hides a copy behind a
        // second property rather than a first.
        var single = CollectionItemShapeGuard.CollectionItemTypes(typeof(OrderModel)).ToList();
        var none = CollectionItemShapeGuard.CollectionItemTypes(typeof(SingleStringA)).ToList();

        // Assert
        single.ShouldBe(new[] { typeof(OrderItem) });
        none.ShouldBeEmpty();
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
}
