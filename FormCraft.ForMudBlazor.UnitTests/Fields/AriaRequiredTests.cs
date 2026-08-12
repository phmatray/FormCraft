namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that a <c>.Required(...)</c> field is announced as required to assistive technology, on
/// both render paths (#199). WCAG 2.1 <b>3.3.2 Labels or Instructions</b> (Level A) expects required
/// fields to be identified; before this they were not, on either path.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the HTML5 attribute comes back.</b> The issue asked for <c>aria-required="true"</c>
/// <i>without</i> HTML5 <c>required</c>. Measured against MudBlazor 9.8.0, that is unreachable:
/// <c>MudInput</c> splats <c>UserAttributes</c> into the element and then writes its own
/// <c>required</c> and <c>aria-required</c> afterwards, both off the single <c>Required</c> bool.
/// Blazor resolves duplicate attributes last-write-wins, so a caller-supplied <c>aria-required</c>
/// is always overwritten and the two attributes cannot be separated. The owner's decision was to
/// drive <c>Required</c> from <c>IsRequired</c> and accept the HTML5 attribute, which is inert here:
/// FormCraft forms render <c>novalidate</c>, a guarantee #206 pins with tests.
/// </para>
/// <para>
/// This deliberately reverses the collection-path half of #190. What #190 actually fixed was the
/// <i>divergence</i> — the same <c>.Required("…")</c> call decorating an item field and not an
/// ordinary one — and that stays fixed, because both paths now resolve the flag the same way. What
/// it also did, and what this undoes, is level the two paths down to silence.
/// </para>
/// <para>
/// ⚠️ <c>aria-required="false"</c> is asserted for optional fields rather than the attribute's
/// absence. MudBlazor emits it unconditionally, and <c>false</c> is the correct ARIA value for an
/// optional field, so its presence is not the defect — the defect was a <i>required</i> field
/// saying <c>"false"</c>, which is an affirmatively wrong statement to a screen reader rather than
/// merely a missing one.
/// </para>
/// </remarks>
public class AriaRequiredTests : MudBlazorTestBase
{
    [Fact]
    public void Required_Field_Should_Announce_Itself_To_Assistive_Technology()
    {
        // Arrange & Act - the plain .Required(...) call, on an ordinary (non-collection) field
        var component = RenderField(f => f.WithLabel("Name").Required("Name is required"));

        // Assert - the attribute a screen reader actually reads, on the element it reads it from
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_Field_Should_Not_Be_Announced_As_Required()
    {
        // Arrange & Act - the overwhelmingly common case must stay untouched
        var component = RenderField(f => f.WithLabel("Name"));

        // Assert - "false" rather than absent: MudBlazor emits it either way, and false is correct
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public void Required_Field_Should_Also_Carry_The_Visible_Required_Marker()
    {
        // Arrange & Act - MudBlazor's asterisk is a CSS ::after on .mud-input-required, so the CLASS
        // is the measurable proxy. The spec listed the asterisk as a non-goal; under this mechanism
        // it is not separable from the ARIA flag, so it ships - and it is itself a WCAG 3.3.2
        // *visible* identification. Pinned so the pairing is a decision on record, not an accident.
        var component = RenderField(f => f.WithLabel("Name").Required("Name is required"));

        // Assert
        component.FindAll(".mud-input-required").ShouldNotBeEmpty();
    }

    [Fact]
    public void Explicit_Native_Required_Opt_Out_Should_Win_Over_Required()
    {
        // Arrange & Act - the escape hatch has to work in BOTH directions once .Required(...) drives
        // the flag. Without this, a caller who deliberately suppressed the decoration in #204 would
        // silently get it back.
        var component = RenderField(f => f
            .WithLabel("Name")
            .Required("Name is required")
            .WithNativeRequired(false));

        // Assert
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
        component.Find("input").HasAttribute("required").ShouldBeFalse();
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    [Fact]
    public void Explicit_Native_Required_Opt_In_Should_Still_Work_Without_Required()
    {
        // Arrange & Act - #204's opt-in keeps working on a field that never called .Required(...)
        var component = RenderField(f => f.WithLabel("Name").WithNativeRequired());

        // Assert
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Numeric_Field_Should_Announce_Itself()
    {
        // Arrange - the shared base property feeds every component-path renderer that binds
        // Required, so the numeric one must follow the text one rather than be fixed separately.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Quantity, f => f.WithLabel("Quantity").Required("Quantity is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Date_Field_Should_Announce_Itself()
    {
        // Arrange - MudDatePicker is the third component-path family binding Required, and the one
        // #190 missed first time round.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.When, f => f.WithLabel("When").Required("When is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Text_Item_Field_Should_Announce_Itself()
    {
        // Arrange & Act - the collection path builds its tree imperatively, so it has to resolve the
        // flag by the same rule rather than inherit it. AddCommonFieldAttributes feeds three
        // renderers and #190 fixed only the one it was measured on, so all three are covered here.
        var component = RenderOrderItem(f => f.WithLabel("Product").Required("Product is required"));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Numeric_Item_Field_Should_Announce_Itself()
    {
        // Arrange & Act - the second of the three renderers
        var component = RenderBasketItem(item => item
            .AddField(x => x.Quantity, f => f.WithLabel("Quantity").Required("Quantity is required")));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Date_Item_Field_Should_Announce_Itself()
    {
        // Arrange & Act - the third, MudDatePicker, which #190 missed on the first pass
        var component = RenderAppointmentItem(item => item
            .AddField(x => x.When, f => f.WithLabel("When").Required("When is required")));

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_Item_Field_Should_Not_Be_Announced_As_Required()
    {
        // Arrange & Act - the item-path counterpart of the ordinary-field case above
        var component = RenderOrderItem(f => f.WithLabel("Product"));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeFalse();
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public void Explicit_Native_Required_Opt_Out_Should_Win_On_The_Item_Path_Too()
    {
        // Arrange & Act - the escape hatch has to behave identically on both paths, or it becomes
        // the next divergence. GetItemFieldRequired tests presence separately from value precisely
        // so this case does not collapse into the "not configured" fallback.
        var component = RenderOrderItem(f => f
            .WithLabel("Product")
            .Required("Product is required")
            .WithNativeRequired(false));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeFalse();
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public void Required_Boolean_Item_Field_Should_Announce_Itself()
    {
        // Arrange - THE DECIDED BOOLEAN CASE (#199 Task 3 Step 1): announced, not pinned inert.
        // RenderBooleanField takes none of AddCommonFieldAttributes' set (MudCheckBox shares almost
        // none of those parameters), so it resolves the flag itself by the same rule.
        //
        // A required consent checkbox is the single most common required control that is NOT a text
        // field, so leaving it silent would have left the headline case of this issue unfixed —
        // and worse than silent once every required text field carries an asterisk, because absence
        // would then read as "optional".
        var component = RenderBasketItem(item => item
            .AddField(x => x.IsGift, f => f.WithLabel("Gift").Required("Gift is required")));

        // Assert - the parameter, the asterisk class, and the attribute on the real <input>
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Gift");
        component.FindComponent<MudCheckBox<bool>>().Instance.Required.ShouldBeTrue();
        component.FindAll(".mud-input-required").ShouldNotBeEmpty();
        component.Find("input[type=checkbox]").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Boolean_Field_Should_Announce_Itself_On_The_Component_Path_Too()
    {
        // Arrange - the ordinary-field half of the same case. MudBlazorBooleanFieldComponent derives
        // from FieldComponentBase rather than MudBlazorFieldComponentBase, so it has no inherited
        // EffectiveNativeRequired and had to resolve the rule itself — precisely the shape of
        // divergence this library keeps re-filing, so both halves are asserted.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Accepted, f => f.WithLabel("Accept").Required("You must accept"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudCheckBox<bool>>().Instance.Required.ShouldBeTrue();
        component.Find("input[type=checkbox]").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_Boolean_Field_Should_Not_Be_Announced_As_Required()
    {
        // Arrange & Act - the common case stays untouched on the checkbox path too
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Accepted, f => f.WithLabel("Accept"))
            .Build();

        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudCheckBox<bool>>().Instance.Required.ShouldBeFalse();
        component.Find("input[type=checkbox]").GetAttribute("aria-required").ShouldBe("false");
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    [Fact]
    public void Required_Select_Field_Should_Announce_Itself()
    {
        // Arrange - a required dropdown is one of the commonest required controls, and was the
        // headline example of the gap: with every required text field carrying an asterisk, an
        // unmarked Country select reads as OPTIONAL rather than merely unannounced.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Country, f => f
                .WithLabel("Country")
                .WithOptions(("US", "United States"), ("BE", "Belgium"))
                .Required("Country is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - MudSelect forwards Required to its inner MudInput, which is what emits the ARIA
        component.FindComponent<MudSelect<string>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_Select_Field_Should_Not_Be_Announced_As_Required()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Country, f => f
                .WithLabel("Country")
                .WithOptions(("US", "United States"), ("BE", "Belgium")))
            .Build();

        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudSelect<string>>().Instance.Required.ShouldBeFalse();
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public void Required_Autocomplete_Field_Should_Announce_Itself()
    {
        // Arrange - MudAutocomplete forwards Required to its inner MudInput like MudSelect does.
        // Lookup and LOV fields are not separately asserted here: both render a plain MudTextField,
        // so they are the already-covered text case with an adornment attached.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Country, f => f
                .WithLabel("City")
                .AsAutocomplete(SearchAsync)
                .Required("City is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudAutocomplete<string>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_FileUpload_Field_Should_Stay_Unannotated()
    {
        // Arrange - THE DECIDED FILE-UPLOAD CASE. Pinned as unannotated rather than fixed, and
        // unlike the boolean case that is not a reversal waiting to happen.
        //
        // MudFileUpload does accept Required and would emit aria-required on its <input
        // type="file">. But FormCraft renders that input with `tabindex="-1"` and `opacity-0`
        // beneath a custom drop zone, so it is deliberately OUT of the tab order: a screen-reader
        // user never lands on the element the annotation would sit on, and the affordance they do
        // reach is a MudButton that takes no such attribute. Annotating the hidden input would
        // satisfy a DOM assertion while telling no user anything — the "forwarded but inert" failure
        // this suite's sibling tests exist to catch.
        //
        // Marking a required upload needs a label-level answer (the field's own <MudText> label, or
        // aria-describedby on the button), which is a design decision rather than one more binding.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - renders, does not throw, and carries no required annotation
        component.FindComponent<MudFileUpload<IBrowserFile>>().Instance.Required.ShouldBeFalse();
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    private static Task<IEnumerable<SelectOption<string>>> SearchAsync(
        string value,
        CancellationToken cancellationToken) =>
        Task.FromResult<IEnumerable<SelectOption<string>>>(
        [
            new SelectOption<string>("BE", "Brussels"),
            new SelectOption<string>("FR", "Paris"),
        ]);

    [Fact]
    public async Task Blank_Required_Field_Should_Surface_Exactly_One_Message()
    {
        // Arrange - the behavioural risk of driving MudBlazor's Required from .Required(...):
        // MudFormComponent can run a required check of its OWN, differently worded, which would
        // double every required message on every form in the library. CollectionRequiredTests pins
        // this for item fields; nothing pinned it for ordinary ones, and ordinary fields are the
        // ones this issue newly sets the flag on — so the guard belongs here too.
        //
        // It does not fire today (fields carry no `For` and sit in no MudForm), but "does not fire
        // today" is exactly the kind of claim that needs a test rather than a comment.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").Required("Name is required"))
            .Build();

        var component = RenderConfig(config);

        // Act
        await component.InvokeAsync(() => component.Instance.ValidateAsync());

        // Assert - exactly one error message, and it is the developer's own wording
        component.WaitForAssertion(() =>
            component.FindComponents<FieldValidationMessage>()
                .Single(m => m.Instance.FieldName == nameof(TestModel.Name))
                .FindComponents<MudText>()
                .Count(t => t.Instance.Color == Color.Error)
                .ShouldBe(1));

        // ...and MudBlazor's own error slot stays empty, so nothing is queued behind it
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Error.ShouldBeFalse();
        textField.ErrorText.ShouldBeNullOrEmpty();
    }

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderOrderItem(
        Action<FieldBuilder<OrderItem, string>> configure)
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item.AddField(x => x.ProductName, configure)))
            .Build();

        return Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, new OrderModel { Items = { new OrderItem() } })
            .Add(p => p.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<BasketModel>> RenderBasketItem(
        Action<FormBuilder<BasketLine>> configureItemForm)
    {
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(configureItemForm))
            .Build();

        return Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<AppointmentModel>> RenderAppointmentItem(
        Action<FormBuilder<AppointmentSlot>> configureItemForm)
    {
        var config = FormBuilder<AppointmentModel>
            .Create()
            .AddCollectionField(x => x.Slots, collection => collection
                .WithLabel("Slots")
                .WithItemForm(configureItemForm))
            .Build();

        return Render<FormCraftComponent<AppointmentModel>>(parameters => parameters
            .Add(p => p.Model, new AppointmentModel { Slots = { new AppointmentSlot() } })
            .Add(p => p.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderField(
        Action<FieldBuilder<TestModel, string>> configure)
    {
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, configure)
            .Build();

        return RenderConfig(config);
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderConfig(
        IFormConfiguration<TestModel> config) =>
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, config));

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public DateTime When { get; set; }

        public bool Accepted { get; set; }

        public string Country { get; set; } = string.Empty;

        public IBrowserFile? Upload { get; set; }
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
