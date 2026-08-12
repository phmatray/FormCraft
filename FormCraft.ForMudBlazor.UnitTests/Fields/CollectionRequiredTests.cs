namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields do NOT carry the HTML5 <c>Required</c> attribute (#190).
/// The project's validation convention is server-side only — messages come from the validator, no
/// component-path renderer emits <c>Required</c>, and the form is marked <c>novalidate</c>. Since
/// #206 that last part is an actual guarantee rather than a best effort: the attribute is rendered
/// on the form itself, so it applies during prerender and targets this component's own form. It was
/// previously applied by script to the *first* form in the document after the first render, which is
/// why this note used to qualify it. The collection
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

        // Assert - the property, and that it actually reaches the DOM. Asserting only the property
        // would stay green if a future change stopped it reaching the input, and this test is the
        // opt-in's only guard.
        var textField = component.FindComponent<MudTextField<string>>();
        textField.Instance.Required.ShouldBeTrue();
        textField.Find("input").HasAttribute("required").ShouldBeTrue();
        component.FindAll(".mud-input-required").Count.ShouldBe(1);
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
        // Arrange - the shared attribute block fed three renderers (text, numeric and date), so the
        // numeric one had to lose the forward too rather than being fixed only where it was
        // measured. Since #203 all three resolve it through one component property, so the "three
        // renderers" are three components reading the same rule.
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

        // Assert - anchored to the field under test, not to whichever input happens to be first
        var input = component.FindComponent<MudTextField<string>>().Find("input");
        input.HasAttribute("required").ShouldBeFalse();
        input.GetAttribute("aria-required").ShouldNotBe("true");
    }

    [Fact]
    public void Required_ItemField_Should_Not_Render_MudBlazors_Required_Asterisk()
    {
        // Arrange & Act - the asterisk is a CSS ::after on .mud-input-required, which MudBlazor adds
        // only when Required is true. A markup search for "*" cannot see it, so the CLASS is the
        // measurable proxy. This is the user-visible half of the change: a required item field now
        // looks exactly like a required ordinary field, which never carried one.
        var component = RenderOrderForm(BuildConfiguration(field => field
            .Required("Product name is required")));

        // Assert
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    [Fact]
    public void DateItemField_Should_Not_Render_The_Html5_Required_Attribute()
    {
        // Arrange - MudDatePicker was the THIRD renderer fed by the shared attribute block, and it
        // derives from MudFormComponent too, so it took the same forward. Its sibling suite covers
        // the date path for adornments (#184); this keeps that parity for Required.
        var config = FormBuilder<AppointmentModel>
            .Create()
            .AddCollectionField(x => x.Slots, collection => collection
                .WithLabel("Slots")
                .WithItemForm(item => item
                    .AddField(x => x.When, field => field
                        .WithLabel("When")
                        .Required("When is required"))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<AppointmentModel>>(parameters => parameters
            .Add(p => p.Model, new AppointmentModel { Slots = { new AppointmentSlot() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeFalse();
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    [Fact]
    public void DateItemField_Should_Honour_An_Explicit_Required_Attribute()
    {
        // Arrange - the opt-in has to reach the date path too, or the escape hatch is text/numeric
        // only while the README promises it for item fields generally.
        var config = FormBuilder<AppointmentModel>
            .Create()
            .AddCollectionField(x => x.Slots, collection => collection
                .WithLabel("Slots")
                .WithItemForm(item => item
                    .AddField(x => x.When, field => field
                        .WithLabel("When")
                        .WithAttribute("Required", true))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<AppointmentModel>>(parameters => parameters
            .Add(p => p.Model, new AppointmentModel { Slots = { new AppointmentSlot() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeTrue();
    }

    [Fact]
    public void BooleanItemField_With_An_Explicit_Required_Attribute_Should_Be_Inert()
    {
        // Arrange - MudCheckBox has no Required parameter, and MudBlazorBooleanFieldComponent binds
        // none, so the opt-in is inert here. Pinned rather than fixed, exactly as
        // CollectionAdornmentTests pins the same inertness for adornments (#184): the point is that
        // configuring it is harmless, not that it works.
        //
        // Before #203 this was inert because RenderBooleanField was a bespoke renderer that never
        // called the shared attribute block — inert on the item path while a standalone bool field
        // was inert for its own separate reason. Both are now the SAME reason, so this can no longer
        // drift into a divergence the way the adornment case did.
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.IsGift, field => field
                        .WithLabel("Gift")
                        .WithAttribute("Required", true))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));

        // Assert - renders, does not throw, and takes no notice of the attribute
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Gift");
        component.FindAll(".mud-input-required").ShouldBeEmpty();
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

        // Assert - scoped to the FieldValidationMessage bound to Items[0].ProductName, not to every
        // error-coloured MudText in the form: a second item field, or any unrelated error text,
        // would otherwise move this count for a reason that has nothing to do with duplication.
        component.WaitForAssertion(() =>
            component.FindComponents<FieldValidationMessage>()
                .Single(m => m.Instance.FieldName == "Items[0].ProductName")
                .FindComponents<MudText>()
                .Count(t => t.Instance.Color == Color.Error)
                .ShouldBe(1));

        // ...and MudBlazor's own error slot stays empty, so nothing is queued behind it
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Error.ShouldBeFalse();
        textField.ErrorText.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void TextItemField_With_WithNativeRequired_Should_Render_The_Decoration()
    {
        // Arrange & Act - #204. The same opt-in as the raw string above, through the typed method.
        // Asserted on all three field kinds that bind Required, because the escape hatch being
        // text-only would be the exact divergence class this library keeps re-filing.
        var component = RenderOrderForm(BuildConfiguration(field => field.WithNativeRequired()));

        // Assert - the component property, and the class MudBlazor's asterisk hangs off.
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeTrue();
        component.FindAll(".mud-input-required").ShouldNotBeEmpty();
    }

    [Fact]
    public void NumericItemField_With_WithNativeRequired_Should_Render_The_Decoration()
    {
        // Arrange - the second renderer. WithNativeRequired is declared on the general TValue
        // overload rather than string-only precisely so this compiles.
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field => field
                        .WithLabel("Quantity")
                        .WithNativeRequired())))
            .Build();

        // Act
        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Required.ShouldBeTrue();
    }

    [Fact]
    public void DateItemField_With_WithNativeRequired_Should_Render_The_Decoration()
    {
        // Arrange - the third renderer, MudDatePicker.
        var config = FormBuilder<AppointmentModel>
            .Create()
            .AddCollectionField(x => x.Slots, collection => collection
                .WithLabel("Slots")
                .WithItemForm(item => item
                    .AddField(x => x.When, field => field
                        .WithLabel("When")
                        .WithNativeRequired())))
            .Build();

        // Act
        var component = Render<FormCraftComponent<AppointmentModel>>(parameters => parameters
            .Add(p => p.Model, new AppointmentModel { Slots = { new AppointmentSlot() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeTrue();
    }

    [Fact]
    public void BooleanItemField_With_WithNativeRequired_Should_Be_Inert()
    {
        // Arrange - MudBlazorBooleanFieldComponent binds no Required, so the opt-in is inert here
        // through the typed method exactly as through the raw string. Pinned rather than fixed,
        // mirroring the adornment inertness test (#184): the claim is that configuring it is
        // harmless, not that it works. Inert identically on both placements since #203.
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.IsGift, field => field
                        .WithLabel("Gift")
                        .WithNativeRequired())))
            .Build();

        // Act
        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Gift");
        component.FindAll(".mud-input-required").ShouldBeEmpty();
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
