using Microsoft.Extensions.Options;

namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovServiceCollectionExtensions"/> (#459) — the instances a
/// built <see cref="IServiceProvider"/> actually resolves, not that a call returns the collection.
/// </summary>
public class LovServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFormCraftLov_Should_Register_A_Resolvable_Factory_And_MappingProcessor()
    {
        var services = new ServiceCollection();

        services.AddFormCraftLov();
        var provider = services.BuildServiceProvider();

        provider.GetService<ILovDataProviderFactory>().ShouldNotBeNull();
        provider.GetService<ILovMappingProcessor>().ShouldNotBeNull();
    }

    [Fact]
    public void AddFormCraftLov_Should_Apply_The_Configure_Callback_To_The_Registered_Options()
    {
        var services = new ServiceCollection();

        services.AddFormCraftLov(options =>
        {
            options.ConfigurationPath = "custom/path";
            options.EnableHotReload = false;
            options.DefaultSearchDebounceMs = 123;
            options.DefaultPageSize = 25;
            options.AllowedBaseUrls.Add("https://example.com");
        });
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOptions<LovServiceOptions>>().Value;

        resolved.ConfigurationPath.ShouldBe("custom/path");
        resolved.EnableHotReload.ShouldBeFalse();
        resolved.DefaultSearchDebounceMs.ShouldBe(123);
        resolved.DefaultPageSize.ShouldBe(25);
        resolved.AllowedBaseUrls.ShouldContain("https://example.com");
    }

    [Fact]
    public void AddLovDataService_Generic_Should_Resolve_The_Same_Instance_For_Both_Interfaces()
    {
        var services = new ServiceCollection();

        services.AddLovDataService<FakeLovService, Item>();
        var provider = services.BuildServiceProvider();

        var asDataService = provider.GetRequiredService<ILovDataService<Item>>();
        var asDataProvider = provider.GetRequiredService<ILovDataProvider<Item>>();

        asDataService.ShouldBeSameAs(asDataProvider);
    }

    [Fact]
    public void AddLovDataService_With_Factory_Should_Return_What_The_Factory_Produced()
    {
        var services = new ServiceCollection();
        var expected = A.Fake<ILovDataProvider<Item>>();

        services.AddLovDataService<Item>(_ => expected);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILovDataProvider<Item>>().ShouldBeSameAs(expected);
    }

    public sealed class Item
    {
        public int Id { get; set; }
    }

    private sealed class FakeLovService : ILovDataService<Item>
    {
        public Task<LovDataResult<Item>> GetItemsAsync(
            LovQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(LovDataResult<Item>.Empty());

        public Task<Item?> GetItemByKeyAsync(object key, CancellationToken cancellationToken = default)
            => Task.FromResult<Item?>(null);
    }
}
