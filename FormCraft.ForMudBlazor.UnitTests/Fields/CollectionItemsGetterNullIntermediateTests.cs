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

        // Assert - renders the same "no items" state a genuinely empty (but readable) collection
        // would, rather than throwing.
        component.Markup.ShouldContain("No nested items");
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
}
