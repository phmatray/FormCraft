namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovDataProviderFactory"/> (#459), covering
/// <see cref="ServiceLovDataProvider{TItem}"/> indirectly through the factory — the way
/// production code always reaches it.
/// </summary>
public class LovDataProviderFactoryTests
{
    [Fact]
    public void Create_Should_Return_A_LambdaLovDataProvider_When_DataProvider_Is_Set()
    {
        var factory = new LovDataProviderFactory(A.Fake<IServiceProvider>());
        var config = new LovConfiguration<Item, int>
        {
            DataProvider = (_, _) => Task.FromResult(new LovDataResult<Item>([new Item { Id = 1 }], 1))
        };

        var provider = factory.Create<Item, int>(config);

        provider.ShouldBeOfType<LambdaLovDataProvider<Item>>();
    }

    [Fact]
    public async Task Create_Should_Resolve_And_Delegate_To_The_DI_Registered_Provider_When_DataProviderServiceType_Is_Set()
    {
        var innerProvider = A.Fake<ILovDataProvider<Item>>();
        var expected = new LovDataResult<Item>([new Item { Id = 7 }], 1);
        A.CallTo(() => innerProvider.GetItemsAsync(A<LovQuery>._, A<CancellationToken>._)).Returns(expected);
        var serviceProvider = A.Fake<IServiceProvider>();
        A.CallTo(() => serviceProvider.GetService(typeof(IItemLovService))).Returns(innerProvider);
        var factory = new LovDataProviderFactory(serviceProvider);
        var config = new LovConfiguration<Item, int> { DataProviderServiceType = typeof(IItemLovService) };

        var provider = factory.Create<Item, int>(config);
        var result = await provider.GetItemsAsync(new LovQuery(), Xunit.TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task GetItemsAsync_Should_Throw_Naming_The_Service_Type_When_It_Does_Not_Implement_ILovDataProvider()
    {
        var serviceProvider = A.Fake<IServiceProvider>();
        A.CallTo(() => serviceProvider.GetService(typeof(IItemLovService))).Returns(new object());
        var factory = new LovDataProviderFactory(serviceProvider);
        var config = new LovConfiguration<Item, int> { DataProviderServiceType = typeof(IItemLovService) };
        var provider = factory.Create<Item, int>(config);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => provider.GetItemsAsync(new LovQuery(), Xunit.TestContext.Current.CancellationToken));

        ex.Message.ShouldContain(nameof(IItemLovService));
        ex.Message.ShouldContain(nameof(Item));
    }

    [Fact]
    public void Create_Should_Throw_Mentioning_ILovRestClient_When_Only_JsonConfigId_Is_Set()
    {
        var factory = new LovDataProviderFactory(A.Fake<IServiceProvider>());
        var config = new LovConfiguration<Item, int> { JsonConfigId = "customers" };

        var ex = Should.Throw<InvalidOperationException>(() => factory.Create<Item, int>(config));

        ex.Message.ShouldContain("ILovRestClient");
    }

    [Fact]
    public void Create_Should_Throw_No_Data_Source_Configured_When_Nothing_Is_Set()
    {
        var factory = new LovDataProviderFactory(A.Fake<IServiceProvider>());
        var config = new LovConfiguration<Item, int>();

        var ex = Should.Throw<InvalidOperationException>(() => factory.Create<Item, int>(config));

        ex.Message.ShouldContain("No data source configured");
    }

    private interface IItemLovService : ILovDataProvider<Item>;

    public sealed class Item
    {
        public int Id { get; set; }
    }
}
