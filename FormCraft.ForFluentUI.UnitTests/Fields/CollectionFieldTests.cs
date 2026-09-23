namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// Collection (one-to-many) fields render in the Fluent adapter, with their item fields dispatched
/// through <see cref="IFieldRendererService"/> exactly as ordinary fields are (#278).
/// </summary>
/// <remarks>
/// <para>
/// This is the whole reason the slice is cheap. Before #203 a new adapter had to reimplement
/// "render a field" a second time with a <c>RenderTreeBuilder</c> type switch, and inherit that
/// implementation's drift with it (#146, #177, #184, #190, #209). Since #203 there is one registry,
/// so a Fluent collection gets every field type the adapter already registers, by construction.
/// </para>
/// <para>
/// Uses <see cref="CollectionItemFixture.OrderModel"/>/<see cref="OrderItem"/> — via
/// <see cref="CollectionItemFixture.TextItemForm"/> — rather than a local model pair (#343): this
/// suite's shape (one collection, one string item field, no root-level field) is exactly the
/// fixture's text path.
/// </para>
/// </remarks>
public class CollectionFieldTests : FluentUITestBase
{
    [Fact]
    public void A_Collection_Should_Render_One_Item_Field_Per_Item()
    {
        // Arrange
        var model = new OrderModel { Items = { new OrderItem(), new OrderItem() } };

        // Act
        var component = RenderOrderForm(model);

        // Assert - one text component per row, resolved through the ordinary renderer registry
        component.FindComponents<FluentUITextFieldComponent<OrderItem>>().Count.ShouldBe(2);
    }

    [Fact]
    public void A_Collection_Should_Render_Its_Label_And_Add_Control()
    {
        // Act
        var component = RenderOrderForm(new OrderModel());

        // Assert
        component.Markup.ShouldContain("Order lines");
        component.Find("[data-testid=formcraft-collection-add]").TextContent.ShouldContain("Add line");
    }

    [Fact]
    public void An_Empty_Collection_Should_Show_Its_Empty_Text()
    {
        // Act
        var component = RenderOrderForm(new OrderModel());

        // Assert
        component.Find("[data-testid=formcraft-collection-empty]").TextContent.ShouldContain("No lines yet");
        component.FindComponents<FluentUITextFieldComponent<OrderItem>>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Add_Should_Append_An_Item_And_Render_Another_Row()
    {
        // Arrange
        var model = new OrderModel();
        var component = RenderOrderForm(model);

        // Act
        await component.Find("[data-testid=formcraft-collection-add]").ClickAsync(new());

        // Assert
        model.Items.Count.ShouldBe(1);
        component.FindComponents<FluentUITextFieldComponent<OrderItem>>().Count.ShouldBe(1);
    }

    [Fact]
    public async Task Remove_Should_Drop_The_Clicked_Row()
    {
        // Arrange
        var model = new OrderModel
        {
            Items = { new OrderItem { ProductName = "first" }, new OrderItem { ProductName = "second" } }
        };
        var component = RenderOrderForm(model);

        // Act - remove the first row
        await component.FindAll("[data-testid=formcraft-collection-remove]")[0].ClickAsync(new());

        // Assert
        model.Items.Count.ShouldBe(1);
        model.Items[0].ProductName.ShouldBe("second");
    }

    [Fact]
    public async Task Reorder_Should_Move_A_Row_When_Enabled()
    {
        // Arrange
        var model = new OrderModel
        {
            Items = { new OrderItem { ProductName = "first" }, new OrderItem { ProductName = "second" } }
        };
        var component = RenderOrderForm(model, canReorder: true);

        // Act - move the second row up
        await component.FindAll("[data-testid=formcraft-collection-move-up]")[1].ClickAsync(new());

        // Assert
        model.Items[0].ProductName.ShouldBe("second");
        model.Items[1].ProductName.ShouldBe("first");
    }

    [Fact]
    public void Reorder_Controls_Should_Be_Absent_When_Not_Enabled()
    {
        // Act - the default is CanReorder=false
        var component = RenderOrderForm(new OrderModel { Items = { new OrderItem(), new OrderItem() } });

        // Assert
        component.FindAll("[data-testid=formcraft-collection-move-up]").ShouldBeEmpty();
        component.FindAll("[data-testid=formcraft-collection-move-down]").ShouldBeEmpty();
    }

    [Fact]
    public async Task Editing_An_Item_Field_Should_Write_Through_To_The_Item()
    {
        // Arrange
        var model = new OrderModel { Items = { new OrderItem() } };
        var component = RenderOrderForm(model);

        // Act - drive the item field through its own ValueChanged callback
        var field = component.FindComponent<FluentUITextFieldComponent<OrderItem>>();
        await component.InvokeAsync(() => field.Instance.Context.OnValueChanged.InvokeAsync("widget"));

        // Assert
        model.Items[0].ProductName.ShouldBe("widget");
    }

    [Fact]
    public void Item_Help_Text_Should_Not_Emit_Duplicate_Element_Ids_Across_Rows()
    {
        // Arrange - the help-text id is derived from the field name alone, so rendering it per row
        // once emitted N elements sharing one id: invalid HTML, and every row's aria-describedby
        // resolving to whichever the browser found first.
        var config = CollectionItemFixture.TextItemForm(
            configure: f => f.WithHelpText("The catalogue name."),
            configureCollection: collection => collection
                .WithLabel("Order lines")
                .AllowAdd("Add line")
                .AllowRemove());

        // Act - three rows of the same field
        var component = Render<FormCraftComponent<OrderModel>>(p => p
            .Add(c => c.Model, new OrderModel { Items = { new OrderItem(), new OrderItem(), new OrderItem() } })
            .Add(c => c.Configuration, config));

        // Assert - the hint still shows on every row, but exactly one element claims the id
        var helpElements = component.FindAll(".formcraft-field-help");
        helpElements.Count.ShouldBe(3);
        helpElements.Count(e => !string.IsNullOrEmpty(e.Id)).ShouldBe(1);
        component.FindAll($"#{FieldHelpText.IdFor("ProductName")}").Count.ShouldBe(1);
    }

    [Fact]
    public void A_Required_Item_Field_Should_Announce_Itself_Like_A_Standalone_One()
    {
        // Arrange - the accessibility guarantee must not depend on where the field sits (#199)
        var config = CollectionItemFixture.TextItemForm(
            configure: f => f.Required("Product is required"),
            configureCollection: collection => collection.WithLabel("Order lines"));

        // Act
        var component = Render<FormCraftComponent<OrderModel>>(p => p
            .Add(c => c.Model, new OrderModel { Items = { new OrderItem() } })
            .Add(c => c.Configuration, config));

        // Assert
        component.Find("[aria-required=true]").ShouldNotBeNull();
    }

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderOrderForm(
        OrderModel model,
        bool canReorder = false)
    {
        var config = CollectionItemFixture.TextItemForm(configureCollection: collection =>
        {
            collection
                .WithLabel("Order lines")
                .AllowAdd("Add line")
                .AllowRemove()
                .WithEmptyText("No lines yet");

            if (canReorder)
            {
                collection.AllowReorder();
            }
        });

        return Render<FormCraftComponent<OrderModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));
    }
}
