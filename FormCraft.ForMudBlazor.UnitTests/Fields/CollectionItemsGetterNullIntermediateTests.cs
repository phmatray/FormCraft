using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// A collection field bound through a nested path whose intermediate is null must not crash on
/// render — the render-path sibling of #408's validation-path guard (#419). Since #203 every field,
/// ordinary or collection, renders through the ordinary render pipeline, and
/// <c>CollectionFieldComponent&lt;TModel, TItem&gt;.Items</c> reads
/// <c>Configuration.CollectionAccessor(Model)</c> on essentially the first line of its own markup —
/// so an unreadable nested binding crashed the component the instant it rendered, before validation
/// ever ran.
/// </summary>
public class CollectionItemsGetterNullIntermediateTests : MudBlazorTestBase
{
    [Fact]
    public void Should_Render_As_Empty_When_The_Collections_Nested_Binding_Intermediate_Is_Null()
    {
        // Arrange - the collection is bound through `x => x.Details!.Items`, whose intermediate
        // (Details) is null. Before this fix, the Items getter called
        // Configuration.CollectionAccessor(Model) unguarded and threw a NullReferenceException.
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .WithEmptyText("No nested items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = null };

        // Act - must not throw.
        var component = this.RenderItemForm(model, config);

        // Assert - must not throw, and (#433) shows the distinct unreadable-binding message rather
        // than the configured EmptyText: a broken binding and a genuinely empty collection must not
        // look the same to the user.
        component.Markup.ShouldContain("could not be read from the model");
        component.Markup.ShouldNotContain("No nested items");
        component.FindComponents<MudBlazorTextFieldComponent<ChildItem>>().ShouldBeEmpty();
    }

    /// <summary>
    /// Models a nested collection scenario. <see cref="Details"/> is nullable to test the render
    /// guard for null-intermediate bindings. The structure mirrors
    /// <c>CollectionFieldValidatorTests.OrderModel</c> (#408).
    /// </summary>
    public class ParentModel
    {
        public DetailModel? Details { get; set; }
    }

    public class DetailModel
    {
        public List<ChildItem> Items { get; set; } = new();
    }

    /// <remarks>
    /// Carries a second property beyond <c>ProductName</c> deliberately —
    /// <c>CollectionItemShapeGuardTests</c> fails the build for a nested type whose shape duplicates
    /// a <c>CollectionItemFixture</c> item (a bare <c>string</c> is <see cref="OrderItem"/>'s exact
    /// shape). <c>Quantity</c> is otherwise unused by this suite.
    /// </remarks>
    public class ChildItem
    {
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }

    /// <summary>
    /// #433: Add and Remove must be inert (not just silently no-op) while the binding is unreadable —
    /// neither control renders at all, matching the existing HasReachedMax/HasReachedMin precedent for
    /// "not actionable right now".
    /// </summary>
    [Fact]
    public void Add_Button_And_Delete_Buttons_Should_Not_Render_When_The_Binding_Is_Unreadable()
    {
        // Arrange - same unreadable binding as the render-crash test above, but with Add/Remove
        // explicitly allowed so there is something for the gate to hide.
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .AllowAdd()
                .AllowRemove()
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = null };

        // Act
        var component = this.RenderItemForm(model, config);

        // Assert - neither control is in the render tree at all (AC1/AC2).
        component.FindAll("button").Any(b => b.TextContent.Contains("Add Item")).ShouldBeFalse();
        component.FindComponents<MudIconButton>()
            .Any(b => b.Instance.UserAttributes.TryGetValue("aria-label", out var label)
                      && (label as string) == "Remove item")
            .ShouldBeFalse();
    }

    /// <summary>
    /// #433 AC3: the diagnostic fires once for the field, not once per render — the same
    /// once-per-(diagnostic, field) contract every other MudBlazor collection diagnostic honours.
    /// </summary>
    [Fact]
    public void A_Warning_Should_Be_Logged_Exactly_Once_For_An_Unreadable_Binding()
    {
        // Arrange
        var logs = new TestSupport.CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = null };

        // Act - three renders of the same still-unreadable binding.
        var component = this.RenderItemForm(model, config);
        component.Render();
        component.Render();

