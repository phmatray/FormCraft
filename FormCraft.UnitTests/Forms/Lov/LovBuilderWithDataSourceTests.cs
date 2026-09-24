namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovBuilder{TModel, TValue, TItem}.WithDataSource(Func{IEnumerable{TItem}})"/>
/// (#472) — proving the collection-backed overload, reached through the public builder chain the
/// demo app and MudBlazor/FluentUI dialogs actually use, now honors
/// <see cref="LovQuery.SearchText"/>, <see cref="LovQuery.SortDefinitions"/> and
/// <see cref="LovQuery.Context"/>, and that <c>GetItemByKeyAsync</c> resolves a real item instead
/// of always returning default. Every LOV below is configured in the demo app's own call order —
/// <c>WithDataSource</c> before <c>WithKey</c>/<c>WithDisplay</c>/<c>AddColumn</c> — proving the
/// resolution is lazy rather than call-order-sensitive (AC7).
/// </summary>
public class LovBuilderWithDataSourceTests
{
    [Fact]
    public async Task SearchText_Should_Narrow_By_A_Configured_Columns_Value()
    {
        var provider = BuildProvider(lov => lov
            .WithDataSource(() => Customers)
            .WithKey(c => c.Id)
            .WithDisplay(c => c.Name)
            .AddColumn(c => c.Name, "Name"));

        var result = await provider.GetItemsAsync(
            new LovQuery { SearchText = "cme" }, Xunit.TestContext.Current.CancellationToken);

        result.Items.ShouldHaveSingleItem();
        result.Items[0].Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task SearchText_Should_Narrow_By_Display_When_No_Columns_Are_Configured()
    {
        var provider = BuildProvider(lov => lov
            .WithDataSource(() => Customers)
            .WithKey(c => c.Id)
            .WithDisplay(c => c.Name));

        var result = await provider.GetItemsAsync(
            new LovQuery { SearchText = "lobex" }, Xunit.TestContext.Current.CancellationToken);

        result.Items.ShouldHaveSingleItem();
        result.Items[0].Name.ShouldBe("Globex");
    }

    [Fact]
    public async Task A_SortDefinition_Should_Order_Items()
    {
        var provider = BuildProvider(lov => lov
            .WithDataSource(() => Customers)
            .WithKey(c => c.Id)
            .WithDisplay(c => c.Name));

        var query = new LovQuery
        {
            Count = Customers.Length,
            SortDefinitions = [new LovSortDefinition(nameof(CustomerDto.Name), descending: true)]
        };
        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.Select(c => c.Name).ShouldBe(["Globex", "Acme"]);
    }

    [Fact]
    public async Task A_Context_Entry_Should_Narrow_By_A_Named_Item_Property()
    {
        var provider = BuildProvider(lov => lov
            .WithDataSource(() => Customers)
            .WithKey(c => c.Id)
            .WithDisplay(c => c.Name));

        var query = new LovQuery();
        query.Context["Region"] = "EU";
        var result = await provider.GetItemsAsync(query, Xunit.TestContext.Current.CancellationToken);

        result.Items.ShouldHaveSingleItem();
        result.Items[0].Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task GetItemByKeyAsync_Should_Return_The_Matching_Item_For_A_Key_Set_Via_WithKey()
    {
        var provider = BuildProvider(lov => lov
            .WithDataSource(() => Customers)
            .WithKey(c => c.Id)
            .WithDisplay(c => c.Name));

        var found = await provider.GetItemByKeyAsync(2, Xunit.TestContext.Current.CancellationToken);

        found.ShouldNotBeNull();
        found.Name.ShouldBe("Globex");
    }

    [Fact]
    public async Task GetItemByKeyAsync_Should_Return_Default_When_The_Key_Does_Not_Exist()
    {
        var provider = BuildProvider(lov => lov
            .WithDataSource(() => Customers)
            .WithKey(c => c.Id)
            .WithDisplay(c => c.Name));

        var found = await provider.GetItemByKeyAsync(999, Xunit.TestContext.Current.CancellationToken);

        found.ShouldBeNull();
    }

    private static ILovDataProvider<CustomerDto> BuildProvider(
        Action<LovBuilder<OrderModel, int, CustomerDto>> configure)
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(configure))
            .Build();
        var lovConfig = GetLovConfiguration(config);
        var factory = new LovDataProviderFactory(A.Fake<IServiceProvider>());
        return factory.Create<CustomerDto, int>(lovConfig);
    }

    private static ILovConfiguration<CustomerDto, int> GetLovConfiguration(IFormConfiguration<OrderModel> config)
    {
        var field = config.Fields.Single(f => f.FieldName == nameof(OrderModel.CustomerId));
        return (ILovConfiguration<CustomerDto, int>)field.AdditionalAttributes["LovConfiguration"];
    }

    private static readonly CustomerDto[] Customers =
    [
        new() { Id = 1, Name = "Acme", Region = "EU" },
        new() { Id = 2, Name = "Globex", Region = "US" }
    ];

    private class OrderModel
    {
        public int CustomerId { get; set; }
    }

    private class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
