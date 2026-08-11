using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.TestSupport;

/// <summary>
/// Collects warning-level log messages so a diagnostic can be asserted on.
/// </summary>
/// <remarks>
/// Two private copies of this already exist (<c>ShrinkLabelDiagnosticsTests</c> and
/// <c>ShrinkLabelDiagnosticCollectorTests</c>); this is the shared one, added rather than making a
/// third. The existing copies are deliberately left alone here — folding them in is #205's job, and
/// rewriting two unrelated suites inside a security fix would bury the change that matters.
/// <para>
/// The list is lock-guarded because a diagnostic may be emitted from a render that bUnit runs on its
/// own dispatcher thread, and an unsynchronised <see cref="List{T}"/> can tear under that.
/// </para>
/// </remarks>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<string> _warnings = [];

    /// <summary>A snapshot of the warnings captured so far.</summary>
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
            if (logLevel < LogLevel.Warning)
            {
                return;
            }

            lock (warnings)
            {
                warnings.Add(formatter(state, exception));
            }
        }
    }
}