        // Assert - one warning, not three.
        logs.Warnings.Count(w => w.Contains("could not read its bound collection from the model")).ShouldBe(1);
    }

    /// <summary>
    /// #433 AC5: a binding that recovers re-enables Add/Remove with no stale disabled state, and a
    /// click after recovery reaches the real model list rather than a throwaway one.
    /// </summary>
    [Fact]
    public async Task Recovering_The_Binding_Should_Re_Enable_Add_And_Let_It_Reach_The_Real_List()
    {
        // Arrange - starts unreadable.
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .WithEmptyText("No nested items")
                .AllowAdd()
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = null };
        var component = this.RenderItemForm(model, config);
        component.FindAll("button").Any(b => b.TextContent.Contains("Add Item")).ShouldBeFalse();

        // Act - the intermediate becomes readable again, the way #419/#433's scenario describes.
        model.Details = new DetailModel();
        component.Render();

        // Assert - EmptyText is back (not the unreadable message), and Add works normally again.
        component.Markup.ShouldContain("No nested items");
        component.Markup.ShouldNotContain("could not be read from the model");
        await component.InvokeAsync(() =>
            component.FindAll("button").First(b => b.TextContent.Contains("Add Item")).Click());
        model.Details!.Items.Count.ShouldBe(1);
    }

    /// <summary>
    /// #433 edge case: a genuinely empty but readable collection must keep rendering exactly as it
    /// does today — the two states must not merge into one bucket.
    /// </summary>
    [Fact]
    public void A_Genuinely_Empty_Readable_Collection_Should_Still_Show_EmptyText()
    {
        // Arrange - Details is non-null but its Items list is empty: readable, just empty.
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .WithEmptyText("No nested items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = new DetailModel() };

        // Act
        var component = this.RenderItemForm(model, config);

        // Assert
        component.Markup.ShouldContain("No nested items");
        component.Markup.ShouldNotContain("could not be read from the model");
    }

    /// <summary>
    /// #433 (found in review): a click can arrive after the model became unreadable but before this
    /// field re-rendered to reflect it — e.g. an external mutation that does not itself trigger this
    /// field's own re-render. <c>AddItem</c>'s OWN opening guard, not just the render-time gate, must
    /// see the CURRENT state.
    /// </summary>
    /// <remarks>
    /// Asserted on <see cref="CountingChildItem.ConstructedCount"/> — how many <c>new TItem()</c> calls
    /// actually happened — rather than on how many times the accessor itself was invoked: the
    /// post-click render this test doesn't otherwise touch reads <c>Items</c>/<c>Details</c> several
    /// times on its own (the empty-state check, the Add gate, …), which would swamp a simple call
    /// count. Construction is the one thing only <c>AddItem</c>'s own <c>Items.Add(new TItem())</c> can
    /// cause. Before the fix, <c>HasReachedMax</c> read a stale (default <see langword="false"/>) latch
    /// without touching <c>Items</c> at all (<c>MaxItems</c> is <c>0</c>, so the check short-circuits),
    /// so <c>AddItem</c> proceeded to construct one item and append it to a throwaway list that the
    /// very next <see cref="CollectionFieldComponent{TModel,TItem}.Items"/> read discarded.
    /// </remarks>
    [Fact]
    public async Task Clicking_Add_After_An_External_Mutation_Should_Not_Append_To_A_Throwaway_List()
    {
        // Arrange - readable and empty, so Add renders.
        CountingChildItem.ConstructedCount = 0;
        var config = FormBuilder<CountingParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .AllowAdd()
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new CountingParentModel { Details = new CountingDetailModel() };
        var component = this.RenderItemForm(model, config);
        component.FindAll("button").Any(b => b.TextContent.Contains("Add Item")).ShouldBeTrue();

        // Act - the model is mutated directly, without going through this field's own
        // NotifyCollectionChanged/re-render, then the (still-rendered, now stale) Add button is
        // clicked.
        model.Details = null;
        await component.InvokeAsync(() =>
            component.FindAll("button").First(b => b.TextContent.Contains("Add Item")).Click());

        // Assert - AddItem's own HasReachedMax check saw the mutation and returned before ever
        // constructing a new item, let alone appending one to a list nobody keeps.
        CountingChildItem.ConstructedCount.ShouldBe(0);
    }

    /// <summary>
    /// Root model for the accessor-construction-counting regression above (#433, found in review) —
    /// local to this one test, not shared, since no other suite here needs to count constructions.
    /// </summary>
    public class CountingParentModel
    {
        public CountingDetailModel? Details { get; set; }
    }

    public class CountingDetailModel
    {
        public List<CountingChildItem> Items { get; set; } = new();
    }

    /// <remarks>
    /// Carries a second property beyond <c>ProductName</c> for the same reason <see cref="ChildItem"/>
    /// does — see its own remarks.
    /// </remarks>
    public class CountingChildItem
    {
        /// <summary>How many times this type has been constructed since last reset by a test.</summary>
        public static int ConstructedCount;

        public CountingChildItem() => ConstructedCount++;

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
