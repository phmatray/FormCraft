namespace FormCraft.ForMudBlazor.UnitTests.TestSupport;

/// <summary>
/// A service provider whose every resolution throws, standing in for a torn-down circuit.
/// </summary>
/// <remarks>
/// The realistic failure, and the reason the diagnostics resolve their logger <i>inside</i> the
/// guard rather than above it: on a disconnected Blazor circuit the scope is disposed, and asking it
/// for an <c>ILoggerFactory</c> throws rather than returning null. A diagnostic that let that escape
/// would take down a render for the sake of a warning nobody asked for.
/// </remarks>
internal sealed class ThrowingServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) =>
        throw new InvalidOperationException("Cannot access a disposed scope.");
}
