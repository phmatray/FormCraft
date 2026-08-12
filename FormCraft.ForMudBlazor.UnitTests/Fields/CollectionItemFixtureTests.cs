namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Self-tests for <see cref="CollectionItemFixture"/> (#205).
/// <para>
/// The fixture exists so an attribute suite declares only the behaviour it tests, and so every such
/// suite covers all four collection-item render paths by default instead of by remembering to. That
/// only holds if the fixture itself is guarded: these tests pin the two properties the suites rely on
/// and cannot check for themselves.
/// </para>
/// <para>
/// <b>All four paths render.</b> Three of them — text, numeric and date — are fed by
/// <c>AddCommonFieldAttributes</c>; the boolean path bypasses it entirely, which is why several suites
/// pin an attribute as *inert* there. A fixture that silently stopped producing one of the four would
/// turn those tests green for the wrong reason, so each path is asserted to reach its MudBlazor
/// component.
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
        // Arrange & Act - a fourth *type*, not a fourth path: MudNumericField<decimal> is a distinct
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
