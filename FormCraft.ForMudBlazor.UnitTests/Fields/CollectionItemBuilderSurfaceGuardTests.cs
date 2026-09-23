using System.Reflection;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests for <see cref="CollectionItemBuilderSurfaceGuard"/> — the check that fails the build when a
/// <see cref="CollectionItemFixture"/> item-form builder skips, misplaces, or silently drops its
/// <c>configureCollection</c> callback (#350).
/// </summary>
/// <remarks>
/// ⚠️ Half of this file exists to prove the guard can still FAIL. "Zero offenders over the real
/// fixture" is the assertion CI reads, and it is also the assertion that keeps passing after the
/// detection path silently breaks. So every violation class the guard claims to catch is exercised
/// here against a deliberately-offending fake builder, alongside one fully-conforming fake that must
/// report no offence at all — the negative control without which "always empty" would pass just as
/// happily as a real check.
/// </remarks>
public class CollectionItemBuilderSurfaceGuardTests
{
    [Fact]
    public void No_CollectionItemFixture_Builder_Should_Skip_Its_Collection_Callback()
    {
        // Arrange - TwoCollectionItemForm is the one deliberate exception: it wires TWO collections
        // through one form, and a single configureCollection callback has no way to say which of them
        // it targets, so the uniform trailing-parameter contract does not apply to it.
        var allowed = new HashSet<MethodInfo>
        {
            typeof(CollectionItemFixture).GetMethod(nameof(CollectionItemFixture.TwoCollectionItemForm))!,
        };

        // Act
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(
            CollectionItemBuilderSurfaceGuard.ItemFormBuilders(typeof(CollectionItemFixture)),
            allowed);

        // Assert - the message has to name the offender AND say what to do, because the reader is a
        // contributor who has just watched a green build turn red on a builder they may not have
        // touched directly.
        offenders.ShouldBeEmpty(
            "Every CollectionItemFixture item-form builder must declare a trailing, optional "
            + "configureCollection parameter and invoke it AFTER its own WithLabel/WithItemForm calls "
            + "(#300, #350):\n  " + string.Join("\n  ", offenders.Select(o => o.ToString())));
    }

    [Fact]
    public void ItemFormBuilders_Should_Only_Return_Members_Returning_IFormConfiguration()
    {
        // Arrange & Act - CollectionItemFixture also declares model factories (NewOrder, NewBasket,
        // NewMixedItems, ...) that return plain models, not form configurations.
        var builders = CollectionItemBuilderSurfaceGuard.ItemFormBuilders(typeof(CollectionItemFixture)).ToList();

        // Assert
        builders.ShouldContain(m => m.Name == nameof(CollectionItemFixture.TextItemForm));
        builders.ShouldNotContain(m => m.Name == nameof(CollectionItemFixture.NewOrder));
        builders.ShouldNotContain(m => m.Name == nameof(CollectionItemFixture.NewMixedItems));
    }

