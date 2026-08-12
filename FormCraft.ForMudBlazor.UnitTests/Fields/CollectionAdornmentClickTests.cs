using Microsoft.AspNetCore.Components.Web;
using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that a collection item field honours <c>.WithAdornment(..., onClick: h)</c> (#192).
/// #184 made the adornment's position, icon and colour work on this path; the handler was the one
/// parameter still dropped, and it was dropped on both paths — accepted by the builder, documented,
/// and never written anywhere. These render through CollectionFieldComponent's imperative
/// RenderTreeBuilder path, so the callback has to be built explicitly rather than bound by Razor.
/// </summary>
public class CollectionAdornmentClickTests : MudBlazorTestBase
{
    /// <summary>
    /// MudBlazor draws a clickable adornment as a real button (and a handler-less one as a plain
    /// icon), so these tests click the DOM rather than invoking the callback directly.
    /// </summary>
    private const string AdornmentButton = "button.mud-input-adornment-icon-button";

    [Fact]
    public void ItemField_Adornment_Click_Should_Invoke_The_Configured_Handler()
    {
        // Arrange
        var received = new List<string?>();
        var component = RenderOrderForm(
            BuildConfiguration(field => field
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: received.Add)),
            "Widget");

        // Act
        component.Find(AdornmentButton).Click();

        // Assert - fired once, and with the item's own value
        received.ShouldHaveSingleItem().ShouldBe("Widget");
    }

    [Fact]
    public void ItemField_Adornment_Click_Should_Pass_The_Value_Typed_By_The_User()
    {
        // Arrange - the handler must see the current value, not the one the row rendered with
        var received = new List<string?>();
        var component = RenderOrderForm(
            BuildConfiguration(field => field
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: received.Add)),
            "before");

        // Act
        component.Find("input").Input("after");
        component.Find(AdornmentButton).Click();

        // Assert
        received.ShouldHaveSingleItem().ShouldBe("after");
    }

    [Fact]
    public void ItemField_Adornment_Click_Should_Pass_Its_Own_Rows_Value()
    {
        // Arrange - one handler serves every row, so it must be told which row was clicked; a
        // callback that captured the wrong index would search the first row from any icon
        var received = new List<string?>();
        var config = BuildConfiguration(field => field
            .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: received.Add));

        // Two rows: the fixture's factories seed a single item, and a second row is this test's own
        // requirement rather than a shape other suites share, so it is built here from the fixture's
        // models rather than added to the fixture.
        var model = new OrderModel
        {
            Items =
            {
                new OrderItem { ProductName = "first" },
                new OrderItem { ProductName = "second" },
            },
        };

        var component = this.RenderItemForm(model, config);

        // Act - click the second row's adornment
        component.FindAll(AdornmentButton)[1].Click();

        // Assert
        received.ShouldHaveSingleItem().ShouldBe("second");
    }

    [Fact]
    public void ItemField_Adornment_Without_A_Handler_Should_Stay_Inert()
    {
        // Arrange & Act - an adornment configured with no handler keeps rendering as a plain icon,
        // exactly as it did after #184; nothing becomes clickable just because the path now can be
        var component = RenderOrderForm(
            BuildConfiguration(field => field
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start)),
            "Widget");

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.Start);
        textField.AdornmentIcon.ShouldBe(Icons.Material.Filled.Search);
        component.FindAll(AdornmentButton).ShouldBeEmpty();
    }

    [Fact]
    public void ItemField_Without_An_Adornment_Should_Be_Unaffected()
    {
        // Arrange & Act - the field that configures nothing must render exactly as before
        var component = RenderOrderForm(BuildConfiguration(_ => { }), "Widget");

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.None);
        textField.OnAdornmentClick.HasDelegate.ShouldBeFalse();
    }

    [Fact]
    public void NumericItemField_With_An_Adornment_Should_Stay_Inert()
    {
        // Arrange - WithAdornment is declared on string fields only, so a numeric item field has no
        // handler to forward. Rendering its adornment must not throw or invent one (#191 tracks
        // numeric adornment support in its own right).
        var config = NumericItemForm(field => field
            .WithAttribute("Adornment", Adornment.End)
            .WithAttribute("AdornmentIcon", Icons.Material.Filled.Numbers));

        // Act
        var component = this.RenderItemForm(NewBasket(), config);

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.End);
        numeric.OnAdornmentClick.HasDelegate.ShouldBeFalse();
    }

    [Fact]
    public void ItemField_Adornment_Handler_Of_The_Wrong_Shape_Should_Be_Ignored()
    {
        // Arrange - AdditionalAttributes is untyped, so a raw WithAttribute can put anything under
        // the key. The renderer must fall back to "no handler" rather than cast and throw.
        var config = BuildConfiguration(field => field
            .WithAttribute("Adornment", Adornment.Start)
            .WithAttribute("AdornmentIcon", Icons.Material.Filled.Search)
            .WithAttribute("OnAdornmentClick", "not a delegate"));

        // Act
        var component = RenderOrderForm(config, "Widget");

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.Start);
        textField.OnAdornmentClick.HasDelegate.ShouldBeFalse();
    }

    /// <summary>
    /// Renders the fixture's text item form (#205) seeded with <paramref name="productName"/>. The
    /// seed is a per-test choice here — one test types over it, another asserts the handler receives
    /// it — so it stays an explicit parameter rather than a default.
    /// </summary>
    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderOrderForm(
        IFormConfiguration<OrderModel> config,
        string productName) =>
        this.RenderItemForm(NewOrder(productName), config);

    private static IFormConfiguration<OrderModel> BuildConfiguration(
        Action<FieldBuilder<OrderItem, string>> configureItemField) =>
        TextItemForm(configureItemField);
}
