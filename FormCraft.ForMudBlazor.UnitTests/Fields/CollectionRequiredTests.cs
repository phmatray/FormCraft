namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields do NOT carry the HTML5 <c>Required</c> attribute (#190).
/// The project's validation convention is server-side only — forms render <c>novalidate</c>, messages
/// come from the validator, and no component-path renderer emits <c>Required</c>. The collection
/// path drove it from <c>field.IsRequired</c>, so the same <c>.Required("…")</c> call rendered
/// differently inside <c>.WithItemForm(...)</c>, and MudBlazor's own required validation ran
/// alongside the configured one — two differently-worded messages for one problem.
/// <para>
/// The attribute is now resolved from an explicit <c>"Required"</c> attribute instead, so a field
/// that opts back in with <c>.WithAttribute("Required", true)</c> still gets it.
/// </para>
/// </summary>
public class CollectionRequiredTests : MudBlazorTestBase
{
    [Fact]
    public void Required_ItemField_Should_Not_Render_The_Html5_Required_Attribute()
    {
        // Arrange & Act - the same .Required(...) call a standalone field would use
        var component = RenderOrderForm(BuildConfiguration(field => field
            .Required("Product name is required")));

        // Assert - validation still runs (see the EditContext tests); the attribute does not
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeFalse();
    }

    [Fact]
    public void ItemField_Should_Honour_An_Explicit_Required_Attribute()
    {
        // Arrange & Act - the escape hatch: dropping the IsRequired forward must not also drop the
        // ability to ask for MudBlazor's asterisk deliberately.
        var component = RenderOrderForm(BuildConfiguration(field => field
            .WithAttribute("Required", true)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeTrue();
    }

    [Fact]
    public void ItemField_Without_Any_Required_Configuration_Should_Not_Render_It()
    {
        // Arrange & Act - unchanged from before #190
        var component = RenderOrderForm(BuildConfiguration(_ => { }));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeFalse();
    }

    [Fact]
    public void Required_NumericItemField_Should_Not_Render_The_Html5_Required_Attribute()
    {
        // Arrange - AddCommonFieldAttributes is shared by the text and numeric paths, so the numeric
        // one must lose the forward too rather than being fixed only where it was measured.
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field => field
                        .WithLabel("Quantity")
                        .Required("Quantity is required"))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Required.ShouldBeFalse();
    }

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderOrderForm(
        IFormConfiguration<OrderModel> config)
    {
        var model = new OrderModel { Items = { new OrderItem() } };

        return Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));
    }

    private static IFormConfiguration<OrderModel> BuildConfiguration(
        Action<FieldBuilder<OrderItem, string>> configureItemField)
    {
        return FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field =>
                    {
                        field.WithLabel("Product");
                        configureItemField(field);
                    })))
            .Build();
    }

    private class OrderModel
    {
        public List<OrderItem> Items { get; set; } = new();
    }

    private class OrderItem
    {
        public string ProductName { get; set; } = string.Empty;
    }

    private class BasketModel
    {
        public List<BasketLine> Lines { get; set; } = new();
    }

    private class BasketLine
    {
        public int Quantity { get; set; }
    }
}