    [Fact]
    public void FindOffenders_Should_Report_No_Offence_For_A_Conforming_Builder()
    {
        // Arrange & Act
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(new[] { Method(nameof(Fakes.Conforming)) });

        // Assert - the negative control: without this, a guard that flagged everything would also
        // report the real fixture's builders as offenders, just as "wrongly", and nothing here would
        // catch it.
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void FindOffenders_Should_Flag_A_Builder_With_No_Trailing_ConfigureCollection_Parameter()
    {
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(new[] { Method(nameof(Fakes.MissingCallback)) });

        offenders.ShouldHaveSingleItem();
        offenders[0].Reason.ShouldContain("no Action<CollectionFieldBuilder");
    }

    [Fact]
    public void FindOffenders_Should_Flag_A_Builder_Whose_ConfigureCollection_Is_Not_Last()
    {
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(new[] { Method(nameof(Fakes.CallbackNotLast)) });

        offenders.ShouldHaveSingleItem();
        offenders[0].Reason.ShouldContain("must be the last parameter");
    }

    [Fact]
    public void FindOffenders_Should_Flag_A_Builder_Whose_ConfigureCollection_Is_Not_Optional()
    {
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(new[] { Method(nameof(Fakes.CallbackNotOptional)) });

        offenders.ShouldHaveSingleItem();
        offenders[0].Reason.ShouldContain("must be optional");
    }

    [Fact]
    public void FindOffenders_Should_Flag_A_Builder_Whose_ConfigureCollection_TModel_Does_Not_Match()
    {
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(new[] { Method(nameof(Fakes.CallbackWrongModel)) });

        offenders.ShouldHaveSingleItem();
        offenders[0].Reason.ShouldContain("but the member returns IFormConfiguration<FakeModel>");
    }

    [Fact]
    public void FindOffenders_Should_Flag_A_Builder_That_Never_Invokes_ConfigureCollection()
    {
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(new[] { Method(nameof(Fakes.CallbackDropped)) });

        offenders.ShouldHaveSingleItem();
        offenders[0].Reason.ShouldContain("did not take effect");
    }

    [Fact]
    public void FindOffenders_Should_Flag_A_Builder_That_Invokes_ConfigureCollection_Before_Its_Own_WithLabel()
    {
        // A reorder-only probe (e.g. AllowReorder()) cannot tell "runs last" from "runs first" - only
        // overriding a value the builder also sets, such as the label, can.
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(new[] { Method(nameof(Fakes.CallbackInvokedFirst)) });

        offenders.ShouldHaveSingleItem();
        offenders[0].Reason.ShouldContain("did not take effect");
    }

    [Fact]
    public void FindOffenders_Should_Turn_A_Throwing_Builder_Into_An_Offence_Rather_Than_Fail_The_Run()
    {
        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(new[] { Method(nameof(Fakes.ThrowsWhenInvoked)) });

        offenders.ShouldHaveSingleItem();
        offenders[0].Reason.ShouldContain("threw when invoked");
    }

    [Fact]
    public void FindOffenders_Should_Skip_A_Builder_In_The_Allowed_Set()
    {
        var missingCallback = Method(nameof(Fakes.MissingCallback));

        var offenders = CollectionItemBuilderSurfaceGuard.FindOffenders(
            new[] { missingCallback },
            new HashSet<MethodInfo> { missingCallback });

        offenders.ShouldBeEmpty();
    }

    private static MethodInfo Method(string name) =>
        typeof(Fakes).GetMethod(name) ?? throw new InvalidOperationException($"No such fake builder: {name}");

    /// <summary>A shape-only fake item, chosen to not collide with any shared item's shape (#297).</summary>
    private sealed class FakeItem
    {
        public Guid Token { get; set; }
    }

    private sealed class FakeModel
    {
        public List<FakeItem> Rows { get; set; } = new();
    }

    /// <summary>A second, distinct model - only used to construct a TModel mismatch.</summary>
    private sealed class OtherFakeModel
    {
        public List<FakeItem> Rows { get; set; } = new();
    }

    /// <summary>
    /// Deliberately non-conforming (and one conforming) item-form builders, one per violation class
    /// this guard claims to catch. Nested so <see cref="CollectionItemShapeGuard"/> never sees them as
    /// candidates for its own, unrelated check.
    /// </summary>
    private static class Fakes
    {
        public static IFormConfiguration<FakeModel> MissingCallback(
            Action<FieldBuilder<FakeItem, Guid>>? configure = null) =>
            FormBuilder<FakeModel>
                .Create()
                .AddCollectionField(x => x.Rows, collection => collection
                    .WithLabel("Rows")
                    .WithItemForm(item => item.AddField(x => x.Token, field =>
                    {
                        field.WithLabel("Token");
                        configure?.Invoke(field);
                    })))
                .Build();

        public static IFormConfiguration<FakeModel> CallbackNotLast(
            Action<CollectionFieldBuilder<FakeModel, FakeItem>>? configureCollection = null,
            Action<FieldBuilder<FakeItem, Guid>>? configure = null) =>
            FormBuilder<FakeModel>
                .Create()
                .AddCollectionField(x => x.Rows, collection =>
                {
                    collection
                        .WithLabel("Rows")
                        .WithItemForm(item => item.AddField(x => x.Token, field =>
                        {
                            field.WithLabel("Token");
                            configure?.Invoke(field);
                        }));
                    configureCollection?.Invoke(collection);
                })
                .Build();

        public static IFormConfiguration<FakeModel> CallbackNotOptional(
            Action<CollectionFieldBuilder<FakeModel, FakeItem>> configureCollection) =>
            FormBuilder<FakeModel>
                .Create()
                .AddCollectionField(x => x.Rows, collection =>
                {
                    collection.WithLabel("Rows").WithItemForm(item => item.AddField(x => x.Token, field => field.WithLabel("Token")));
                    configureCollection(collection);
                })
                .Build();

        public static IFormConfiguration<FakeModel> CallbackWrongModel(
            Action<CollectionFieldBuilder<OtherFakeModel, FakeItem>>? configureCollection = null) =>
            FormBuilder<FakeModel>
                .Create()
                .AddCollectionField(x => x.Rows, collection => collection
                    .WithLabel("Rows")
                    .WithItemForm(item => item.AddField(x => x.Token, field => field.WithLabel("Token"))))
                .Build();

        public static IFormConfiguration<FakeModel> CallbackDropped(
            Action<CollectionFieldBuilder<FakeModel, FakeItem>>? configureCollection = null) =>
            FormBuilder<FakeModel>
                .Create()
                .AddCollectionField(x => x.Rows, collection => collection
                    .WithLabel("Rows")
                    .WithItemForm(item => item.AddField(x => x.Token, field => field.WithLabel("Token"))))
                // configureCollection is deliberately never invoked.
                .Build();

        public static IFormConfiguration<FakeModel> CallbackInvokedFirst(
            Action<CollectionFieldBuilder<FakeModel, FakeItem>>? configureCollection = null) =>
            FormBuilder<FakeModel>
                .Create()
                .AddCollectionField(x => x.Rows, collection =>
                {
                    // Invoked BEFORE the builder's own WithLabel, so any override is clobbered.
                    configureCollection?.Invoke(collection);
                    collection
                        .WithLabel("Rows")
                        .WithItemForm(item => item.AddField(x => x.Token, field => field.WithLabel("Token")));
                })
                .Build();

        public static IFormConfiguration<FakeModel> ThrowsWhenInvoked(
            Action<FieldBuilder<FakeItem, Guid>> configure,
            Action<CollectionFieldBuilder<FakeModel, FakeItem>>? configureCollection = null) =>
            FormBuilder<FakeModel>
                .Create()
                .AddCollectionField(x => x.Rows, collection =>
                {
                    collection
                        .WithLabel("Rows")
                        .WithItemForm(item => item.AddField(x => x.Token, field =>
                        {
                            field.WithLabel("Token");
                            // Dereferences the required delegate unconditionally - the guard passes
                            // null for every parameter but the trailing configureCollection, so this
                            // throws and must become an offence rather than kill the whole run.
                            configure(field);
                        }));
                    configureCollection?.Invoke(collection);
                })
                .Build();

        public static IFormConfiguration<FakeModel> Conforming(
            Action<FieldBuilder<FakeItem, Guid>>? configure = null,
            Action<CollectionFieldBuilder<FakeModel, FakeItem>>? configureCollection = null) =>
            FormBuilder<FakeModel>
                .Create()
                .AddCollectionField(x => x.Rows, collection =>
                {
                    collection
                        .WithLabel("Rows")
                        .WithItemForm(item => item.AddField(x => x.Token, field =>
                        {
                            field.WithLabel("Token");
                            configure?.Invoke(field);
                        }));
                    configureCollection?.Invoke(collection);
                })
                .Build();
    }
}
