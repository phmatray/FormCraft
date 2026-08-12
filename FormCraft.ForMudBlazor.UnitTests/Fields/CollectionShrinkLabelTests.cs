namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields honor the configurable ShrinkLabel (#177).
/// </summary>
/// <remarks>
/// Written when item fields rendered through CollectionFieldComponent's imperative
/// RenderTreeBuilder path, which resolved ShrinkLabel in its own <c>GetItemFieldShrinkLabel</c>
/// rather than through <c>MudBlazorFieldComponentBase.EffectiveShrinkLabel</c> — a duplicate
/// resolver, hence duplicate coverage.
/// <para>
/// #203 removed that duplicate: item fields resolve ShrinkLabel through the one component property,
/// including the form-level cascade fallback these tests exercise. They pass unmodified and are kept
/// as the guard that the item placement keeps inheriting it.
/// </para>
/// </remarks>
public class CollectionShrinkLabelTests : MudBlazorTestBase
{
    [Fact]
    public void ItemField_Should_Default_To_ShrinkLabel_True()
    {
        // Arrange & Act
        var component = RenderOrderForm(BuildConfiguration(itemShrinkLabel: null));

        // Assert - unchanged from before #177
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    [Fact]
    public void ItemField_Should_Honor_FieldLevel_WithShrinkLabel()
    {
        // Arrange & Act
        var component = RenderOrderForm(BuildConfiguration(itemShrinkLabel: false));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void ItemField_Should_Honor_FormLevel_DefaultShrinkLabel()
    {
        // Arrange & Act - the cascade has to reach into the collection component too
        var component = RenderOrderForm(
            BuildConfiguration(itemShrinkLabel: null), defaultShrinkLabel: false);

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void ItemField_FieldLevel_Should_Override_FormLevel()
    {
        // Arrange & Act
        var component = RenderOrderForm(
            BuildConfiguration(itemShrinkLabel: true), defaultShrinkLabel: false);

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderOrderForm(
        IFormConfiguration<OrderModel> config, bool? defaultShrinkLabel = null)
    {
        var model = new OrderModel { Items = { new OrderItem { ProductName = "Widget" } } };

        return Render<FormCraftComponent<OrderModel>>(parameters =>
        {
            parameters.Add(p => p.Model, model);
            parameters.Add(p => p.Configuration, config);
            if (defaultShrinkLabel is { } shrink)
            {
                parameters.Add(p => p.DefaultShrinkLabel, shrink);
            }
        });
    }

    private static IFormConfiguration<OrderModel> BuildConfiguration(bool? itemShrinkLabel)
    {
        return FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field =>
                    {
                        field.WithLabel("Product");
                        if (itemShrinkLabel is { } shrink)
                        {
                            field.WithShrinkLabel(shrink);
                        }
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
}
