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

    /// <summary>
    /// The same warnings, each paired with the logger category it was emitted under (#284).
    /// </summary>
    /// <remarks>
    /// The category is how a caller tells one diagnostic from another — <c>DiagnosticLog.Warn</c>
    /// takes it as a parameter, so "was this logged under the category it was given" is a claim
    /// only this can check. Every existing suite asserts on <see cref="Warnings"/> and is unaffected.
    /// </remarks>
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
