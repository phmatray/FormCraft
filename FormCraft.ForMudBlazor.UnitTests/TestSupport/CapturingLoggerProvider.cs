using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.TestSupport;

/// <summary>
/// Collects warning-level log messages so a diagnostic can be asserted on.
/// </summary>
/// <remarks>
/// The single <c>CapturingLoggerProvider</c> for the MudBlazor test project — <c>ShrinkLabelDiagnosticsTests</c>
/// and <c>ShrinkLabelDiagnosticCollectorTests</c> used to carry their own private copies; both were
/// folded into this one in #305.
/// <para>
/// The list is lock-guarded because a diagnostic may be emitted from a render that bUnit runs on its
/// own dispatcher thread, and an unsynchronised <see cref="List{T}"/> can tear under that.
/// </para>
/// </remarks>
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
