using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Diagnostics;

/// <summary>
/// Direct tests for <see cref="DiagnosticLog"/>, the shared diagnostic emitter (#284).
/// </summary>
/// <remarks>
/// <para>
/// Four diagnostics used to carry a verbatim copy of the same resolve-log-swallow block, identical
/// comment and empty <c>catch</c> included. Folding them into one emitter makes the swallow a single
/// promise rather than four, which is worth testing directly: the guarantee is <b>"never throws"</b>,
/// and each way it could throw is a separate arm the component suites reach only incidentally.
/// </para>
/// <para>
/// The disposed-scope arm is the one that motivated the shape. A diagnostic runs during render, so
/// resolving the logger has to sit <i>inside</i> the guard rather than above it — on a torn-down
/// circuit the resolution itself throws, and a warning nobody asked for would take the form down
/// with it.
/// </para>
/// </remarks>
public class DiagnosticLogTests
{
    private const string Category = "FormCraft.ForMudBlazor.TestDiagnostic";

    [Fact]
    public void Warn_Should_Log_Under_The_Given_Category()
    {
        // Arrange - the category is what routes and mutes a diagnostic, so a caller that passes one
        // must get it. Emitting everything under a single shared category would let a developer
        // muting one diagnostic silence all four without noticing.
        var logs = new CapturingLoggerProvider();

        // Act
        DiagnosticLog.Warn(BuildServices(logs), Category, "Field '{Field}' is misconfigured.", "Phone");

        // Assert
        var entries = logs.Entries;
        entries.Count.ShouldBe(1);
        entries[0].Category.ShouldBe(Category);
    }

    [Fact]
    public void Warn_Should_Format_The_Template_With_Its_Arguments()
    {
        // Arrange - the existing suites assert on rendered message text (ShouldContain on the field
        // name and the offending setting), so the arguments have to reach the formatter in order.
        var logs = new CapturingLoggerProvider();

        // Act
        DiagnosticLog.Warn(
            BuildServices(logs),
            Category,
            "Field '{Field}' holds a value that its mask '{Mask}' rejects.",
            "Phone",
            "(000) 000-0000");

        // Assert
        var warnings = logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Phone");
        warnings[0].ShouldContain("(000) 000-0000");
    }

    [Fact]
    public void Warn_Should_Not_Throw_When_No_Services_Are_Available()
    {
        // Arrange & Act & Assert - a component rendered outside DI has no provider at all. Nothing
        // to log to is not an error; it is the ordinary state of a field rendered in isolation.
        Should.NotThrow(() => DiagnosticLog.Warn(null, Category, "Field '{Field}' is misconfigured.", "Phone"));
    }

    [Fact]
    public void Warn_Should_Not_Throw_When_No_LoggerFactory_Is_Registered()
    {
        // Arrange - an app that never called AddLogging. The provider resolves fine and simply has
        // no factory in it, which is the null-conditional arm rather than the catch.
        var services = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        Should.NotThrow(() => DiagnosticLog.Warn(services, Category, "Field '{Field}' is misconfigured.", "Phone"));
    }

    [Fact]
    public void Warn_Should_Not_Throw_When_Resolving_The_Logger_Throws()
    {
        // Arrange & Act & Assert - the disposed-circuit case, and the reason the resolution lives
        // inside the guard. This is the arm a null-check alone would not survive.
        Should.NotThrow(() => DiagnosticLog.Warn(
            new ThrowingServiceProvider(),
            Category,
            "Field '{Field}' is misconfigured.",
            "Phone"));
    }

    [Fact]
    public void Warn_Should_Not_Log_When_No_LoggerFactory_Is_Registered()
    {
        // Arrange - the other half of the no-factory case: it degrades silently rather than
        // reaching for some other sink.
        //
        // The provider is registered as a bare ILoggerProvider — deliberately, and it is what gives
        // this test teeth. `AddLogging` would register an ILoggerFactory and make it the positive
        // case; leaving the provider out of the container altogether would wire `logs` to nothing,
        // so the assertion below could not fail whatever DiagnosticLog did. Registered but
        // factory-less, the provider is genuinely reachable and the only missing piece is the one
        // door DiagnosticLog is allowed to use.
        var logs = new CapturingLoggerProvider();
        var services = new ServiceCollection()
            .AddSingleton<ILoggerProvider>(logs)
            .BuildServiceProvider();

        // Act
        DiagnosticLog.Warn(services, Category, "Field '{Field}' is misconfigured.", "Phone");

        // Assert - the premise first, so a future edit that registers a factory fails loudly here
        // rather than turning the real assertion into a tautology again.
        services.GetService<ILoggerFactory>().ShouldBeNull();
        logs.Warnings.ShouldBeEmpty();
    }

    private static IServiceProvider BuildServices(ILoggerProvider provider)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(provider));
        return services.BuildServiceProvider();
    }
}
