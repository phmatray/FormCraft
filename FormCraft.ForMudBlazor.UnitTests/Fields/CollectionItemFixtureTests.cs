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

        // Assert - one of each component type. MudDatePicker embeds a MudTextField<string> of its
        // own, so the text field is selected by label rather than by FindComponent, which would
        // return whichever came first in the tree.
        component.FindComponents<MudTextField<string>>()
            .Select(f => f.Instance.Label)
            .ShouldContain("Name");
        component.FindComponent<MudNumericField<int>>().Instance.Label.ShouldBe("Quantity");
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Gift");
        component.FindComponent<MudDatePicker>().Instance.Label.ShouldBe("When");
    }

    [Fact]
    public void MultiFieldItemForm_Should_Declare_Its_Four_Fields_In_A_Stable_Order()
    {
        // Arrange & Act - the order is not cosmetic: CollectionRenderCharacterisationTests'
        // Every_Item_Field_Kind_Should_Render_Its_Own_Nested_Validation_Slot asserts the exact
        // sequence Rows[0].Name, Rows[0].Quantity, Rows[0].IsGift, Rows[0].When. Swapping two
        // AddField calls in the builder would turn that consumer red while every assertion in this
        // file — the one that exists to pin what the suites rely on and cannot check themselves —
        // stayed green, sending the next maintainer to debug the wrong file.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewMixedItems(new MixedItem()),
            CollectionItemFixture.MultiFieldItemForm());

        // Assert - the validation slots carry the item field names in declaration order
        component.FindComponents<FieldValidationMessage>()
            .Select(m => m.Instance.FieldName)
            .ShouldBe(new[] { "Rows[0].Name", "Rows[0].Quantity", "Rows[0].IsGift", "Rows[0].When" });
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
        var reorderable = this.RenderItemForm(
            CollectionItemFixture.NewMixedItems(new MixedItem(), new MixedItem()),
            CollectionItemFixture.MultiFieldItemForm(
                configureCollection: collection => collection.AllowReorder()));

        // ...and the same form WITHOUT the callback, which is the half that makes this a test of the
        // callback rather than of MudBlazor. Asserting only that the buttons appear would stay green
        // if reordering ever became the default, or if a regression rendered the controls
        // unconditionally — proving nothing about the parameter the test is named for.
        var plain = this.RenderItemForm(
            CollectionItemFixture.NewMixedItems(new MixedItem(), new MixedItem()),
            CollectionItemFixture.MultiFieldItemForm());

        // Assert - reorder controls only render when the collection allows reordering
        reorderable.FindAll("button[aria-label='Move up']").ShouldNotBeEmpty();
        plain.FindAll("button[aria-label='Move up']").ShouldBeEmpty();
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

        // All four, not just the two easy ones: this is the only test seeding every member with
        // per-row distinguishable values, so if it checks half of them it is the reason a
        // mis-bound bool or DateTime would reach a consumer suite unnoticed.
        component.FindComponents<MudCheckBox<bool>>()
            .Select(f => f.Instance.Value)
            .ShouldBe(new[] { true, false });
        component.FindComponents<MudDatePicker>()
            .Select(f => f.Instance.Date)
            .ShouldBe(new DateTime?[] { new DateTime(2020, 1, 1), new DateTime(2030, 12, 31) });
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
    public void TwoCollectionItemForm_Should_Render_Both_Collections_Item_Fields()
    {
        // Arrange & Act - the shape whose whole purpose is that the two item fields share a member
        // name (both bind OrderItem.ProductName) while belonging to different collections.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewTwoCollections(),
            CollectionItemFixture.TwoCollectionItemForm());

        // Assert - two text fields, one per collection, in declaration order
        var fields = component.FindComponents<MudTextField<string>>();
        fields.Count.ShouldBe(2);
        fields[0].Instance.Label.ShouldBe("Contact name");
        fields[1].Instance.Label.ShouldBe("Supplier name");
    }

    [Fact]
    public void TwoCollectionItemForm_Should_Route_Each_Callback_To_Its_Own_Collection()
    {
        // Arrange & Act - two callbacks, two destinations. A builder wiring both to one collection
        // would leave the diagnostic suites configuring only half the form while looking configured.
        var component = this.RenderItemForm(
            CollectionItemFixture.NewTwoCollections(),
            CollectionItemFixture.TwoCollectionItemForm(
                configureContact: field => field.WithLabel("Renamed contact"),
                configureSupplier: field => field.WithLabel("Renamed supplier")));

        // Assert - count first: if the builder regressed to attaching both item forms to one
        // collection, only one field renders and fields[1] throws IndexOutOfRange, turning the
        // defect this test names into a stack trace instead of a diagnosis.
        var fields = component.FindComponents<MudTextField<string>>();
        fields.Count.ShouldBe(2);
        fields[0].Instance.Label.ShouldBe("Renamed contact");
        fields[1].Instance.Label.ShouldBe("Renamed supplier");
    }

    [Fact]
    public void NewTwoCollections_Should_Seed_One_Row_In_Each_Collection()
    {
        // Arrange & Act
        var blank = CollectionItemFixture.NewTwoCollections();
        var seeded = CollectionItemFixture.NewTwoCollections("Ada", "Acme");

        // Assert - in the model...
        blank.Contacts.ShouldHaveSingleItem();
        blank.Suppliers.ShouldHaveSingleItem();
        blank.Contacts[0].ProductName.ShouldBe(string.Empty);
        // Both halves: with only the Contacts one asserted, changing the `supplier` default to a
        // non-empty value would leave ShrinkLabelKeyCollisionTests silently seeded when it calls
        // NewTwoCollections() expecting two blank rows.
        blank.Suppliers[0].ProductName.ShouldBe(string.Empty);
        seeded.Contacts[0].ProductName.ShouldBe("Ada");
        seeded.Suppliers[0].ProductName.ShouldBe("Acme");

        // ...and in what renders, so a builder binding both fields to the same collection is caught
        var fields = this
            .RenderItemForm(seeded, CollectionItemFixture.TwoCollectionItemForm())
            .FindComponents<MudTextField<string>>();
        fields[0].Instance.Value.ShouldBe("Ada");
        fields[1].Instance.Value.ShouldBe("Acme");
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

    [Fact]
    public void Each_Item_Form_Should_Apply_The_Callers_Collection_Configuration()
    {
        // Arrange & Act - AllowReorder belongs to the COLLECTION, not to a field, so no field
        // callback can reach it. Until #300 only MultiFieldItemForm took a collection callback, and
        // any suite needing one on a single-field item form had to hand-roll the whole
        // configuration — keeping its own model copy with it, which is the duplication this fixture
        // exists to remove.
        //
        // Every model below carries TWO rows deliberately. CollectionFieldComponent renders Move up
        // with Disabled="@(index == 0)", so a one-row collection renders the control permanently
        // inert: a mere "the button exists" assertion would hold for a form on which reordering
        // cannot actually happen.
        var text = this.RenderItemForm(
            CollectionItemFixture.NewOrderWithItems("A", "B"),
            CollectionItemFixture.TextItemForm(
                configureCollection: collection => collection.AllowReorder()));
        var numeric = this.RenderItemForm(
            new BasketModel { Lines = { new BasketLine(), new BasketLine() } },
            CollectionItemFixture.NumericItemForm(
                configureCollection: collection => collection.AllowReorder()));
        var date = this.RenderItemForm(
            new AppointmentModel { Slots = { new AppointmentSlot(), new AppointmentSlot() } },
            CollectionItemFixture.DateItemForm(
                configureCollection: collection => collection.AllowReorder()));
        var boolean = this.RenderItemForm(
            new BasketModel { Lines = { new BasketLine(), new BasketLine() } },
            CollectionItemFixture.BooleanItemForm(
                configureCollection: collection => collection.AllowReorder()));
        var dec = this.RenderItemForm(
            new PricedBasketModel { Lines = { new PricedLine(), new PricedLine() } },
            CollectionItemFixture.DecimalItemForm(
                configureCollection: collection => collection.AllowReorder()));

        // ...and each form WITHOUT the callback, the half that makes this a test of the callback
        // rather than of MudBlazor. Asserting only that the controls appear would stay green if
        // reordering ever became the default, or if a regression rendered them unconditionally —
        // proving nothing about the parameter the test is named for.
        //
        // Numeric and Boolean are both here even though they configure the SAME collection
        // (BasketModel.Lines): they are separate builders wiring separate callbacks, so one passing
        // says nothing about the other.
        var plainText = this.RenderItemForm(
            CollectionItemFixture.NewOrderWithItems("A", "B"),
            CollectionItemFixture.TextItemForm());
        var plainNumeric = this.RenderItemForm(
            new BasketModel { Lines = { new BasketLine(), new BasketLine() } },
            CollectionItemFixture.NumericItemForm());
        var plainDate = this.RenderItemForm(
            new AppointmentModel { Slots = { new AppointmentSlot(), new AppointmentSlot() } },
            CollectionItemFixture.DateItemForm());
        var plainBoolean = this.RenderItemForm(
            new BasketModel { Lines = { new BasketLine(), new BasketLine() } },
            CollectionItemFixture.BooleanItemForm());
        var plainDecimal = this.RenderItemForm(
            new PricedBasketModel { Lines = { new PricedLine(), new PricedLine() } },
            CollectionItemFixture.DecimalItemForm());

        // Assert - usable reorder controls, and only where the caller asked for them
        ShouldOfferReordering(text);
        ShouldOfferReordering(numeric);
        ShouldOfferReordering(date);
        ShouldOfferReordering(boolean);
        ShouldOfferReordering(dec);

        ShouldNotOfferReordering(plainText);
        ShouldNotOfferReordering(plainNumeric);
        ShouldNotOfferReordering(plainDate);
        ShouldNotOfferReordering(plainBoolean);
        ShouldNotOfferReordering(plainDecimal);
    }

    [Fact]
    public void Each_Item_Forms_Collection_Callback_Should_Run_After_The_Fixtures_Own_Configuration()
    {
        // Arrange & Act - the invoke ORDER is half the contract, and AllowReorder cannot pin it: it
        // is an independent setter that reads the same whether the caller's callback runs before or
        // after the fixture's own WithLabel/WithItemForm. Overriding the label can only succeed if
        // the caller runs LAST — the same property Each_Item_Form_Should_Apply_The_Callers_
        // Configuration pins for the field callback ("the callback runs after the default label, so
        // it can override it"). Without this test all six builders could invoke the callback first
        // and every other assertion here would stay green.
        var forms = new IRenderedComponent<IComponent>[]
        {
            this.RenderItemForm(
                CollectionItemFixture.NewOrder(),
                CollectionItemFixture.TextItemForm(
                    configureCollection: collection => collection.WithLabel("Renamed"))),
            this.RenderItemForm(
                CollectionItemFixture.NewBasket(),
                CollectionItemFixture.NumericItemForm(
                    configureCollection: collection => collection.WithLabel("Renamed"))),
            this.RenderItemForm(
                CollectionItemFixture.NewAppointment(),
                CollectionItemFixture.DateItemForm(
                    configureCollection: collection => collection.WithLabel("Renamed"))),
            this.RenderItemForm(
                CollectionItemFixture.NewBasket(),
                CollectionItemFixture.BooleanItemForm(
                    configureCollection: collection => collection.WithLabel("Renamed"))),
            this.RenderItemForm(
                CollectionItemFixture.NewPricedBasket(),
                CollectionItemFixture.DecimalItemForm(
                    configureCollection: collection => collection.WithLabel("Renamed"))),
            this.RenderItemForm(
                CollectionItemFixture.NewNamedOrder(),
                CollectionItemFixture.RootFieldAndItemForm(
                    configureCollection: collection => collection.WithLabel("Renamed"))),

            // MultiFieldItemForm has carried the parameter since #282; asserting it here is what
            // makes the ordering uniform across all SEVEN builders rather than the six #300 touched.
            this.RenderItemForm(
                CollectionItemFixture.NewMixedItems(new MixedItem()),
                CollectionItemFixture.MultiFieldItemForm(
                    configureCollection: collection => collection.WithLabel("Renamed"))),
        };

        // Assert - the caller's label won, so its callback ran after the fixture set its own.
        // The collection's label renders as the header MudText (Typo.h6).
        foreach (var form in forms)
        {
            form.Find("h6").TextContent.Trim().ShouldBe("Renamed");
        }
    }

    [Fact]
    public void RootFieldAndItemForm_Should_Apply_The_Callers_Collection_Configuration()
    {
        // Arrange & Act - three callbacks now, two of which target fields and one the collection.
        // A mis-wire is easy in that shape, so this asserts the new one reaches the collection AND
        // that the two existing ones still reach their own fields — a builder that routed the root
        // callback into the collection would otherwise pass a test that only looked at reordering.
        var reorderable = this.RenderItemForm(
            new NamedOrderModel { Items = { new NamedOrderItem(), new NamedOrderItem() } },
            CollectionItemFixture.RootFieldAndItemForm(
                root => root.WithLabel("Customer name"),
                item => item.WithLabel("Product name"),
                collection => collection.AllowReorder()));

        // ...and the same form WITHOUT the collection callback: the negative half, which is what
        // makes this a test of the parameter rather than of MudBlazor's defaults.
        var plain = this.RenderItemForm(
            new NamedOrderModel { Items = { new NamedOrderItem(), new NamedOrderItem() } },
            CollectionItemFixture.RootFieldAndItemForm());

        // Assert - the collection callback reaches the collection...
        ShouldOfferReordering(reorderable);
        ShouldNotOfferReordering(plain);

        // ...and adding a third callback did not re-route the two that were already there
        var fields = reorderable.FindComponents<MudTextField<string>>();
        fields[0].Instance.Label.ShouldBe("Customer name");
        fields[1].Instance.Label.ShouldBe("Product name");
    }

    /// <summary>
    /// Asserts that a rendered collection offers <em>usable</em> reorder controls — the observable
    /// effect of <c>AllowReorder()</c>, and therefore the proof that a builder's collection callback
    /// reached the collection.
    /// </summary>
    /// <remarks>
    /// The enabled-ness check is the point. <c>CollectionFieldComponent</c> disables Move up on row
    /// 0 and Move down on the last row, so on a one-row collection both controls render and both are
    /// permanently inert — "a Move up button exists" is true of a form that cannot be reordered.
    /// Callers therefore pass two-row models, and this asserts one control per row with the second
    /// one live.
    /// </remarks>
    private static void ShouldOfferReordering<TModel>(
        IRenderedComponent<FormCraftComponent<TModel>> component)
        where TModel : new()
    {
        var moveUp = component.FindAll("button[aria-label='Move up']");
        moveUp.Count.ShouldBe(2);
        moveUp[0].HasAttribute("disabled").ShouldBeTrue();
        moveUp[1].HasAttribute("disabled").ShouldBeFalse();
    }

    /// <summary>
    /// The negative control for <see cref="ShouldOfferReordering{TModel}"/>: no callback, so the
    /// collection never allows reordering and the controls are absent rather than merely disabled.
    /// </summary>
    private static void ShouldNotOfferReordering<TModel>(
        IRenderedComponent<FormCraftComponent<TModel>> component)
        where TModel : new()
        => component.FindAll("button[aria-label='Move up']").ShouldBeEmpty();
}
