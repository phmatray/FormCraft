using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Pins that the aggregated ShrinkLabel warning counts fields from different scopes separately (#213).
/// </summary>
/// <remarks>
/// <c>ShrinkLabelDiagnosticCollector</c> is form-wide and keys conflicts by field identity. The
/// collection path passed a **bare** <c>FieldName</c>, which is unique only within one item form —
/// so a top-level field and an item field sharing a name overwrote each other and the warning
/// undercounted. The collector's own docs already reject keying by *label* for the same reason
/// ("Name" in Billing and in Shipping); this is that argument applied one level up.
/// </remarks>
public class ShrinkLabelKeyCollisionTests : MudBlazorTestBase
{
    private readonly CapturingLoggerProvider _logs = new();

    public ShrinkLabelKeyCollisionTests()
    {
        Services.AddLogging(builder => builder.AddProvider(_logs));
    }

    [Fact]
    public void A_TopLevel_Field_And_An_Item_Field_Sharing_A_Name_Should_Both_Be_Counted()
    {
        // Arrange - both conflict (a placeholder pins the label), both ask for a floating label, and
        // both are called "Name". They are different fields and must be reported as two.
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Customer name")
                .WithPlaceholder("e.g. Ada")
                .WithShrinkLabel(false))
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.Name, f => f
                        .WithLabel("Product name")
                        .WithPlaceholder("e.g. Widget")
                        .WithShrinkLabel(false))))
            .Build();

        // Act
        Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, new OrderModel { Items = { new OrderItem() } })
            .Add(p => p.Configuration, config));

        // Assert - one aggregated warning, naming both fields.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("2 field(s)");
        warnings[0].ShouldContain("Customer name");
        warnings[0].ShouldContain("Product name");
    }

    [Fact]
    public void Two_Collections_With_Same_Named_Item_Fields_Should_Both_Be_Counted()
    {
        // Arrange - the collision this issue does not name. Each CollectionFieldComponent has its
        // own once-per-field latch, so nothing local catches this; the clash exists only in the
        // form-wide collector, which is why the key must carry the owning collection's name.
        var config = FormBuilder<TwoCollectionModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.Name, f => f
                        .WithLabel("Item name")
                        .WithPlaceholder("e.g. Widget")
                        .WithShrinkLabel(false))))
            .AddCollectionField(x => x.Extras, collection => collection
                .WithLabel("Extras")
                .WithItemForm(item => item
                    .AddField(x => x.Name, f => f
                        .WithLabel("Extra name")
                        .WithPlaceholder("e.g. Gift wrap")
                        .WithShrinkLabel(false))))
            .Build();

        // Act
        Render<FormCraftComponent<TwoCollectionModel>>(parameters => parameters
            .Add(p => p.Model, new TwoCollectionModel
            {
                Items = { new OrderItem() },
                Extras = { new OrderItem() }
            })
            .Add(p => p.Configuration, config));

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("2 field(s)");
        warnings[0].ShouldContain("Item name");
        warnings[0].ShouldContain("Extra name");
    }

    [Fact]
    public void Several_Rows_Should_Still_Produce_One_Warning_For_The_Item_Field()
    {
        // Arrange - the property the qualified key must not break. The warning is about a field's
        // configuration, so a collection of many rows must report it once, not once per row.
        var config = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.Name, f => f
                        .WithLabel("Product name")
                        .WithPlaceholder("e.g. Widget")
                        .WithShrinkLabel(false))))
            .Build();

        // Act
        Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, new OrderModel
            {
                Items = { new OrderItem(), new OrderItem(), new OrderItem() }
            })
            .Add(p => p.Configuration, config));

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("1 field(s)");
    }

    private class OrderModel
    {
        public string Name { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
    }

    private class TwoCollectionModel
    {
        public List<OrderItem> Items { get; set; } = new();
        public List<OrderItem> Extras { get; set; } = new();
    }

    private class OrderItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
