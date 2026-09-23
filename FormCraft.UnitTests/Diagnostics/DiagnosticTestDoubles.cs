using Microsoft.Extensions.Logging;

namespace FormCraft.UnitTests.Diagnostics;

/// <summary>
/// Collects warning-level log messages so <c>FormDiagnosticLog.Warn</c> can be asserted on. Mirrors
/// <c>FormCraft.ForMudBlazor.UnitTests.TestSupport.CapturingLoggerProvider</c>, kept local rather than
/// shared across assemblies for one test file's worth of use.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<(string Category, string Message)> _entries = [];

    /// <summary>A snapshot of the warnings captured so far.</summary>
    public IReadOnlyList<string> Warnings
    {
        get
        {
            lock (_entries)
            {
                return _entries.Select(entry => entry.Message).ToList();
            }
        }
    }

    /// <summary>The same warnings, each paired with the logger category it was emitted under.</summary>
    public IReadOnlyList<(string Category, string Message)> Entries
    {
        get
        {
            lock (_entries)
            {
                return _entries.ToList();
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, List<(string Category, string Message)> warnings)
        : ILogger
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
            if (logLevel < LogLevel.Warning)
            {
                return;
            }

            lock (warnings)
            {
                warnings.Add((category, formatter(state, exception)));
            }
        }
    }
}

/// <summary>
/// A service provider whose every resolution throws, standing in for a torn-down circuit. Mirrors
/// <c>FormCraft.ForMudBlazor.UnitTests.TestSupport.ThrowingServiceProvider</c>.
/// </summary>
internal sealed class ThrowingServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) =>
        throw new InvalidOperationException("Cannot access a disposed scope.");
}
