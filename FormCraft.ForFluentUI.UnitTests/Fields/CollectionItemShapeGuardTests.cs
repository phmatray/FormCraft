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

    [Fact]
    public void TestAssemblyTypes_Should_Scan_This_Assembly_As_Well_As_The_Fixtures()
    {
        // Arrange & Act - the fact above asserts an EMPTY offender list, which passes just as
        // happily when this assembly was never actually scanned (e.g. a `typeof(...).Assembly`
        // typo naming the guard's own assembly instead of this test class's - a plausible slip
        // given how similar the two names are). Pin that the union really does reach both sides,
        // so a mistyped call site fails loudly here instead of passing silently above.
        var types = CollectionItemShapeGuard.TestAssemblyTypes(typeof(CollectionItemShapeGuardTests).Assembly)
            .ToList();

        // Assert
        types.ShouldContain(typeof(CollectionItemShapeGuardTests), "this assembly's own types must be in the universe");
        types.ShouldContain(typeof(OrderModel), "the fixture's shared types must be in the universe too");
    }
}
