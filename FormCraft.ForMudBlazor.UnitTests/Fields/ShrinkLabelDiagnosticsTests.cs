using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests the diagnostic that warns when <c>ShrinkLabel=false</c> cannot be honoured (#181).
/// <para>
/// MudBlazor ORs ShrinkLabel with has-value / has-placeholder / has-start-adornment before
/// emitting the "mud-shrink" class, so the setting is silently inert on a field carrying a
/// placeholder or a start adornment. These tests assert that FormCraft says so, and — just as
/// important — that it stays quiet in the cases where the setting works or where the override
/// is correct behaviour.
/// </para>
/// </summary>
public class ShrinkLabelDiagnosticsTests : MudBlazorTestBase
{
    private readonly CapturingLoggerProvider _logs = new();

    public ShrinkLabelDiagnosticsTests()
    {
        Services.AddLogging(builder => builder.AddProvider(_logs));
    }

    [Fact]
    public void Should_Warn_When_ShrinkLabel_False_Meets_A_Placeholder()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithPlaceholder("user@example.com")
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderForm(config);

        // Assert - names the field, so a form of many fields points at the right one
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Email");
        warnings[0].ShouldContain("Placeholder");
    }

    [Fact]
    public void Should_Not_Warn_When_ShrinkLabel_False_Is_Honoured()
    {
        // Arrange - no placeholder, so the setting genuinely takes effect
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Warn_For_Default_Configuration()
    {
        // Arrange - ShrinkLabel unset; there is no conflict to report
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithPlaceholder("user@example.com"))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderForm(
        IFormConfiguration<TestModel> config, bool? defaultShrinkLabel = null)
    {
        return Render<FormCraftComponent<TestModel>>(parameters =>
        {
            parameters.Add(p => p.Model, new TestModel());
            parameters.Add(p => p.Configuration, config);
            if (defaultShrinkLabel is { } shrink)
            {
                parameters.Add(p => p.DefaultShrinkLabel, shrink);
            }
        });
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Minimal ILoggerProvider that records warning-level messages so tests can assert on
    /// what a developer would actually see in their console.
    /// </summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _warnings = [];

        public IReadOnlyList<string> Warnings
        {
            get
            {
                lock (_warnings)
                {
                    return _warnings.ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose()
        {
        }

        private void Record(string message)
        {
            lock (_warnings)
            {
                _warnings.Add(message);
            }
        }

        private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
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
                    provider.Record(formatter(state, exception));
                }
            }
        }
    }
}
