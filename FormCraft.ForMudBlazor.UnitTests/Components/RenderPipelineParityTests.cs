using System.Globalization;
using FormCraft.ForMudBlazor.UnitTests.Fields;
using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Parity tests for the consolidated render pipeline (#148): behaviors that used
/// to live only in FormCraftComponent's legacy type-switch (Options-driven selects,
/// MinDate/MaxDate passthrough, invariant Culture on numeric fields, checkbox
/// rendering for booleans, Variant/Margin/ShrinkLabel/Immediate settings) must be
/// produced identically by the FieldRendererService components.
/// </summary>
/// <remarks>
/// The collection-item comparisons below were the sharp end of this suite while a field inside
/// <c>.WithItemForm(...)</c> was rendered by a second, hand-written implementation — they compared
/// two genuinely different renderers, and repeatedly caught them disagreeing.
/// <para>
/// Since #203 there is one implementation, so those comparisons are close to a tautology. That is
/// the intended end state, not a reason to delete them: they now pin the wiring rather than the
/// attributes — that a collection item field still reaches the same component, with the field's own
/// configuration and the form's cascaded defaults intact. A regression that re-routed item fields,
/// dropped the cascade, or reintroduced a bespoke branch would surface here first.
/// </para>
/// </remarks>
public class RenderPipelineParityTests : MudBlazorTestBase
{
    private IRenderedComponent<FormCraftComponent<TestModel>> RenderForm(IFormConfiguration<TestModel> config, TestModel? model = null)
    {
        model ??= new TestModel();

        // Render next to a MudPopoverProvider so picker/select components work.
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<FormCraftComponent<TestModel>>(1);
            builder.AddComponentParameter(2, "Model", model);
            builder.AddComponentParameter(3, "Configuration", config);
            builder.CloseComponent();
        });

        return cut.FindComponent<FormCraftComponent<TestModel>>();
    }

    [Fact]
    public void StringSelect_Should_Render_MudSelect_With_Options()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, field => field
                .WithLabel("Status")
                .WithOptions(
                    ("active", "Active"),
                    ("inactive", "Inactive")))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var select = component.FindComponent<MudSelect<string>>();
        select.Instance.Label.ShouldBe("Status");

        var fieldComponent = component.FindComponent<MudBlazorSelectFieldComponent<TestModel, string>>();
        fieldComponent.Instance.Options.Count().ShouldBe(2);
        fieldComponent.Instance.Options.Select(o => o.Label).ShouldBe(new[] { "Active", "Inactive" });
    }

    [Fact]
    public async Task StringSelect_ValueChanged_Should_Update_Model()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, field => field
                .WithLabel("Status")
                .WithOptions(
                    ("active", "Active"),
                    ("inactive", "Inactive")))
            .Build();

        var component = RenderForm(config, model);
        var select = component.FindComponent<MudSelect<string>>();

        // Act
        await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync("inactive"));

        // Assert
        model.Status.ShouldBe("inactive");
    }

    [Fact]
    public void IntSelect_Should_Render_MudSelect_With_Value_Type_Options()
    {
        // Arrange
        var model = new TestModel { Priority = 2 };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Priority, field => field
                .WithLabel("Priority")
                .WithOptions(
                    (1, "Low"),
                    (2, "High")))
            .Build();

        // Act
        var component = RenderForm(config, model);

        // Assert
        var select = component.FindComponent<MudSelect<int>>();
        select.Instance.Label.ShouldBe("Priority");
        select.Instance.Value.ShouldBe(2);

        var fieldComponent = component.FindComponent<MudBlazorSelectFieldComponent<TestModel, int>>();
        fieldComponent.Instance.Options.Select(o => o.Value).ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public async Task NullableIntSelect_Should_Render_And_Update_Model()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Rating, field => field
                .WithLabel("Rating")
                .WithOptions(
                    ((int?)1, "One"),
                    ((int?)2, "Two")))
            .Build();

        var component = RenderForm(config, model);

        // Assert - renders a select bound to the nullable value type
        var select = component.FindComponent<MudSelect<int?>>();
        var fieldComponent = component.FindComponent<MudBlazorSelectFieldComponent<TestModel, int?>>();
        fieldComponent.Instance.Options.Count().ShouldBe(2);

        // Act
        await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(2));

        // Assert
        model.Rating.ShouldBe(2);
    }

    [Fact]
    public void NullableIntSelect_Should_Accept_Options_Typed_With_Underlying_Value_Type()
    {
        // Arrange - the legacy switch converted ANY enumerable with Value/Label
        // properties via reflection; SelectOption<int> on an int? field must work.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Rating, field => field
                .WithLabel("Rating")
                .WithAttribute("Options", new List<SelectOption<int>>
                {
                    new(1, "One"),
                    new(2, "Two"),
                }))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var fieldComponent = component.FindComponent<MudBlazorSelectFieldComponent<TestModel, int?>>();
        fieldComponent.Instance.Options.Select(o => o.Value).ShouldBe(new int?[] { 1, 2 });
        fieldComponent.Instance.Options.Select(o => o.Label).ShouldBe(new[] { "One", "Two" });
    }

    [Fact]
    public void Select_Should_Accept_Untyped_Options_With_Value_And_Label_Properties()
    {
        // Arrange - parity with the legacy reflection-based option conversion
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, field => field
                .WithLabel("Status")
                .WithAttribute("Options", new List<CustomOption>
                {
                    new() { Value = "a", Label = "Alpha" },
                    new() { Value = "b", Label = "Beta" },
                }))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var fieldComponent = component.FindComponent<MudBlazorSelectFieldComponent<TestModel, string>>();
        fieldComponent.Instance.Options.Select(o => o.Value).ShouldBe(new[] { "a", "b" });
        fieldComponent.Instance.Options.Select(o => o.Label).ShouldBe(new[] { "Alpha", "Beta" });
    }

    [Fact]
    public void NumericField_Should_Use_Invariant_Culture()
    {
        // Arrange - the legacy switch always set Culture to InvariantCulture
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Priority, field => field.WithLabel("Priority"))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>();
        numeric.Instance.Culture.ShouldBe(CultureInfo.InvariantCulture);
    }

    [Fact]
    public void NumericField_Should_Allow_Culture_Override_Via_Attribute()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Priority, field => field
                .WithLabel("Priority")
                .WithAttribute("Culture", culture))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>();
        numeric.Instance.Culture.ShouldBe(culture);
    }

    [Fact]
    public void NumericField_Should_Keep_Legacy_Styling_Settings()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Priority, field => field.WithLabel("Priority"))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert - Variant/Margin/ShrinkLabel/Immediate exactly as the legacy switch
        var numeric = component.FindComponent<MudNumericField<int>>();
        numeric.Instance.Variant.ShouldBe(Variant.Outlined);
        numeric.Instance.Margin.ShouldBe(Margin.Dense);
        numeric.Instance.ShrinkLabel.ShouldBeTrue();
        numeric.Instance.Immediate.ShouldBeTrue();
    }

    [Fact]
    public void TextField_Should_Keep_Legacy_Styling_Settings()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, field => field.WithLabel("Status"))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var text = component.FindComponent<MudTextField<string>>();
        text.Instance.Variant.ShouldBe(Variant.Outlined);
        text.Instance.Margin.ShouldBe(Margin.Dense);
        text.Instance.ShrinkLabel.ShouldBeTrue();
        text.Instance.Immediate.ShouldBeTrue();
    }

    [Fact]
    public void DateTimeField_Should_Pass_MinDate_And_MaxDate_To_DatePicker()
    {
        // Arrange - MinDate/MaxDate were honored only by the legacy switch path
        var min = new DateTime(2020, 1, 1);
        var max = new DateTime(2030, 12, 31);
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.BirthDate, field => field
                .WithLabel("Birth Date")
                .WithAttribute("MinDate", min)
                .WithAttribute("MaxDate", max))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var picker = component.FindComponent<MudDatePicker>();
        picker.Instance.MinDate.ShouldBe(min);
        picker.Instance.MaxDate.ShouldBe(max);
    }

    [Fact]
    public async Task DateTimeField_Should_Update_Model_On_Change()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.BirthDate, field => field.WithLabel("Birth Date"))
            .Build();

        var component = RenderForm(config, model);
        var picker = component.FindComponent<MudDatePicker>();

        // Act
        await component.InvokeAsync(() => picker.Instance.DateChanged.InvokeAsync(new DateTime(1990, 6, 15)));

        // Assert
        model.BirthDate.ShouldBe(new DateTime(1990, 6, 15));
    }

    [Fact]
    public void BooleanField_Should_Render_As_Checkbox_By_Default()
    {
        // Arrange - the legacy switch rendered MudCheckBox, not MudSwitch
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.IsActive, field => field.WithLabel("Is Active"))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var checkbox = component.FindComponent<MudCheckBox<bool>>();
        checkbox.Instance.Label.ShouldBe("Is Active");
        component.FindComponents<MudSwitch<bool>>().ShouldBeEmpty();
    }

    [Fact]
    public void BooleanField_Should_Render_As_Switch_When_DisplayStyle_Requests_It()
    {
        // Arrange - the renderer component keeps its opt-in switch style
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.IsActive, field => field
                .WithLabel("Is Active")
                .WithAttribute("DisplayStyle", BooleanDisplayStyle.Switch))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        component.FindComponents<MudSwitch<bool>>().Count.ShouldBe(1);
        component.FindComponents<MudCheckBox<bool>>().ShouldBeEmpty();
    }

    [Fact]
    public void MultiSelectField_Should_Render_MultiSelection_MudSelect()
    {
        // Arrange - the legacy switch silently skipped MultiSelectOptions fields;
        // the consolidated pipeline renders a proper multi-select.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Categories, field => field
                .WithLabel("Categories")
                .AsMultiSelect(
                    ("tech", "Technology"),
                    ("health", "Healthcare")))
            .Build();

        // Act
        var component = RenderForm(config);

        // Assert
        var select = component.FindComponent<MudSelect<string>>();
        select.Instance.MultiSelection.ShouldBeTrue();
        select.Instance.Label.ShouldBe("Categories");
        component.Markup.ShouldNotContain("Unsupported field type");
    }

    [Fact]
    public async Task MultiSelectField_SelectedValues_Should_Update_Model()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Categories, field => field
                .WithLabel("Categories")
                .AsMultiSelect(
                    ("tech", "Technology"),
                    ("health", "Healthcare")))
            .Build();

        var component = RenderForm(config, model);
        var select = component.FindComponent<MudSelect<string>>();

        // Act
        await component.InvokeAsync(() =>
            select.Instance.SelectedValuesChanged.InvokeAsync(new[] { "tech", "health" }));

        // Assert
        model.Categories.ShouldNotBeNull();
        model.Categories.ShouldBe(new[] { "tech", "health" });
    }

    [Fact]
    public void CollectionItemField_Should_Honour_The_Same_Presentation_Attributes_As_A_Standalone_Field()
    {
        // Arrange - the SAME builder calls applied to a standalone field and to a collection item
        // field. The two go through different renderers (component vs CollectionFieldComponent's
        // RenderTreeBuilder), and presentation attributes have repeatedly drifted between them:
        // Variant in #146, ShrinkLabel in #177, the adornments in #184 — each found reactively,
        // years apart. This pins the set the two paths DO agree on, so a regression in any of them
        // fails here. Since #203 that set is the whole of it — the divergence list in Presentation()
        // is empty, because the second renderer that produced the divergences is gone.
        // One field carrying every compared attribute at once, so a single comparison covers the
        // whole set. Two deliberate exclusions:
        //
        // - AsPassword's visibility toggle is off: it is component-path-only and would replace the
        //   start adornment below with its own eye icon, failing this test for a reason it does not
        //   exist to test.
        // - Lines stays at its default 1, and is covered by the multiline test below instead.
        //   MudBlazor renders a <textarea> once Lines > 1, and a textarea has no `type` attribute.
        //   Until #207 that made the combination genuinely unsafe to assert here: InputType went
        //   inert and this test's headline masking assertion became vacuous, comparing a parameter
        //   that changed nothing. That is no longer why Lines is excluded — the combination is now
        //   defined behaviour (masking wins, the field renders on one line) and is asserted by
        //   PasswordCollectionItemField_... below. Lines stays out of THIS field only because a
        //   password field always renders Lines=1, so putting it here would compare a constant.
        static void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field
                .WithLabel("Product")
                .WithPlaceholder("e.g. Widget")
                .WithHelpText("The catalogue name")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, Color.Secondary)
                .WithVariant(Variant.Filled)
                .AsPassword(enableVisibilityToggle: false)
                .WithAttribute("MaxLength", 500)
                .WithAttribute("Mask", "aaaa-0000")
                .WithAutocomplete("current-password")
                .Required("Product name is required");

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var standalone = standaloneRender.FindComponent<MudTextField<string>>().Instance;

        var itemRender = this.RenderItemForm(NewOrder(), collectionConfig);
        var itemField = itemRender.FindComponent<MudTextField<string>>().Instance;

        // Assert - compared as one set, so a newly-honoured attribute on the component path that
        // the collection path ignores shows up here rather than in a bug report
        Presentation(itemField).ShouldBe(Presentation(standalone));

        // Guard the guard: a comparison of two all-default fields would pass while proving nothing.
        standalone.Adornment.ShouldBe(Adornment.Start);
        standalone.Variant.ShouldBe(Variant.Filled);
        standalone.InputType.ShouldBe(InputType.Password);
        standalone.MaxLength.ShouldBe(500);
        // Without this the Mask entry in Presentation() would be a null-vs-null comparison — present
        // in the array, proving nothing, and reading as coverage. Exactly the trap this method's own
        // doc warns about, so the guard has to include the attribute that was just added to it.
        standalone.Mask?.Mask.ShouldBe("aaaa-0000");
        standalone.UserAttributes.GetValueOrDefault("autocomplete").ShouldBe("current-password");

        // And guard against the subtler failure the parameter comparison cannot see: a value that
        // is forwarded but has no effect on the rendered element. `type="password"` is what masks
        // the characters, so assert the DOM, on both paths.
        standaloneRender.Find("input").GetAttribute("type").ShouldBe("password");
        itemRender.Find("input").GetAttribute("type").ShouldBe("password");

        // Required is compared here for a .Required(...) field, which since #263 renders FALSE on
        // both paths: the announcement moved to aria-required via UserAttributes, and MudBlazor's
        // Required parameter — which drags the HTML5 attribute and the asterisk along with it — is
        // now reserved for the explicit .WithNativeRequired() opt-in.
        //
        // The value has now flipped twice (#190 false, #199 true, #263 false) while the guard stayed
        // put, so it is worth being explicit about what it still guards. On its own this line no
        // longer distinguishes "correctly announced" from "levelled back down to silence" — both
        // read false here. The pair below does: aria-required must be "true" on the standalone path
        // AND equal on the item path. So read the two together — this line pins that the native
        // decoration is off, the next pins that the field is still announced anyway. Silence fails
        // the second even though it passes the first.
        standalone.Required.ShouldBeFalse();

        // And the accessibility attribute itself, on both paths (#199). The parameter comparison
        // above cannot see this: Required is what FormCraft sets, aria-required is what MudBlazor
        // derives from it and what a screen reader actually reads. Asserting only the parameter
        // would stay green if the value stopped reaching the element — the same "forwarded but
        // inert" failure the InputType assertions above exist to catch.
        standaloneRender.Find("input").GetAttribute("aria-required").ShouldBe("true");
        itemRender.Find("input").GetAttribute("aria-required")
            .ShouldBe(standaloneRender.Find("input").GetAttribute("aria-required"));
    }

    [Theory]
    [InlineData("email")]
    [InlineData("tel")]
    [InlineData("url")]
    [InlineData("search")]
    [InlineData("number")]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("definitely-not-an-input-type")]
    public void InputType_Should_Resolve_Identically_On_Both_Paths(string configured)
    {
        // Arrange - #210 widened TextInputTypeMap with number/date/time. Both paths resolve through
        // that one method, which is exactly why #189 extracted it — so this compares the whole
        // recognised set rather than the newly added arms alone, and includes an unrecognised value
        // so the shared fallback to `text` is pinned too.
        //
        // `password` is deliberately absent: #207 makes a masked field render on a single line, and
        // its cross-path behaviour is asserted by PasswordCollectionItemField_… below.
        void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Value").WithInputType(configured);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = this.RenderItemForm(NewOrder(), collectionConfig);

        // Assert - the resolved parameter agrees, and so does the attribute the browser reads.
        var standaloneType = standaloneRender.FindComponent<MudTextField<string>>().Instance.InputType;

        itemRender.FindComponent<MudTextField<string>>().Instance.InputType.ShouldBe(standaloneType);
        itemRender.Find("input").GetAttribute("type")
            .ShouldBe(standaloneRender.Find("input").GetAttribute("type"));
    }

    [Theory]
    [InlineData("0000-0000")]
    [InlineData("(000) 000-0000")]
    [InlineData("aaa-000")]
    [InlineData("**-**")]
    public void Mask_Should_Resolve_Identically_On_Both_Paths(string configured)
    {
        // Arrange - #211. Masks were the last entry on this class's "deliberately NOT compared" list:
        // the component path read the string into a property and dropped it, and the collection path
        // did not forward it at all, so `.WithAttribute("Mask", …)` was inert everywhere. Both paths
        // now resolve through TextMaskMap, which is what makes them comparable rather than merely
        // equally broken.
        void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Value").WithAttribute("Mask", configured);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = this.RenderItemForm(NewOrder(), collectionConfig);

        // Assert - the pattern and the mask TYPE, never the instance: each path builds its own IMask,
        // so reference equality would fail for two identically-configured fields and prove nothing.
        var standaloneMask = standaloneRender.FindComponent<MudTextField<string>>().Instance.Mask;
        var itemMask = itemRender.FindComponent<MudTextField<string>>().Instance.Mask;

        standaloneMask.ShouldNotBeNull();
        itemMask.ShouldNotBeNull();
        itemMask.Mask.ShouldBe(standaloneMask.Mask);

        // The concrete type is asserted, across every pattern in the theory, because it is
        // load-bearing rather than incidental. MudMask.SetMask preserves the user's text and caret
        // only when the mask it is handed is of the SAME TYPE as the one it already holds:
        //
        //     if (_mask.GetType() == other.GetType()) { _mask.UpdateFrom(other); return; }
        //     other.SetText(ReadText);
        //     _mask = other;
        //
        // Both paths construct a new instance on every render, and Immediate="true" means a render
        // per keystroke — so a resolver that returned a different IMask implementation for some
        // patterns would swap the mask out mid-edit. Pinning the type over several patterns is what
        // catches that.
        standaloneMask.ShouldBeOfType<PatternMask>();
        itemMask.ShouldBeOfType<PatternMask>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Mask_Options_Should_Resolve_Identically_On_Both_Paths(bool cleanDelimiters)
    {
        // Arrange - #265's half of the mask parity question. The test above pins the PATTERN; this
        // pins the OPTION that travels with it, which is the half that decides what the model
        // actually stores. A path that resolved the same pattern with CleanDelimiters left at its
        // default would satisfy every assertion above while writing "(555) 123-4567" where the other
        // wrote "5551234567" — a divergence invisible to a pattern-only comparison.
        //
        // Since #203/#250 both paths render through the same MudBlazorTextFieldComponent, so this
        // passes structurally rather than because two implementations were kept in step. That is
        // exactly why it is worth pinning: the convergence is the thing that could regress, and a
        // future change that reintroduces a separate item-field attribute reader would land on a
        // green suite without it.
        void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Value").WithMask("(000) 000-0000", cleanDelimiters);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item.AddField(x => x.ProductName, Configure)))
            .Build();

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, new OrderModel { Items = { new OrderItem() } })
            .Add(p => p.Configuration, collectionConfig));

        // Assert - type, pattern and option, never the instance: each path builds its own.
        var standaloneMask = standaloneRender.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<PatternMask>();
        var itemMask = itemRender.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<PatternMask>();

        itemMask.Mask.ShouldBe(standaloneMask.Mask);
        standaloneMask.CleanDelimiters.ShouldBe(cleanDelimiters);
        itemMask.CleanDelimiters.ShouldBe(cleanDelimiters);
    }

    [Fact]
    public void A_Supplied_Mask_Should_Resolve_Identically_On_Both_Paths()
    {
        // Arrange - #265's factory overload, held to the same parity bar as the pattern above. A
        // supplied mask takes a different branch of Resolve than a pattern string does, so it is
        // its own opportunity for the two paths to disagree.
        void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Value").WithMask(() => new RegexMask("^[0-9]{0,4}$"));

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item.AddField(x => x.ProductName, Configure)))
            .Build();

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, new OrderModel { Items = { new OrderItem() } })
            .Add(p => p.Configuration, collectionConfig));

        // Assert - the same implementation type on both, which is the property MudMask.SetMask cares
        // about, and the same pattern.
        var standaloneMask = standaloneRender.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<RegexMask>();
        var itemMask = itemRender.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<RegexMask>();

        itemMask.Mask.ShouldBe(standaloneMask.Mask);
        itemMask.Mask.ShouldBe("^[0-9]{0,4}$");
    }

    [Fact]
    public void A_Mask_Configured_Once_Should_Not_Be_Shared_Between_Collection_Rows()
    {
        // Arrange - every row of a collection reads the SAME IFieldConfiguration, so anything the
        // configuration stores by reference is handed to every row's component. A BaseMask is not a
        // value: it carries the live Text, CaretPos and Selection of the input it is attached to, and
        // MudMask.SetMask adopts an incoming mask outright (`_mask = other`) whenever its type
        // differs from the PatternMask it seeds itself with — which is every RegexMask, BlockMask and
        // MultiMask. Two rows holding one instance therefore edit each other's text.
        //
        // Two rows, one configuration, and a mask the configuration cannot hand out twice by
        // reference is the whole test.
        var collectionConfig = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item.AddField(x => x.ProductName, field => field
                    .WithLabel("Value")
                    .WithMask(() => new RegexMask("^[0-9]{0,4}$")))))
            .Build();

        // Act
        var render = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, new OrderModel { Items = { new OrderItem(), new OrderItem() } })
            .Add(p => p.Configuration, collectionConfig));

        // Assert
        var masks = render.FindComponents<MudTextField<string>>()
            .Select(c => c.Instance.Mask)
            .ToList();

        masks.Count.ShouldBe(2);
        masks[0].ShouldNotBeNull();
        masks[1].ShouldNotBeNull();
        masks[0].ShouldNotBeSameAs(masks[1]);
        masks[0].ShouldBeOfType<RegexMask>();
        masks[1].ShouldBeOfType<RegexMask>();
    }

    [Theory]
    [InlineData("0000-0000", "12345678", "1234-5678")]
    [InlineData("(000) 000-0000", "5551234567", "(555) 123-4567")]
    [InlineData("aaa-000", "abc123", "abc-123")]
    public void A_Configured_Mask_Should_Conform_Typed_Input_On_Both_Paths(
        string configured,
        string typed,
        string expected)
    {
        // Arrange - the half a parameter assertion cannot reach. The tests above prove an IMask with
        // the right pattern is handed to MudBlazor; this proves the pattern actually means what a
        // caller writing "(000) 000-0000" expects — that `0` takes a digit, `a` a letter, and every
        // other character is a literal the mask inserts. A mask bound but interpreted differently
        // would satisfy every other test in this file.
        void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Value").WithAttribute("Mask", configured);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act - drive each path's own resolved mask, rather than a locally constructed one: a test
        // that built its own PatternMask would pass even if the render paths bound something else.
        var standaloneMask = RenderForm(standaloneConfig)
            .FindComponent<MudTextField<string>>().Instance.Mask;
        var itemMask = this.RenderItemForm(NewOrder(), collectionConfig)
            .FindComponent<MudTextField<string>>().Instance.Mask;

        standaloneMask.ShouldNotBeNull();
        itemMask.ShouldNotBeNull();
        standaloneMask.Insert(typed);
        itemMask.Insert(typed);

        // Assert
        standaloneMask.Text.ShouldBe(expected);
        itemMask.Text.ShouldBe(expected);
    }

    [Fact]
    public void A_Mask_Combined_With_Lines_Should_Be_Resolved_Identically_On_Both_Paths()
    {
        // Arrange - mask plus multi-line, which unlike `.AsPassword()` + `Lines` (#207) IS honoured in
        // full: MudTextField chooses its input implementation on `Mask == null` alone, so a masked
        // field always renders a MudMask, and MudMask opens a <textarea> past one line while still
        // masking. Neither setting is dropped. What #211 must guarantee is that both render paths land
        // in the same place, which is what this asserts — the element choice AND the values behind it.
        void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Value").AsTextArea(lines: 4).WithAttribute("Mask", "0000-0000");

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = this.RenderItemForm(NewOrder(), collectionConfig);

        // Assert - same element choice, and the same resolved Lines and pattern behind it.
        itemRender.FindAll("textarea").Count.ShouldBe(standaloneRender.FindAll("textarea").Count);
        itemRender.FindAll("input").Count.ShouldBe(standaloneRender.FindAll("input").Count);

        var standalone = standaloneRender.FindComponent<MudTextField<string>>().Instance;
        var item = itemRender.FindComponent<MudTextField<string>>().Instance;

        item.Lines.ShouldBe(standalone.Lines);
        item.Mask?.Mask.ShouldBe(standalone.Mask?.Mask);
    }

    [Fact]
    public void NativeRequiredOptIn_Should_Be_Honoured_Identically_On_Both_Paths()
    {
        // Arrange - #204. `.WithNativeRequired()` (and the raw "Required" attribute it replaces) used
        // to be read ONLY by CollectionFieldComponent, so the escape hatch worked inside an item form
        // and was silently ignored outside one. This pins the EXPLICIT direction of the Required
        // comparison: the test above pins that both paths agree for a plain `.Required(...)` field
        // (`true` since #199 — it read `false` under #190), this one pins that both agree on `true`
        // when the decoration is asked for without `.Required(...)` at all. The remaining
        // combination, an explicit `false` overriding `.Required(...)`, is covered on both paths by
        // AriaRequiredTests.
        static void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Product").WithNativeRequired();

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = this.RenderItemForm(NewOrder(), collectionConfig);

        // Assert - the parameter on both, and the attribute on the element the browser sees.
        standaloneRender.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeTrue();
        itemRender.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeTrue();

        standaloneRender.Find("input").HasAttribute("required").ShouldBeTrue();
        itemRender.Find("input").HasAttribute("required").ShouldBeTrue();
    }

    [Fact]
    public void NumericCollectionItemField_Should_Honour_The_Same_Presentation_Attributes_As_A_Standalone_Field()
    {
        // Arrange - the numeric counterpart of the test above. Only the string field was ever
        // compared across the two paths, which is exactly how #191 survived #184: that fix taught
        // the collection path to forward adornments for numeric item fields, leaving the component
        // path the deficient one, so the same configuration rendered an icon inside
        // .WithItemForm(...) and nothing outside it. Comparing only strings could not see that.
        static void Configure<TOwner>(FieldBuilder<TOwner, int> field)
            where TOwner : new()
            => field
                .WithLabel("Quantity")
                .WithPlaceholder("e.g. 3")
                .WithHelpText("Units to order")
                .WithAdornment(Icons.Material.Filled.Numbers, Adornment.End, Color.Secondary)
                .WithVariant(Variant.Filled)
                // #199. Required joined the numeric compared set here; it was already in the string
                // one. Configured rather than left default so the comparison is of the interesting
                // value — two fields agreeing on `false` would pass while proving nothing.
                .Required("Quantity is required");

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Priority, Configure)
            .Build();

        var collectionConfig = NumericItemForm(Configure);

        // Act - the rendered components are kept, not just their instances, so the DOM assertions
        // below can read the element MudBlazor actually produced.
        var standaloneRender = RenderForm(standaloneConfig);
        var standalone = standaloneRender.FindComponent<MudNumericField<int>>().Instance;

        var itemRender = this.RenderItemForm(NewBasket(), collectionConfig);
        var itemField = itemRender.FindComponent<MudNumericField<int>>().Instance;

        // Assert
        Presentation(itemField).ShouldBe(Presentation(standalone));

        // Guard the guard: two all-default fields would compare equal while proving nothing.
        standalone.Adornment.ShouldBe(Adornment.End);
        standalone.AdornmentIcon.ShouldBe(Icons.Material.Filled.Numbers);
        standalone.Variant.ShouldBe(Variant.Filled);
        standalone.Required.ShouldBeFalse();

        // The accessibility attribute a screen reader reads, on both paths (#199, #263). Since #263
        // this is the assertion carrying the weight — see the longer note on the text-field parity
        // test above — because the Required parameter reads false for silence and success alike.
        standaloneRender.Find("input").GetAttribute("aria-required").ShouldBe("true");
        itemRender.Find("input").GetAttribute("aria-required")
            .ShouldBe(standaloneRender.Find("input").GetAttribute("aria-required"));
    }

    /// <summary>
    /// The numeric counterpart of the attribute set below. Kept as a separate overload rather than
    /// a shared generic because MudTextField and MudNumericField have no common base that exposes
    /// these, and the two lists are free to diverge (a numeric field has no InputType or Lines).
    /// </summary>
    private static object?[] Presentation(MudNumericField<int> field) =>
    [
        field.Label,
        field.Placeholder,
        field.HelperText,
        // Joined the numeric set in #199, matching the string one. Both paths resolve it by the
        // same rule now (explicit "Required" attribute, else IsRequired), so a change to one alone
        // fails here.
        field.Required,
        field.Variant,
        field.Margin,
        field.ShrinkLabel,
        field.Adornment,
        field.AdornmentIcon,
        field.AdornmentColor,
    ];

    /// <summary>
    /// The click selector for a rendered, clickable adornment. MudBlazor draws one as a real button.
    /// </summary>
    private const string AdornmentButton = "button.mud-input-adornment-icon-button";

    [Fact]
    public void CollectionItemField_Should_Fire_The_Adornment_Handler_Like_A_Standalone_Field()
    {
        // Arrange - the same builder call on both paths, with the one parameter that used to be
        // discarded everywhere (#192). Delegate identity is not comparable across the two paths —
        // each builds its own callback — so the parity claim here is behavioural: both invoke it,
        // and both hand it their own field's value.
        var fired = new List<string?>();

        void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field
                .WithLabel("Product")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, Color.Secondary, fired.Add);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act
        RenderForm(standaloneConfig, new TestModel { Status = "standalone" })
            .Find(AdornmentButton)
            .Click();

        this.RenderItemForm(NewOrder("in-collection"), collectionConfig)
            .Find(AdornmentButton)
            .Click();

        // Assert - one call per path, each carrying the value of the field that was clicked
        fired.ShouldBe(new[] { "standalone", "in-collection" });
    }

    [Fact]
    public void MultilineCollectionItemField_Should_Render_The_Same_Element_As_A_Standalone_One()
    {
        // Arrange - Lines is split out of the presentation test because it changes the element
        // MudBlazor renders rather than an attribute on it: past 1 line the input becomes a
        // <textarea>. That makes it the one forwarded attribute whose parity cannot be judged by
        // comparing parameters, and the reason it must not share a field with AsPassword — a
        // textarea has no `type`, so the two together would silently unmask while every parameter
        // still matched.
        static void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Notes").AsTextArea(lines: 4, maxLength: 2000);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = this.RenderItemForm(NewOrder(), collectionConfig);

        // Assert - same parameters, and the same element actually rendered
        Presentation(itemRender.FindComponent<MudTextField<string>>().Instance)
            .ShouldBe(Presentation(standaloneRender.FindComponent<MudTextField<string>>().Instance));

        standaloneRender.FindAll("textarea").Count.ShouldBe(1);
        itemRender.FindAll("textarea").Count.ShouldBe(1);
        standaloneRender.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(4);
    }

    [Fact]
    public void PasswordCollectionItemField_With_Lines_Should_Stay_Masked_Like_A_Standalone_One()
    {
        // Arrange - #207, the counterpart to the test above. `.AsPassword()` plus a multi-line
        // setting used to render the credential in clear text on BOTH paths: past Lines > 1
        // MudBlazor emits a <textarea>, which carries no `type` attribute, so the masking was
        // dropped without a word. It was never a drift between the two paths — it was a shared gap,
        // which is why the parity comparison could not have caught it and this case is asserted on
        // the rendered element rather than on parameters.
        //
        // A masked textarea does not exist, so the combination can never be honoured as written:
        // masking wins and the field renders on one line. Both paths must agree on that, and on the
        // fact that they collapse it to the same single line rather than one of them keeping four.
        static void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field
                .WithLabel("Secret")
                .AsPassword(enableVisibilityToggle: false)
                .AsTextArea(lines: 4);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        // Act
        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = this.RenderItemForm(NewOrder(), collectionConfig);

        // Assert - neither path renders a textarea, both mask, and both agree on the line count.
        standaloneRender.FindAll("textarea").ShouldBeEmpty();
        itemRender.FindAll("textarea").ShouldBeEmpty();

        standaloneRender.Find("input").GetAttribute("type").ShouldBe("password");
        itemRender.Find("input").GetAttribute("type").ShouldBe("password");

        itemRender.FindComponent<MudTextField<string>>().Instance.Lines
            .ShouldBe(standaloneRender.FindComponent<MudTextField<string>>().Instance.Lines);
        standaloneRender.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(1);
    }

    [Fact]
    public void PasswordToggleCollectionItemField_Should_Offer_The_Same_Toggle_As_A_Standalone_Field()
    {
        // Arrange - #203, and the last entry to leave the divergence list below.
        //
        // `.AsPassword()`'s visibility toggle used to be component-path only: a standalone field got
        // an eye icon that revealed the value, and the identical field inside `.WithItemForm(...)`
        // got nothing, because the collection path was a separate renderer that had never been
        // taught the feature. Nobody had to implement it for item fields — converging the two paths
        // onto one component is what made it appear on both, which is the argument for the refactor.
        //
        // Asserted on its own field rather than folded into the big comparison above, because the
        // toggle claims the single adornment slot and would displace that field's start adornment.
        static void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field.WithLabel("Secret").AsPassword(enableVisibilityToggle: true);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, Configure)
            .Build();

        var collectionConfig = TextItemForm(Configure);

        var standaloneRender = RenderForm(standaloneConfig);
        var itemRender = this.RenderItemForm(NewOrder(), collectionConfig);

        // Assert - the toggle affordance itself is identical...
        Presentation(itemRender.FindComponent<MudTextField<string>>().Instance)
            .ShouldBe(Presentation(standaloneRender.FindComponent<MudTextField<string>>().Instance));

        // Guard the guard: two fields with no adornment at all would compare equal for free.
        var standalone = standaloneRender.FindComponent<MudTextField<string>>().Instance;
        standalone.Adornment.ShouldBe(Adornment.End);
        standalone.AdornmentIcon.ShouldBe(Icons.Material.Filled.Visibility);

        // ...both start masked...
        standaloneRender.Find("input").GetAttribute("type").ShouldBe("password");
        itemRender.Find("input").GetAttribute("type").ShouldBe("password");

        // Act - and the affordance is not merely drawn on both, it WORKS on both. A rendered eye
        // that does nothing would satisfy every parameter comparison above.
        standaloneRender.Find(AdornmentButton).Click();
        itemRender.Find(AdornmentButton).Click();

        // Assert
        standaloneRender.Find("input").GetAttribute("type").ShouldBe("text");
        itemRender.Find("input").GetAttribute("type").ShouldBe("text");
    }

    /// <summary>
    /// The presentation attributes both render paths are expected to honour identically, and which
    /// the test above actually configures. Add to this list whenever a field component gains one.
    /// <para>
    /// <c>Required</c> joined the compared set in #190, which pinned both paths to <c>false</c> even
    /// for a <c>.Required(...)</c> field. #199 reversed the VALUE — both now render <c>true</c>, so a
    /// required field is announced to assistive technology (WCAG 2.1 3.3.2, Level A) — while keeping
    /// the agreement that made #190 worth filing. What mattered then and still matters is that the
    /// two agree; before #190 the attribute was listed as a known divergence while the test never
    /// configured it, so the comparison passed vacuously over a real disagreement. That is the trap
    /// the list below warns about.
    /// </para>
    /// <para>
    /// <b>The divergence list is empty.</b> It used to name the attributes the two paths were known
    /// to disagree on, and it grew: <c>Required</c>, <c>InputType</c>, <c>Lines</c>,
    /// <c>MaxLength</c>, <c>Autocomplete</c>, <c>OnAdornmentClick</c>, <c>EnablePasswordToggle</c>,
    /// each entered by a bug report and left by its own fix. #203 removed the second render path
    /// altogether — collection item fields go through <c>IFieldRendererService</c> and the same
    /// per-type components as everything else — so there is no longer a mechanism by which a
    /// presentation attribute CAN diverge, and nothing left to list.
    /// </para>
    /// <para>
    /// What survives is not a divergence but a shared gap, and one measurement note:
    /// </para>
    /// <list type="bullet">
    /// <item>✅ <b>Resolved in #204, generalised by #199</b> — the native-required opt-in used to be
    /// collection-path only: <c>.WithAttribute("Required", true)</c> was read solely by
    /// <c>CollectionFieldComponent</c>, so it was honoured inside an item form and silently ignored
    /// outside one. Both placements read it (via the typed <c>.WithNativeRequired()</c>), and
    /// <c>Required</c> is compared in <c>Presentation()</c> below — in the string overload since
    /// #204 and in the numeric one since #199. Since #199 <c>.Required(...)</c> alone renders
    /// <c>true</c>, so the field is announced to assistive technology; #190's <c>false</c>-on-both
    /// invariant was the same agreement one value lower, and reversing it is what that issue asked
    /// for.</item>
    /// <item>✅ <b>Resolved in #203</b> — <c>EnablePasswordToggle</c>. Not by implementing it for
    /// item fields, but by deleting the renderer that lacked it; covered by
    /// <see cref="PasswordToggleCollectionItemField_Should_Offer_The_Same_Toggle_As_A_Standalone_Field"/>,
    /// which asserts the toggle both renders AND works in either placement.</item>
    /// <item>✅ <b>Resolved in #211</b> — <c>Mask</c> used to render on neither path: FormCraft stores
    /// it as a string, MudBlazor's parameter wants an <c>IMask</c>, and the component path's
    /// <c>GetMask()</c> was an unimplemented stub that nothing called. It is resolved through
    /// <c>TextMaskMap</c> and compared in <c>Presentation()</c> below. It was the <i>last</i> entry
    /// on this list, and it left by being implemented rather than by the #203 deletion — the two
    /// ways an entry can go.</item>
    /// </list>
    /// <para>
    /// <c>InputType</c>, <c>Lines</c>, <c>MaxLength</c> and <c>Autocomplete</c> moved out of that
    /// list and into the compared set in #189 — before it, a <c>.AsPassword()</c> item field
    /// rendered its characters in clear text inside a collection.
    /// </para>
    /// <para>
    /// <c>OnAdornmentClick</c> stays out of the compared set for a reason that is now purely
    /// mechanical rather than a divergence: an <c>EventCallback</c> wraps a delegate built per
    /// component instance, so comparing two of them compares two different objects and proves
    /// nothing. Behaviour is asserted instead by
    /// <see cref="CollectionItemField_Should_Fire_The_Adornment_Handler_Like_A_Standalone_Field"/>,
    /// which checks each placement actually invokes the configured handler with its own value.
    /// </para>
    /// <para>
    /// Historically this was the subtlest disagreement of the set, and worth keeping on record. The
    /// handler-less case diverged in MARKUP but not in behaviour: the component path bound
    /// <c>OnAdornmentClick</c> unconditionally, so an ordinary field's decorative adornment was
    /// always a focusable <c>&lt;button&gt;</c> — inert, but in the tab order — while the collection
    /// path emitted an empty callback and MudBlazor drew a plain icon. An accessibility defect that
    /// no parameter comparison could see, closed in #216 by making the component path's binding
    /// conditional too, and unable to reopen since #203 left only one binding. Both placements are
    /// still pinned by their own tests
    /// (<c>TextField_Adornment_Without_A_Handler_Should_Render_A_Plain_Icon</c> and
    /// <c>ItemField_Adornment_Without_A_Handler_Should_Stay_Inert</c>).
    /// </para>
    /// </summary>
    private static object?[] Presentation(MudTextField<string> field) =>
    [
        field.Label,
        field.Placeholder,
        field.HelperText,
        field.Required,
        field.Variant,
        field.Margin,
        field.ShrinkLabel,
        field.Adornment,
        field.AdornmentIcon,
        field.AdornmentColor,
        field.InputType,
        field.Lines,
        field.MaxLength,
        // The pattern, not the IMask: each path resolves its own instance (they must — a mask carries
        // the live caret and text of the input it is attached to), so comparing the objects would
        // report a divergence for two identically-configured fields.
        field.Mask?.Mask,
        // MudTextField has no Autocomplete parameter; both paths emit a raw lowercase HTML
        // attribute, so the unmatched-attribute bag is where the comparison has to read it.
        field.UserAttributes.GetValueOrDefault("autocomplete"),
    ];

    [Fact]
    public void CustomTemplate_Should_Take_Precedence_Over_Options()
    {
        // Arrange - custom templates beat every built-in renderer, including selects
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Status, field => field
                .WithLabel("Status")
                .WithOptions(("a", "A"), ("b", "B"))
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "my-template");
                    builder.AddContent(2, context.Value);
                    builder.CloseElement();
                }))
            .Build();

        // Act
        var component = RenderForm(config, new TestModel { Status = "a" });

        // Assert
        component.Find(".my-template").TextContent.ShouldBe("a");
        component.FindComponents<MudSelect<string>>().ShouldBeEmpty();
    }

    private class TestModel
    {
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public int? Rating { get; set; }
        public bool IsActive { get; set; }
        public DateTime BirthDate { get; set; }
        public IEnumerable<string>? Categories { get; set; }
    }

    private class CustomOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
