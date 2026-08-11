namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields honor <c>.WithAdornment(...)</c> (#184). These render through
/// CollectionFieldComponent's imperative RenderTreeBuilder path, which resolves presentation
/// attributes in AddCommonFieldAttributes rather than through MudBlazorFieldComponentBase — so it
/// needs its own coverage. Before #184 the three adornment attributes were silently dropped here
/// while the component path forwarded them, so the same builder call rendered differently
/// depending on whether the field sat inside <c>.WithItemForm(...)</c>.
/// </summary>
public class CollectionAdornmentTests : MudBlazorTestBase
{
    [Fact]
    public void ItemField_Should_Render_A_Start_Adornment()
    {
        // Arrange & Act
        var component = RenderOrderForm(BuildConfiguration(field => field
            .WithAdornment(Icons.Material.Filled.Search, Adornment.Start)));

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.Start);
        textField.AdornmentIcon.ShouldBe(Icons.Material.Filled.Search);
    }

    [Fact]
    public void ItemField_Should_Render_The_Configured_Adornment_Color()
    {
        // Arrange & Act - WithAdornment always writes all three attributes, colour included
        var component = RenderOrderForm(BuildConfiguration(field => field
            .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, Color.Secondary)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.AdornmentColor
            .ShouldBe(Color.Secondary);
    }

    [Fact]
    public void ItemField_Without_An_Adornment_Should_Render_None()
    {
        // Arrange & Act - unchanged from before #184: no adornment configured, none rendered
        var component = RenderOrderForm(BuildConfiguration(_ => { }));

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.None);
        textField.AdornmentIcon.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void ItemField_With_Only_An_Adornment_Position_Should_Default_Its_Color()
    {
        // Arrange & Act - a field that set "Adornment" through raw WithAttribute has no colour to
        // read, so the resolver must supply one rather than assume all three are present.
        var component = RenderOrderForm(BuildConfiguration(field => field
            .WithAttribute("Adornment", Adornment.End)));

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.End);
        textField.AdornmentColor.ShouldBe(Color.Default);
    }

    [Fact]
    public void NumericItemField_Should_Render_An_End_Adornment()
    {
        // Arrange - WithAdornment is declared on FieldBuilder<TModel, string> only, so a numeric
        // field configures the same three attributes through raw WithAttribute. The renderer must
        // honour them either way: it reads AdditionalAttributes, not the builder method.
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field => field
                        .WithLabel("Quantity")
                        .WithAttribute("Adornment", Adornment.End)
                        .WithAttribute("AdornmentIcon", Icons.Material.Filled.Numbers)
                        .WithAttribute("AdornmentColor", Color.Primary))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.End);
        numeric.AdornmentIcon.ShouldBe(Icons.Material.Filled.Numbers);
        numeric.AdornmentColor.ShouldBe(Color.Primary);
    }

    [Fact]
    public void NumericItemField_Without_An_Adornment_Should_Render_None()
    {
        // Arrange & Act - unchanged from before #184
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field => field.WithLabel("Quantity"))))
            .Build();

        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Adornment.ShouldBe(Adornment.None);
    }

    [Fact]
    public void BooleanItemField_With_An_Adornment_Should_Render_Without_One()
    {
        // Arrange - MudCheckBox has no adornment concept at all, so RenderBooleanField sets its own
        // attributes and takes no part in the forward. Configuring one must be inert, not a throw.
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.IsGift, field => field
                        .WithLabel("Gift")
                        .WithAttribute("Adornment", Adornment.Start)
                        .WithAttribute("AdornmentIcon", Icons.Material.Filled.Search))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Gift");
        component.Markup.ShouldNotContain(Icons.Material.Filled.Search);
    }

    [Fact]
    public void DateItemField_Should_Keep_Its_Calendar_Icon()
    {
        // Arrange - MudDatePicker's own default is Adornment.End with a calendar icon, unlike the
        // text and numeric fields whose default is None. Forwarding an unset adornment onto it
        // would silently erase that icon, so the date path deliberately does not take the forward.
        var config = FormBuilder<AppointmentModel>
            .Create()
            .AddCollectionField(x => x.Slots, collection => collection
                .WithLabel("Slots")
                .WithItemForm(item => item
                    .AddField(x => x.When, field => field.WithLabel("When"))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<AppointmentModel>>(parameters => parameters
            .Add(p => p.Model, new AppointmentModel { Slots = { new AppointmentSlot() } })
            .Add(p => p.Configuration, config));

        // Assert
        var picker = component.FindComponent<MudDatePicker>().Instance;
        picker.Adornment.ShouldBe(Adornment.End);
        picker.AdornmentIcon.ShouldNotBeNullOrEmpty();
    }

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderOrderForm(
        IFormConfiguration<OrderModel> config)
    {
        var model = new OrderModel { Items = { new OrderItem { ProductName = "Widget" } } };

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

        public bool IsGift { get; set; }
    }

    private class AppointmentModel
    {
        public List<AppointmentSlot> Slots { get; set; } = new();
    }

    private class AppointmentSlot
    {
        public DateTime When { get; set; }
    }
}
