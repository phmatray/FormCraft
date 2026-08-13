namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Self-tests for <see cref="CollectionItemFixture"/> (#205).
/// <para>
/// The fixture exists so an attribute suite declares only the behaviour it tests, and so every such
/// suite covers all five collection-item field types by default instead of by remembering to. That
/// only holds if the fixture itself is guarded: these tests pin the properties the suites rely on and
/// cannot check for themselves.
/// </para>
/// <para>
/// <b>All five types render.</b> Text, numeric, date and decimal all reach a MudBlazor input that
/// binds the shared presentation attributes; the boolean one bypasses them entirely, which is why
/// several suites pin an attribute as *inert* there. A fixture that silently stopped producing one of
/// them would turn those tests green for the wrong reason, so each is asserted to reach its MudBlazor
/// component.
/// <para>
/// ⚠️ <b>Keep this count in step with <see cref="CollectionItemFixture"/>'s own list.</b> That type's
/// doc records why: a miscounted comment about which renderers a shared block fed once survived a
/// copy and hid the date path from coverage until review caught it. The count was four until #258
/// added the decimal type; a reader who trusts a stale number will not know to add the assertion.
/// </para>
/// </para>
/// <para>
/// <b>Beyond the field types</b>, the fixture carries two shapes — a root-level field beside the
/// collection (<c>RootFieldAndItemForm</c>), and one item form holding four fields at once
/// (<c>MultiFieldItemForm</c>, #282) — both guarded here too.
/// </para>
/// <para>
/// <b>The seed is the caller's choice.</b> <c>CollectionRequiredTests</c> needs a blank
/// <c>ProductName</c> for its validation tests; <c>CollectionAdornmentTests</c> seeds it with
/// <c>"Widget"</c>. Picking one and applying it everywhere would silently change a test's meaning —
/// the exact failure the fixture is supposed to prevent — so the factories default to the model's own
/// default and take an explicit seed.
/// </para>
/// </summary>
public class CollectionItemFixtureTests : MudBlazorTestBase
{
    [Fact]
    public void TextItemForm_Should_Render_The_Text_Path()
    {
        // Arrange & Act
        var component = this.RenderItemForm(
            CollectionItemFixture.NewOrder(),
            CollectionItemFixture.TextItemForm());

        // Assert - the string path reaches MudTextField, labelled as the suites expect
        component.FindComponent<MudTextField<string>>().Instance.Label.ShouldBe("Product");
    }

    [Fact]
    public void NumericItemForm_Should_Render_The_Numeric_Path()
    {
        // Arrange & Act
        var component = this.RenderItemForm(
            CollectionItemFixture.NewBasket(),
            CollectionItemFixture.NumericItemForm());

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Label.ShouldBe("Quantity");
    }

    [Fact]
    public void DateItemForm_Should_Render_The_Date_Path()
    {
        // Arrange & Act
        var component = this.RenderItemForm(
            CollectionItemFixture.NewAppointment(),
            CollectionItemFixture.DateItemForm());

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Label.ShouldBe("When");
    }

    [Fact]
    public void BooleanItemForm_Should_Render_The_Boolean_Path()
    {
        // Arrange & Act - the path that bypasses AddCommonFieldAttributes
        var component = this.RenderItemForm(
            CollectionItemFixture.NewBasket(),
            CollectionItemFixture.BooleanItemForm());

        // Assert
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Gift");
    }

    [Fact]
    public void DecimalItemForm_Should_Render_The_Decimal_Path()
    {
        // Arrange & Act - a fifth *type*, not a fifth path: MudNumericField<decimal> is a distinct
        // closed generic from MudNumericField<int>, so a suite asserting on one says nothing about
        // the other. CollectionCultureTests is the named consumer (#218 is about decimal parsing).
        var component = this.RenderItemForm(
            CollectionItemFixture.NewPricedBasket(),
            CollectionItemFixture.DecimalItemForm());

        // Assert
        component.FindComponent<MudNumericField<decimal>>().Instance.Label.ShouldBe("Price");
    }

    [Fact]
    public void NewPricedBasket_Should_Default_Blank_And_Honour_Its_Seed()
    {
        // Arrange & Act
        var blank = CollectionItemFixture.NewPricedBasket();
        var seeded = CollectionItemFixture.NewPricedBasket(12.5m);

        // Assert - in the model, and in what the field actually renders
        blank.Lines.ShouldHaveSingleItem();
        blank.Lines[0].Price.ShouldBe(0m);
        seeded.Lines[0].Price.ShouldBe(12.5m);

        var component = this.RenderItemForm(seeded, CollectionItemFixture.DecimalItemForm());
        component.FindComponent<MudNumericField<decimal>>().Instance.Value.ShouldBe(12.5m);
    }

    [Fact]
    public void Each_Item_Form_Should_Apply_The_Callers_Configuration()
    {
        // Arrange & Act - the callback is how a suite declares the behaviour it is testing, so it
        // must reach the field on every path that takes one. A fixture that dropped it would leave
        // the suites asserting against an unconfigured field.
        var text = this.RenderItemForm(
            CollectionItemFixture.NewOrder(),
            CollectionItemFixture.TextItemForm(field => field.WithLabel("Renamed")));
        var numeric = this.RenderItemForm(
            CollectionItemFixture.NewBasket(),
            CollectionItemFixture.NumericItemForm(field => field.WithLabel("Renamed")));
        var date = this.RenderItemForm(
            CollectionItemFixture.NewAppointment(),
            CollectionItemFixture.DateItemForm(field => field.WithLabel("Renamed")));
        var boolean = this.RenderItemForm(
            CollectionItemFixture.NewBasket(),
            CollectionItemFixture.BooleanItemForm(field => field.WithLabel("Renamed")));
        var dec = this.RenderItemForm(
            CollectionItemFixture.NewPricedBasket(),
            CollectionItemFixture.DecimalItemForm(field => field.WithLabel("Renamed")));

        // Assert - the callback runs after the default label, so it can override it
        text.FindComponent<MudTextField<string>>().Instance.Label.ShouldBe("Renamed");
        numeric.FindComponent<MudNumericField<int>>().Instance.Label.ShouldBe("Renamed");
        date.FindComponent<MudDatePicker>().Instance.Label.ShouldBe("Renamed");
        boolean.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Renamed");
        dec.FindComponent<MudNumericField<decimal>>().Instance.Label.ShouldBe("Renamed");
    }

    [Fact]
    public void MultiFieldItemForm_Should_Render_All_Four_Fields_In_One_Row()
    {
        // Arrange & Act - the shape every other builder here is not: ONE item form holding four
        // fields at once, rather than one field per model pair. CollectionAdornmentTests and
        // CollectionRenderCharacterisationTests are the named consumers (#282) — both were carrying
        // their own MixedRow, and the two copies had already drifted apart.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewMixedItems(new MixedItem()),
            CollectionItemFixture.MultiFieldItemForm());

        // Assert - one of each component type, in the order the builder declares them. MudDatePicker
        // embeds a MudTextField<string> of its own, so the text field is selected by label rather
        // than by FindComponent, which would return whichever came first in the tree.
        component.FindComponents<MudTextField<string>>()
            .Select(f => f.Instance.Label)
            .ShouldContain("Name");
        component.FindComponent<MudNumericField<int>>().Instance.Label.ShouldBe("Quantity");
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Gift");
        component.FindComponent<MudDatePicker>().Instance.Label.ShouldBe("When");
    }

    [Fact]
    public void MultiFieldItemForm_Should_Route_Each_Callback_To_Its_Own_Field()
    {
        // Arrange & Act - four callbacks, four destinations. A builder that wired two of them to the
        // same field, or dropped one, would leave a suite configuring nothing while still looking
        // configured at the call site. Renamed apart so the assertions can tell them apart: with a
        // shared label this test would pass on a builder that applied one callback four times.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewMixedItems(new MixedItem()),
            CollectionItemFixture.MultiFieldItemForm(
                configureText: field => field.WithLabel("Renamed text"),
                configureNumeric: field => field.WithLabel("Renamed numeric"),
                configureBoolean: field => field.WithLabel("Renamed boolean"),
                configureDate: field => field.WithLabel("Renamed date")));

        // Assert
        component.FindComponents<MudTextField<string>>()
            .Select(f => f.Instance.Label)
            .ShouldContain("Renamed text");
        component.FindComponent<MudNumericField<int>>().Instance.Label.ShouldBe("Renamed numeric");
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Renamed boolean");
        component.FindComponent<MudDatePicker>().Instance.Label.ShouldBe("Renamed date");
    }

    [Fact]
    public void MultiFieldItemForm_Should_Apply_The_Callers_Collection_Configuration()
    {
        // Arrange & Act - the collection itself, not just its fields. CollectionAdornmentTests'
        // reorder test needs .AllowReorder(), which is a property of the COLLECTION; without this
        // callback that suite would have to hand-roll the whole configuration and would keep its own
        // model copy with it, which is the duplication this fixture exists to remove.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewMixedItems(new MixedItem(), new MixedItem()),
            CollectionItemFixture.MultiFieldItemForm(
                configureCollection: collection => collection.AllowReorder()));

        // Assert - reorder controls only render when the collection allows reordering
        component.FindAll("button[aria-label='Move up']").ShouldNotBeEmpty();
    }

    [Fact]
    public void NewMixedItems_Should_Seed_Each_Row_And_Preserve_Order()
    {
        // Arrange & Act - the multi-row factory for the four-member row. It takes whole rows rather
        // than a params list of scalars because four members cannot be seeded from one value each
        // the way NewOrderWithItems' product names can.
        var model = CollectionItemFixture.NewMixedItems(
            new MixedItem { Name = "first", Quantity = 1, IsGift = true, When = new DateTime(2020, 1, 1) },
            new MixedItem { Name = "second", Quantity = 2, IsGift = false, When = new DateTime(2030, 12, 31) });

        // Assert - every member survived, and the rows kept their order
        model.Rows.Count.ShouldBe(2);
        model.Rows.Select(r => r.Name).ShouldBe(new[] { "first", "second" });
        model.Rows.Select(r => r.Quantity).ShouldBe(new[] { 1, 2 });
        model.Rows.Select(r => r.IsGift).ShouldBe(new[] { true, false });
        model.Rows.Select(r => r.When)
            .ShouldBe(new[] { new DateTime(2020, 1, 1), new DateTime(2030, 12, 31) });

        // ...and in what renders, as the other seed tests do. Asserting the POCO alone would stay
        // green if the builder bound a field to the wrong property.
        var component = this.RenderItemForm(model, CollectionItemFixture.MultiFieldItemForm());
        component.FindComponents<MudTextField<string>>()
            .Where(f => f.Instance.Label == "Name")
            .Select(f => f.Instance.Value)
            .ShouldBe(new[] { "first", "second" });
        component.FindComponents<MudNumericField<int>>()
            .Select(f => f.Instance.Value)
            .ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public void RootFieldAndItemForm_Should_Render_The_Root_Field_Beside_The_Item_Field()
    {
        // Arrange & Act - the one shape none of the other builders produce: a top-level field
        // ALONGSIDE the collection. ShrinkLabelKeyCollisionTests is the named consumer (#213), and
        // for it the shape is load-bearing rather than incidental — its whole subject is a top-level
        // field and an item field whose names collide, which cannot be expressed by a form that has
        // only a collection in it.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewNamedOrder(),
            CollectionItemFixture.RootFieldAndItemForm());

        // Assert - two text fields, the ROOT one first. The labels differ so this can tell them
        // apart: with both defaulting to "Name" these two lines were the same assertion twice, and a
        // builder that emitted the item field first, or emitted two item fields and no root field,
        // would have passed.
        var fields = component.FindComponents<MudTextField<string>>();
        fields.Count.ShouldBe(2);
        fields[0].Instance.Label.ShouldBe("Name");
        fields[1].Instance.Label.ShouldBe("Item name");
    }

    [Fact]
    public void RootFieldAndItemForm_Should_Apply_Both_Callers_Configurations()
    {
        // Arrange & Act - two callbacks, two destinations. A builder that wired both to the same
        // field, or dropped one, would leave the collision suite configuring nothing.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewNamedOrder(),
            CollectionItemFixture.RootFieldAndItemForm(
                root => root.WithLabel("Customer name"),
                item => item.WithLabel("Product name")));

        // Assert
        var fields = component.FindComponents<MudTextField<string>>();
        fields[0].Instance.Label.ShouldBe("Customer name");
        fields[1].Instance.Label.ShouldBe("Product name");
    }

    [Fact]
    public void NewNamedOrder_Should_Default_Blank_And_Honour_Its_Seeds()
    {
        // Arrange & Act
        var blank = CollectionItemFixture.NewNamedOrder();
        var seeded = CollectionItemFixture.NewNamedOrder("Ada", "Widget");

        // Assert - both members are called Name on purpose; that collision is the point.
        blank.Name.ShouldBe(string.Empty);
        blank.Items.ShouldHaveSingleItem();
        blank.Items[0].Name.ShouldBe(string.Empty);
        seeded.Name.ShouldBe("Ada");
        seeded.Items[0].Name.ShouldBe("Widget");

        // ...and in what the fields actually render, as the other seed tests do. Asserting the POCO
        // alone would stay green if the builder bound either field to the wrong property.
        var fields = this.RenderItemForm(seeded, CollectionItemFixture.RootFieldAndItemForm())
            .FindComponents<MudTextField<string>>();
        fields[0].Instance.Value.ShouldBe("Ada");
        fields[1].Instance.Value.ShouldBe("Widget");
    }

    [Fact]
    public void NewOrderWithItems_Should_Create_One_Item_Per_Name()
    {
        // Arrange & Act - the multi-row factory. Several suites need more than one row (a per-row
        // handler must be told which row it came from; a per-field warning must fire once, not once
        // per row) and each was hand-rolling the literal.
        var model = CollectionItemFixture.NewOrderWithItems("first", "second", "third");

        // Assert - order preserved, in the model and in what renders
        model.Items.Count.ShouldBe(3);
        model.Items.Select(i => i.ProductName).ShouldBe(new[] { "first", "second", "third" });

        var component = this.RenderItemForm(model, CollectionItemFixture.TextItemForm());
        component.FindComponents<MudTextField<string>>()
            .Select(f => f.Instance.Value)
            .ShouldBe(new[] { "first", "second", "third" });
    }

    [Fact]
    public void RenderItemForm_Should_Apply_Extra_Component_Parameters()
    {
        // Arrange & Act - the escape hatch that keeps suites needing a third parameter (the
        // form-level cascade, or the EditContext callback) from re-implementing the Model and
        // Configuration wiring this helper owns.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewOrder(),
            CollectionItemFixture.TextItemForm(),
            parameters => parameters.Add(p => p.DefaultShrinkLabel, false));

        // Assert - the extra parameter reached the form and cascaded to the item field
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void NewOrder_Should_Leave_The_Product_Name_Blank_By_Default()
    {
        // Arrange & Act - CollectionRequiredTests depends on this: a seeded value would satisfy the
        // Required validator and quietly turn its validation tests into assertions about nothing.
        var model = CollectionItemFixture.NewOrder();

        // Assert
        model.Items.ShouldHaveSingleItem();
        model.Items[0].ProductName.ShouldBe(string.Empty);
    }

    [Fact]
    public void NewOrder_Should_Honour_A_Seeded_Product_Name()
    {
        // Arrange & Act - CollectionAdornmentTests renders against a populated field
        var model = CollectionItemFixture.NewOrder("Widget");

        // Assert - in the model, and in what the field actually renders
        model.Items[0].ProductName.ShouldBe("Widget");

        var component = this.RenderItemForm(model, CollectionItemFixture.TextItemForm());
        component.FindComponent<MudTextField<string>>().Instance.Value.ShouldBe("Widget");
    }

    [Fact]
    public void NewBasket_Should_Default_Blank_And_Honour_Its_Seeds()
    {
        // Arrange & Act
        var blank = CollectionItemFixture.NewBasket();
        var seeded = CollectionItemFixture.NewBasket(quantity: 7, isGift: true);

        // Assert
        blank.Lines.ShouldHaveSingleItem();
        blank.Lines[0].Quantity.ShouldBe(0);
        blank.Lines[0].IsGift.ShouldBeFalse();
        seeded.Lines[0].Quantity.ShouldBe(7);
        seeded.Lines[0].IsGift.ShouldBeTrue();
    }

    [Fact]
    public void NewAppointment_Should_Default_Blank_And_Honour_Its_Seed()
    {
        // Arrange & Act
        var blank = CollectionItemFixture.NewAppointment();
        var seeded = CollectionItemFixture.NewAppointment(new DateTime(2030, 12, 31));

        // Assert
        blank.Slots.ShouldHaveSingleItem();
        blank.Slots[0].When.ShouldBe(default);
        seeded.Slots[0].When.ShouldBe(new DateTime(2030, 12, 31));
    }
}
