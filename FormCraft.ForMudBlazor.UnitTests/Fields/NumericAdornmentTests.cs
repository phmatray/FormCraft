namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that standalone numeric fields honour a configured adornment (#191). These render through
/// the declarative component path (<c>MudBlazorNumericFieldComponent</c> and its nullable twin),
/// which emitted no adornment attributes at all — so an adornment configured on a numeric field was
/// accepted and silently discarded. #184 had already taught the *collection* path to forward them
/// for numeric item fields, which left the same configuration rendering an icon inside
/// <c>.WithItemForm(...)</c> and nothing outside it. These pin the component path; the cross-path
/// comparison lives in <c>RenderPipelineParityTests</c>.
/// </summary>
public class NumericAdornmentTests : MudBlazorTestBase
{
    [Fact]
    public void NumericField_Should_Render_The_Configured_Adornment()
    {
        // Arrange & Act
        var component = RenderBasketForm(BuildQuantityConfiguration(field => field
            .WithAttribute("Adornment", Adornment.End)
            .WithAttribute("AdornmentIcon", Icons.Material.Filled.Numbers)
            .WithAttribute("AdornmentColor", Color.Primary)));

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.End);
        numeric.AdornmentIcon.ShouldBe(Icons.Material.Filled.Numbers);
        numeric.AdornmentColor.ShouldBe(Color.Primary);
    }

    [Fact]
    public void NumericField_Without_An_Adornment_Should_Render_None()
    {
        // Arrange & Act - MudNumericField's own default is None, so forwarding an unset adornment
        // must leave an unconfigured field exactly as it rendered before #191.
        var component = RenderBasketForm(BuildQuantityConfiguration(_ => { }));

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.None);
        numeric.AdornmentIcon.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void NumericField_With_Only_An_Adornment_Position_Should_Default_Its_Color()
    {
        // Arrange & Act - a field that set only "Adornment" through raw WithAttribute has no colour
        // to read, so the component must supply one rather than assume all three are present.
        var component = RenderBasketForm(BuildQuantityConfiguration(field => field
            .WithAttribute("Adornment", Adornment.End)));

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.End);
        numeric.AdornmentColor.ShouldBe(Color.Default);
    }

    [Fact]
    public void NullableNumericField_Should_Render_The_Configured_Adornment()
    {
        // Arrange - the nullable variant is a separate component with its own markup, so it needs
        // its own coverage; fixing one of the pair and not the other is exactly how #191 arose.
        var component = RenderBasketForm(BuildDiscountConfiguration(field => field
            .WithAttribute("Adornment", Adornment.Start)
            .WithAttribute("AdornmentIcon", Icons.Material.Filled.Percent)
            .WithAttribute("AdornmentColor", Color.Secondary)));

        // Assert
        var numeric = component.FindComponent<MudNumericField<decimal?>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.Start);
        numeric.AdornmentIcon.ShouldBe(Icons.Material.Filled.Percent);
        numeric.AdornmentColor.ShouldBe(Color.Secondary);
    }

    [Fact]
    public void NullableNumericField_Without_An_Adornment_Should_Render_None()
    {
        // Arrange & Act
        var component = RenderBasketForm(BuildDiscountConfiguration(_ => { }));

        // Assert
        var numeric = component.FindComponent<MudNumericField<decimal?>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.None);
        numeric.AdornmentIcon.ShouldBeNullOrEmpty();
    }

    private IRenderedComponent<FormCraftComponent<BasketModel>> RenderBasketForm(
        IFormConfiguration<BasketModel> config)
    {
        return Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel())
            .Add(p => p.Configuration, config));
    }

    private static IFormConfiguration<BasketModel> BuildQuantityConfiguration(
        Action<FieldBuilder<BasketModel, int>> configureField)
    {
        return FormBuilder<BasketModel>
            .Create()
            .AddField(x => x.Quantity, field =>
            {
                field.WithLabel("Quantity");
                configureField(field);
            })
            .Build();
    }

    private static IFormConfiguration<BasketModel> BuildDiscountConfiguration(
        Action<FieldBuilder<BasketModel, decimal?>> configureField)
    {
        return FormBuilder<BasketModel>
            .Create()
            .AddField(x => x.Discount, field =>
            {
                field.WithLabel("Discount");
                configureField(field);
            })
            .Build();
    }

    private class BasketModel
    {
        public int Quantity { get; set; }

        public decimal? Discount { get; set; }
    }
}
