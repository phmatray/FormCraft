using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Direct tests for <see cref="ShrinkLabelDiagnosticCollector"/> (#181). The aggregation logic
/// has failure modes a component test cannot reach cheaply — fields that first render on a
/// later pass, and a logging stack that throws — so it is exercised here in isolation.
/// </summary>
public class ShrinkLabelDiagnosticCollectorTests
{
    [Fact]
    public void Should_Report_Fields_That_Appear_After_The_First_Flush()
    {
        // Arrange - a field revealed later by a visibility condition, an expanded group, or a
        // newly added collection row reports on a subsequent pass. A one-shot latch would drop
        // it silently, which is worse than the duplicate warning it avoids.
        var logs = new CapturingLoggerProvider();
        var services = BuildServices(logs);
        var collector = new ShrinkLabelDiagnosticCollector();

        collector.Report("Alpha", "Alpha", "a Placeholder");
        collector.Flush(services);

        // Act - Beta becomes visible on a later render
        collector.Report("Beta", "Beta", "a Placeholder");
        collector.Flush(services);

        // Assert
        logs.Warnings.Count.ShouldBe(2);
        logs.Warnings[0].ShouldContain("Alpha");
        logs.Warnings[1].ShouldContain("Beta");
        logs.Warnings[1].ShouldNotContain("Alpha");
    }

    [Fact]
    public void Should_Not_Repeat_A_Field_On_Later_Flushes()
    {
        // Arrange - re-renders are constant while a user types; the same conflict must not
        // re-log each time.
        var logs = new CapturingLoggerProvider();
        var services = BuildServices(logs);
        var collector = new ShrinkLabelDiagnosticCollector();
        collector.Report("Alpha", "Alpha", "a Placeholder");

        // Act
        collector.Flush(services);
        collector.Flush(services);
        collector.Report("Alpha", "Alpha", "a Placeholder");
        collector.Flush(services);

        // Assert
        logs.Warnings.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_Not_Throw_When_Resolving_The_Logger_Throws()
    {
        // Arrange - the diagnostic runs inside OnAfterRender, so an exception here would take
        // the form down. A logging stack that throws must be swallowed.
        var collector = new ShrinkLabelDiagnosticCollector();
        collector.Report("Alpha", "Alpha", "a Placeholder");

        // Act & Assert
        Should.NotThrow(() => collector.Flush(new ThrowingServiceProvider()));
    }

    [Fact]
    public void Should_Not_Throw_When_No_Services_Are_Available()
    {
        var collector = new ShrinkLabelDiagnosticCollector();
        collector.Report("Alpha", "Alpha", "a Placeholder");

        Should.NotThrow(() => collector.Flush(null));
    }

    private static IServiceProvider BuildServices(ILoggerProvider provider)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(provider));
        return services.BuildServiceProvider();
    }

    private sealed class ThrowingServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            throw new InvalidOperationException("Cannot access a disposed scope.");
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _warnings = [];

        public IReadOnlyList<string> Warnings => _warnings;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_warnings);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<string> warnings) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (logLevel >= LogLevel.Warning)
                {
                    warnings.Add(formatter(state, exception));
                }
            }
        }
    }
}
