namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// Runs <see cref="CollectionItemShapeGuard"/> — the check that fails the build when a suite
/// re-declares a collection-item shape <see cref="CollectionItemFixture"/> already provides
/// (#297) — over this assembly too (#343).
/// </summary>
/// <remarks>
/// The guard's own behaviour (signature comparison, ownership, the allowlist escape hatch) is
/// pinned once, in <c>FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemShapeGuardTests</c>.
/// This file is deliberately thin: it is the entry point that makes the guard run over the Fluent
/// UI suite's own types, not a second copy of those tests.
/// </remarks>
public class CollectionItemShapeGuardTests
{
    [Fact]
    public void No_Suite_Should_Re_Declare_A_Collection_Item_Shape_The_Fixture_Provides()
    {
        // Arrange & Act - the guard as CI runs it, over this assembly.
        var offenders = CollectionItemShapeGuard.FindOffenders(
            CollectionItemShapeGuard.TestAssemblyTypes(typeof(CollectionItemShapeGuardTests).Assembly));

        // Assert - the message has to name the offender AND say what to do, because the reader is a
        // contributor who has just watched a green build turn red on a file they did not touch.
        offenders.ShouldBeEmpty(
            "These types re-declare a shape CollectionItemFixture already provides. Use the fixture's "
            + "model and item-form builder instead (#205, #258, #282, #343). If a local copy is "
            + "genuinely warranted, pass it to FindOffenders' allowlist with the reason:\n  "
            + string.Join("\n  ", offenders.Select(o => o.Detail)));
    }
}
