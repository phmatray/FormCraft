using Microsoft.Extensions.Logging;

namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovMappingProcessor"/> (#459) — proves the model's properties
/// after a mapping run, not that a builder returns itself.
/// </summary>
public class LovMappingProcessorTests
{
    [Fact]
    public void ApplyMappings_Should_Write_Every_Mapped_Field_To_The_Model()
    {
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());
        var item = new SourceItem { Name = "Acme", Code = "ACM" };
        var model = new TargetModel();
        List<ILovFieldMapping> mappings =
        [
            new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName),
            new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Code, m => m.CustomerCode)
        ];

        processor.ApplyMappings(model, item, mappings);

        model.CustomerName.ShouldBe("Acme");
        model.CustomerCode.ShouldBe("ACM");
    }

    [Fact]
    public async Task ApplyMappingsAsync_Should_Apply_Both_Sync_And_Async_Mappings()
    {
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());
        var item = new SourceItem { Name = "Acme", Code = "ACM" };
        var model = new TargetModel();
        List<ILovFieldMapping> mappings =
        [
            new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName),
            new AsyncLovFieldMapping<SourceItem, TargetModel, string>(
                i => i.Code,
                m => m.CustomerCode,
                async (_, m, _, _) =>
                {
                    await Task.Yield();
                    m.AsyncActionRan = true;
                })
        ];

        await processor.ApplyMappingsAsync(model, item, mappings, Xunit.TestContext.Current.CancellationToken);

        model.CustomerName.ShouldBe("Acme");
        model.CustomerCode.ShouldBe("ACM");
        model.AsyncActionRan.ShouldBeTrue();
    }

    [Fact]
    public void ApplyMappings_Should_Log_A_Throwing_Mapping_And_Still_Apply_Its_Siblings()
    {
        var logger = new RecordingLogger();
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>(), logger);
        var item = new SourceItem { Name = "Acme", Code = "ACM" };
        var model = new TargetModel();
        var throwingMapping = A.Fake<ILovFieldMapping>();
        A.CallTo(() => throwingMapping.Apply(A<object>._, A<object>._))
            .Throws<InvalidOperationException>();
        List<ILovFieldMapping> mappings =
        [
            throwingMapping,
            new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName)
        ];

        processor.ApplyMappings(model, item, mappings);

        model.CustomerName.ShouldBe("Acme");
        logger.Warnings.ShouldNotBeEmpty();
    }

    [Fact]
    public void ApplyMappings_Should_Return_Without_Throwing_When_Model_Is_Null()
    {
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());
        List<ILovFieldMapping> mappings =
            [new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName)];

        Should.NotThrow(() => processor.ApplyMappings<TargetModel>(null!, new SourceItem(), mappings));
    }

    [Fact]
    public void ApplyMappings_Should_Return_Without_Throwing_When_SelectedItem_Is_Null()
    {
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());
        List<ILovFieldMapping> mappings =
            [new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName)];

        Should.NotThrow(() => processor.ApplyMappings(new TargetModel(), null!, mappings));
    }

    [Fact]
    public void ApplyMappings_Should_Return_Without_Throwing_When_Mappings_Is_Null_Or_Empty()
    {
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());

        Should.NotThrow(() => processor.ApplyMappings(new TargetModel(), new SourceItem(), null!));
        Should.NotThrow(() => processor.ApplyMappings(new TargetModel(), new SourceItem(), []));
    }

    [Fact]
    public async Task ApplyMappingsAsync_Should_Return_Without_Throwing_When_Model_Or_Item_Or_Mappings_Are_Missing()
    {
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());
        List<ILovFieldMapping> mappings =
            [new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName)];

        await Should.NotThrowAsync(() => processor.ApplyMappingsAsync<TargetModel>(null!, new SourceItem(), mappings));
        await Should.NotThrowAsync(() => processor.ApplyMappingsAsync(new TargetModel(), null!, mappings));
        await Should.NotThrowAsync(() => processor.ApplyMappingsAsync(new TargetModel(), new SourceItem(), []));
    }

    [Fact]
    public void ClearMappings_Should_Reset_Every_Mapped_Property_To_Its_Types_Default()
    {
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());
        var model = new TargetModel { CustomerName = "Acme", Age = 42 };
        List<ILovFieldMapping> mappings =
        [
            new LovFieldMapping<SourceItem, TargetModel, string>(i => i.Name, m => m.CustomerName),
            new LovFieldMapping<SourceItem, TargetModel, int>(i => i.Score, m => m.Age)
        ];

        processor.ClearMappings(model, mappings);

        model.CustomerName.ShouldBeNull();
        model.Age.ShouldBe(0);
    }

    private sealed class SourceItem
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private sealed class TargetModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool AsyncActionRan { get; set; }
    }

    /// <summary>Captures Warning-level log entries so a swallowed exception can be asserted on.</summary>
    private sealed class RecordingLogger : ILogger<LovMappingProcessor>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
