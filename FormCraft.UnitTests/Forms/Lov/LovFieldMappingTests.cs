namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovFieldMapping{TItem, TModel, TValue}"/> and
/// <see cref="AsyncLovFieldMapping{TItem, TModel, TValue}"/> (#459).
/// </summary>
public class LovFieldMappingTests
{
    [Fact]
    public void Apply_Should_Copy_The_Source_Propertys_Value_To_The_Target_Property()
    {
        var mapping = new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName);
        var item = new SourceItem { Name = "Acme" };
        var model = new TargetModel();

        mapping.Apply(item, model);

        model.CustomerName.ShouldBe("Acme");
    }

    [Fact]
    public void AsyncLovFieldMapping_Apply_Should_Copy_The_Source_Propertys_Value_Synchronously()
    {
        var mapping = new AsyncLovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName);
        var item = new SourceItem { Name = "Acme" };
        var model = new TargetModel();

        mapping.Apply(item, model);

        model.CustomerName.ShouldBe("Acme");
    }

    [Fact]
    public async Task AsyncLovFieldMapping_ApplyAsync_Should_Apply_The_Mapping_Before_Invoking_The_Async_Action()
    {
        var actionSawMappedValue = false;
        var actionRan = false;
        var mapping = new AsyncLovFieldMapping<SourceItem, TargetModel, string>(
            i => i.Name,
            m => m.CustomerName,
            async (_, m, _, _) =>
            {
                // If the basic mapping had not already run, this would still be null.
                actionSawMappedValue = m.CustomerName == "Acme";
                actionRan = true;
                await Task.CompletedTask;
            });
        var item = new SourceItem { Name = "Acme" };
        var model = new TargetModel();

        await mapping.ApplyAsync(item, model, A.Fake<IServiceProvider>(), Xunit.TestContext.Current.CancellationToken);

        model.CustomerName.ShouldBe("Acme");
        actionRan.ShouldBeTrue();
        actionSawMappedValue.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentException_When_The_Source_Expression_Is_Not_A_Property_Access()
    {
        Should.Throw<ArgumentException>(() =>
            new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name.ToUpper(), m => m.CustomerName));
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentException_When_The_Target_Expression_Is_Not_A_Property_Access()
    {
        Should.Throw<ArgumentException>(() =>
            new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName.ToUpper()));
    }

    [Fact]
    public void AsyncLovFieldMapping_Constructor_Should_Throw_ArgumentException_For_A_Non_Property_Expression()
    {
        Should.Throw<ArgumentException>(() =>
            new AsyncLovFieldMapping<SourceItem, TargetModel, string>(i => i.Name.ToUpper(), m => m.CustomerName));
    }

    private sealed class SourceItem
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TargetModel
    {
        public string CustomerName { get; set; } = string.Empty;
    }
}
