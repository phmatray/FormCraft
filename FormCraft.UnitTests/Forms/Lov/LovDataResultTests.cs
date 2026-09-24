namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovDataResult{TItem}.FromCollection(IEnumerable{TItem}, LovQuery, Func{TItem, string, bool}?)"/>
/// (#472) — the shared search → context → sort → page pipeline that both
/// <see cref="LambdaLovDataProvider{TItem}"/> and
/// <c>LovBuilder&lt;TModel, TValue, TItem&gt;.WithDataSource(Func&lt;IEnumerable&lt;TItem&gt;&gt;)</c>
/// route through.
/// </summary>
public class LovDataResultTests
{
    [Fact]
    public void FromCollection_With_A_SearchPredicate_Should_Narrow_Items_And_TotalCount()
    {
        var query = new LovQuery { SearchText = "ebr" };

        var result = LovDataResult<Item>.FromCollection(
            Items,
            query,
            (item, text) => item.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

        result.Items.ShouldHaveSingleItem();
        result.Items[0].Name.ShouldBe("Zebra");
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public void FromCollection_With_A_Context_Entry_Should_Narrow_By_A_Named_Item_Property()
    {
        var query = new LovQuery();
        query.Context["Region"] = "EU";

        var result = LovDataResult<Item>.FromCollection(Items, query, null);

        result.Items.ShouldAllBe(i => i.Region == "EU");
        result.TotalCount.ShouldBe(Items.Count(i => i.Region == "EU"));
    }

    [Fact]
    public void FromCollection_With_A_SortDefinition_Should_Order_Items()
    {
        var query = new LovQuery
        {
            Count = Items.Count,
            SortDefinitions = [new LovSortDefinition(nameof(Item.Name))]
        };

        var result = LovDataResult<Item>.FromCollection(Items, query, null);

        result.Items.Select(i => i.Name).ShouldBe(["Apple", "Mango", "Zebra"]);
    }

    [Fact]
    public void TwoArg_Overload_Should_Ignore_SearchText_But_Still_Apply_Context_And_Sort()
    {
        // No predicate reaches the 2-arg overload, so a non-null SearchText makes no difference
        // to which items come back — but Context filtering and SortDefinitions ordering now run,
        // exactly as the 3-arg overload defines them (AC5).
        var query = new LovQuery
        {
            SearchText = "this text matches nothing and must be ignored",
            SortDefinitions = [new LovSortDefinition(nameof(Item.Name))]
        };
        query.Context["Region"] = "EU";

        var result = LovDataResult<Item>.FromCollection(Items, query);

        result.Items.Select(i => i.Name).ShouldBe(["Apple", "Zebra"]);
        result.TotalCount.ShouldBe(2);
    }

    private static readonly List<Item> Items =
    [
        new() { Id = 1, Name = "Zebra", Region = "EU" },
        new() { Id = 2, Name = "Apple", Region = "EU" },
        new() { Id = 3, Name = "Mango", Region = "US" }
    ];

    private sealed class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
