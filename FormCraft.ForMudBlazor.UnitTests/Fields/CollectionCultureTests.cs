using System.Globalization;
using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that a numeric item field honours a configured <c>Culture</c> (#218).
/// </summary>
/// <remarks>
/// The collection path hard-coded <c>CultureInfo.InvariantCulture</c> while the component path read a
/// configurable one, so the same model with the same configuration parsed decimals differently inside
/// and outside <c>.WithItemForm(...)</c> — a user typing <c>1,5</c> in a French locale got different
/// results depending on where the field sat.
/// <para>
/// Invariant remains the **default**, which is what both paths render for an unconfigured field;
/// making it configurable must not change what existing forms do.
/// </para>
/// </remarks>
public class CollectionCultureTests : MudBlazorTestBase
{
    private static readonly CultureInfo French = new("fr-FR");

    [Fact]
    public void NumericItemField_Should_Honour_A_Configured_Culture()
    {
        // Arrange & Act
        var component = RenderBasket(field => field.WithAttribute("Culture", French));

        // Assert
        component.FindComponent<MudNumericField<decimal>>().Instance.Culture.ShouldBe(French);
    }

    [Fact]
    public void NumericItemField_Without_A_Culture_Should_Stay_Invariant()
    {
        // Arrange & Act - the default must not move. Every existing collection form parses against
        // InvariantCulture today, and making the value configurable must not change that silently.
        var component = RenderBasket(_ => { });

        // Assert
        component.FindComponent<MudNumericField<decimal>>().Instance.Culture
            .ShouldBe(CultureInfo.InvariantCulture);
    }

    [Fact]
    public void Both_Render_Paths_Should_Agree_On_The_Configured_Culture()
    {
        // Arrange - the parity claim this issue is really about: same model, same configuration,
        // same parsing, inside or outside a collection.
        var standaloneConfig = FormBuilder<PriceModel>
            .Create()
            .AddField(x => x.Amount, f => f.WithLabel("Amount").WithAttribute("Culture", French))
            .Build();

        // Act
        var standalone = Render<FormCraftComponent<PriceModel>>(parameters => parameters
            .Add(p => p.Model, new PriceModel())
            .Add(p => p.Configuration, standaloneConfig));

        var item = RenderBasket(field => field.WithAttribute("Culture", French));

        // Assert
        item.FindComponent<MudNumericField<decimal>>().Instance.Culture
            .ShouldBe(standalone.FindComponent<MudNumericField<decimal>>().Instance.Culture);
    }

    /// <summary>
    /// The decimal item form comes from <see cref="CollectionItemFixture"/> (#258); the blank seed
    /// matches the unseeded <c>Price</c> this suite used to declare locally, and the assertions here
    /// are about <c>Culture</c> rather than about any value.
    /// </summary>
    private IRenderedComponent<FormCraftComponent<PricedBasketModel>> RenderBasket(
        Action<FieldBuilder<PricedLine, decimal>> configure) =>
        this.RenderItemForm(NewPricedBasket(), DecimalItemForm(configure));

    /// <summary>
    /// Stays local: it is a non-collection model, used only for the standalone half of the parity
    /// comparison below, so the fixture has no reason to carry it.
    /// </summary>
    private class PriceModel
    {
        public decimal Amount { get; set; }
    }
}
