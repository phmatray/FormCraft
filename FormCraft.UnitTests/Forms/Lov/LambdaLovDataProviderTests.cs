namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LambdaLovDataProvider{TItem}"/> (#459) — its search,
/// context-filter, sort and pagination pipeline in <c>FromCollection</c>, and <c>FromAsyncSource</c>.
/// </summary>
public class LambdaLovDataProviderTests
{
    [Fact]
    public async Task FromCollection_Search_Predicate_Should_Narrow_Results()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(
            () => Items,
            item => item.Id,
            (item, text) => item.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

        var result = await provider.GetItemsAsync(
            new LovQuery { SearchText = "ebr" }, Xunit.TestContext.Current.CancellationToken);

        result.Items.ShouldHaveSingleItem();
        result.Items[0].Name.ShouldBe("Zebra");
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task FromCollection_Context_Filter_Should_Narrow_By_A_Named_Item_Property()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => Items, item => item.Id);
        var query = new LovQuery();
        query.Context["Region"] = "EU";

        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.ShouldAllBe(i => i.Region == "EU");
        result.TotalCount.ShouldBe(Items.Count(i => i.Region == "EU"));
    }

    [Fact]
    public async Task FromCollection_Should_Sort_Ascending_By_Default()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => Items, item => item.Id);
        var query = new LovQuery
        {
            Count = Items.Count,
            SortDefinitions = [new LovSortDefinition(nameof(Item.Name))]
        };

        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Name).ShouldBe(["Apple", "Mango", "Zebra"]);
    }

    [Fact]
    public async Task FromCollection_Should_Sort_Descending_When_The_Definition_Says_So()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => Items, item => item.Id);
        var query = new LovQuery
        {
            Count = Items.Count,
            SortDefinitions = [new LovSortDefinition(nameof(Item.Name), descending: true)]
        };

        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Name).ShouldBe(["Zebra", "Mango", "Apple"]);
    }

    [Fact]
    public async Task FromCollection_Should_Apply_Multiple_Sort_Definitions_In_The_Order_Given()
    {
        // LambdaLovDataProvider re-applies each SortDefinition as a fresh, stable OrderBy over
        // the previous result, so the LAST definition in the list becomes the primary sort key
        // and earlier definitions only break ties within it — the mirror image of ThenBy.
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => Items, item => item.Id);
        var query = new LovQuery
        {
            Count = Items.Count,
            SortDefinitions =
            [
                new LovSortDefinition(nameof(Item.Name)),   // tie-break
                new LovSortDefinition(nameof(Item.Region))  // primary (applied last)
            ]
        };

        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Id).ShouldBe([2, 1, 3]);
    }

    [Fact]
    public async Task FromCollection_Pagination_Should_Return_The_Correct_Slice_And_The_Pre_Pagination_Total()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => Items, item => item.Id);
        var query = new LovQuery { StartIndex = 1, Count = 2 };

        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Id).ShouldBe(Items.Skip(1).Take(2).Select(i => i.Id));
        result.TotalCount.ShouldBe(Items.Count);
    }

    [Fact]
    public async Task FromCollection_Should_Return_An_Empty_Page_For_An_Empty_Collection()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => [], item => item.Id);

        var result = await provider.GetItemsAsync(new LovQuery(), Xunit.TestContext.Current.CancellationToken);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task FromCollection_Should_Return_An_Empty_Page_When_StartIndex_Is_Beyond_The_Collection()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => Items, item => item.Id);
        var query = new LovQuery { StartIndex = 999, Count = 10 };

        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(Items.Count);
    }

    [Fact]
    public async Task FromAsyncSource_Should_Produce_The_Same_Paging_Behaviour_As_FromCollection()
    {
        var provider = LambdaLovDataProvider<Item>.FromAsyncSource(
            async ct =>
            {
                await Task.Yield();
                return Items;
            },
            item => item.Id);
        var query = new LovQuery { StartIndex = 1, Count = 1 };

        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Id).ShouldBe([Items[1].Id]);
        result.TotalCount.ShouldBe(Items.Count);
    }

    [Fact]
    public async Task GetItemByKeyAsync_Should_Find_An_Item_By_Key()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => Items, item => item.Id);

        var found = await provider.GetItemByKeyAsync(Items[1].Id, Xunit.TestContext.Current.CancellationToken);

        found.ShouldNotBeNull();
        found.Id.ShouldBe(Items[1].Id);
    }

    [Fact]
    public async Task GetItemByKeyAsync_Should_Return_Default_When_The_Key_Is_Not_Found()
    {
        var provider = LambdaLovDataProvider<Item>.FromCollection(() => Items, item => item.Id);

        var found = await provider.GetItemByKeyAsync(-1, Xunit.TestContext.Current.CancellationToken);

        found.ShouldBeNull();
    }

    [Fact]
    public async Task GetItemByKeyAsync_Should_Return_Default_When_No_GetByKeyFunc_Was_Supplied()
    {
        var provider = new LambdaLovDataProvider<Item>((_, _) => Task.FromResult(LovDataResult<Item>.Empty()));

        var found = await provider.GetItemByKeyAsync(1, Xunit.TestContext.Current.CancellationToken);

        found.ShouldBeNull();
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
