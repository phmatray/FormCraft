namespace FormCraft.UnitTests.Rendering;

/// <summary>
/// Pins <see cref="CollectionRowIdentity"/> directly — the RowKey/_rowTokens/HasDuplicateReference
/// mechanism moved out of both Blazor adapters into one core implementation (#334, #401, #422). The
/// adapters' own <c>CollectionRowIdentityTests</c> suites already pin this through a rendered
/// component; this suite is the regression guard for the extracted type itself, in isolation.
/// </summary>
public class CollectionRowIdentityTests
{
    private sealed class Item
    {
        public string Name { get; init; } = "";
    }

    private struct StructItem
    {
        public string Name { get; set; }
    }

    [Fact]
    public void A_Value_Typed_Item_Should_Key_By_Boxed_Index()
    {
        var identity = new CollectionRowIdentity();
        var items = new List<StructItem> { new() { Name = "a" }, new() { Name = "b" } };

        identity.KeyFor(items, 0).ShouldBe(0);
        identity.KeyFor(items, 1).ShouldBe(1);
    }

    [Fact]
    public void The_Same_Reference_Type_Item_Should_Key_The_Same_Way_On_Every_Call()
    {
        var identity = new CollectionRowIdentity();
        var items = new List<Item> { new() { Name = "a" } };

        var first = identity.KeyFor(items, 0);
        var second = identity.KeyFor(items, 0);

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void Two_Different_Items_Should_Get_Two_Different_Keys()
    {
        var identity = new CollectionRowIdentity();
        var items = new List<Item> { new() { Name = "a" }, new() { Name = "b" } };

        var first = identity.KeyFor(items, 0);
        var second = identity.KeyFor(items, 1);

        second.ShouldNotBeSameAs(first);
    }

    /// <summary>
    /// Legal for a reference type: the SAME item instance appears twice. Minting one weak token
    /// would hand two rows the same key, which is the duplicate-key crash a keyed loop exists to
    /// avoid (#308) — so both occurrences fall back to the boxed index instead.
    /// </summary>
    [Fact]
    public void The_Same_Item_Instance_Appearing_Twice_Should_Fall_Back_To_Boxed_Index_For_Both()
    {
        var identity = new CollectionRowIdentity();
        var shared = new Item { Name = "shared" };
        var items = new List<Item> { shared, shared };

        identity.KeyFor(items, 0).ShouldBe(0);
        identity.KeyFor(items, 1).ShouldBe(1);
    }

    /// <summary>
    /// Two collection fields must never share tokens — each rendered component holds its own
    /// <see cref="CollectionRowIdentity"/> instance, so the same item object in two different
    /// instances is free to get two unrelated keys.
    /// </summary>
    [Fact]
    public void Two_Separate_Instances_Should_Not_Share_Tokens_For_The_Same_Item()
    {
        var item = new Item { Name = "shared-across-instances" };
        var items = new List<Item> { item };

        var first = new CollectionRowIdentity().KeyFor(items, 0);
        var second = new CollectionRowIdentity().KeyFor(items, 0);

        second.ShouldNotBeSameAs(first);
    }
}
