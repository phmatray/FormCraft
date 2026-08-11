namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields do NOT carry the HTML5 <c>Required</c> attribute (#190).
/// The project's validation convention is server-side only — forms render <c>novalidate</c>, messages
/// come from the validator, and no component-path renderer emits <c>Required</c>. The collection
/// path drove it from <c>field.IsRequired</c>, so the same <c>.Required("…")</c> call put
/// <c>required</c> and <c>aria-required="true"</c> on the input inside <c>.WithItemForm(...)</c> and
/// neither outside it.
/// <para>
/// Measured while fixing #190: the attribute armed no second validator — item fields sit in no
/// <c>MudForm</c> and carry no <c>For</c>, so MudBlazor's own required check never fired and the
/// field surfaced one message either way. The defect is the contradicted convention and the
/// accessibility semantics, not duplicate messages; the one-message test below pins the count so
/// that stays true if item fields are ever wired into MudBlazor's validation.
/// </para>
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

    [Fact]
    public void Required_ItemField_Should_Not_Put_The_Required_Attribute_On_The_Input_Element()
    {
        // Arrange & Act - the component property is the cause; this is the effect the convention
        // actually names ("Required() adds validation but NOT the HTML5 required attribute").
        // Measured before the fix: the input carried required="" and aria-required="true".
        var component = RenderOrderForm(BuildConfiguration(field => field
            .Required("Product name is required")));

        // Assert
        var input = component.FindAll("input")[0];
        input.HasAttribute("required").ShouldBeFalse();
        input.GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public async Task Blank_Required_ItemField_Should_Still_Fail_Validation_With_The_Configured_Message()
    {
        // Arrange - dropping the attribute must not drop the validation it never drove.
        var model = new OrderModel { Items = { new OrderItem() } };
        EditContext? editContext = null;

        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BuildConfiguration(field => field
                .Required("Product name is required")))
            .Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        // Act
        var isValid = true;
        await component.InvokeAsync(async () => isValid = await component.Instance.ValidateAsync());

        // Assert - the developer's own wording, on the nested identifier, and in the markup
        isValid.ShouldBeFalse();
        editContext.ShouldNotBeNull();
        editContext!.GetValidationMessages(new FieldIdentifier(model, "Items[0].ProductName"))
            .ShouldContain("Product name is required");
        component.WaitForAssertion(() =>
            component.Markup.ShouldContain("Product name is required"));
    }

    [Fact]
    public async Task Blank_Required_ItemField_Should_Surface_Exactly_One_Message()
    {
        // Arrange - MudBlazor's Required can drive a second, differently-worded required check of
        // its own. It does not fire for item fields today (no MudForm, no For), and emitting the
        // attribute was the only way it could ever be armed - so this pins the COUNT at one rather
        // than merely asserting the right text is present, and would catch the duplicate if that
        // wiring ever changed.
        var model = new OrderModel { Items = { new OrderItem() } };

        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BuildConfiguration(field => field
                .Required("Product name is required"))));

        // Act
        await component.InvokeAsync(() => component.Instance.ValidateAsync());

        // Assert - FieldValidationMessage renders one MudText per error for the field
        component.WaitForAssertion(() =>
            component.FindComponents<MudText>()
                .Count(t => t.Instance.Color == Color.Error)
                .ShouldBe(1));

        // ...and MudBlazor's own error slot stays empty, so nothing is queued behind it
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Error.ShouldBeFalse();
        textField.ErrorText.ShouldBeNullOrEmpty();
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
