using System.Globalization;

namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Parity tests for the consolidated render pipeline (#148): behaviors that used
/// to live only in FormCraftComponent's legacy type-switch (Options-driven selects,
/// MinDate/MaxDate passthrough, invariant Culture on numeric fields, checkbox
/// rendering for booleans, Variant/Margin/ShrinkLabel/Immediate settings) must be
/// produced identically by the FieldRendererService components.
/// </summary>
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
        // fails here. It is not a claim that the paths agree on everything: see Presentation()
        // for the attributes still known to diverge.
        // One field carrying every compared attribute at once, so a single comparison covers the
        // whole set. Two deliberate exclusions:
        //
        // - AsPassword's visibility toggle is off: it is component-path-only and would replace the
        //   start adornment below with its own eye icon, failing this test for a reason it does not
        //   exist to test.
        // - Lines stays at its default 1, and is covered by the multiline test below instead.
        //   MudBlazor renders a <textarea> once Lines > 1, and a textarea has no `type` attribute —
        //   so combining Lines with AsPassword would make InputType inert and this test's headline
        //   masking assertion vacuous: it would compare a parameter that changes nothing.
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
                .WithAutocomplete("current-password")
                .Required("Product name is required");

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
        var standalone = standaloneRender.FindComponent<MudTextField<string>>().Instance;

        var itemRender = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, new OrderModel { Items = { new OrderItem() } })
            .Add(p => p.Configuration, collectionConfig));
        var itemField = itemRender.FindComponent<MudTextField<string>>().Instance;

        // Assert - compared as one set, so a newly-honoured attribute on the component path that
        // the collection path ignores shows up here rather than in a bug report
        Presentation(itemField).ShouldBe(Presentation(standalone));

        // Guard the guard: a comparison of two all-default fields would pass while proving nothing.
        standalone.Adornment.ShouldBe(Adornment.Start);
        standalone.Variant.ShouldBe(Variant.Filled);
        standalone.InputType.ShouldBe(InputType.Password);
        standalone.MaxLength.ShouldBe(500);
        standalone.UserAttributes.GetValueOrDefault("autocomplete").ShouldBe("current-password");

        // And guard against the subtler failure the parameter comparison cannot see: a value that
        // is forwarded but has no effect on the rendered element. `type="password"` is what masks
        // the characters, so assert the DOM, on both paths.
        standaloneRender.Find("input").GetAttribute("type").ShouldBe("password");
        itemRender.Find("input").GetAttribute("type").ShouldBe("password");

        // Required is the one compared attribute whose agreed value IS the default (#190): both
        // paths must render false for a .Required(...) field, because validation here is
        // server-side. So unlike the others it cannot be guarded by asserting a non-default —
        // its bite comes from the collection path, which used to render true off field.IsRequired
        // and would diverge from this standalone false the moment that emission came back.
        standalone.Required.ShouldBeFalse();
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
                .WithVariant(Variant.Filled);

        var standaloneConfig = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Priority, Configure)
            .Build();

        var collectionConfig = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item.AddField(x => x.Quantity, Configure)))
            .Build();

        // Act
        var standalone = RenderForm(standaloneConfig)
            .FindComponent<MudNumericField<int>>().Instance;

        var itemField = Render<FormCraftComponent<OrderModel>>(parameters => parameters
                .Add(p => p.Model, new OrderModel { Items = { new OrderItem() } })
                .Add(p => p.Configuration, collectionConfig))
            .FindComponent<MudNumericField<int>>().Instance;

        // Assert
        Presentation(itemField).ShouldBe(Presentation(standalone));

        // Guard the guard: two all-default fields would compare equal while proving nothing.
        standalone.Adornment.ShouldBe(Adornment.End);
        standalone.AdornmentIcon.ShouldBe(Icons.Material.Filled.Numbers);
        standalone.Variant.ShouldBe(Variant.Filled);
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

        var collectionConfig = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item.AddField(x => x.ProductName, Configure)))
            .Build();

        // Act
        RenderForm(standaloneConfig, new TestModel { Status = "standalone" })
            .Find(AdornmentButton)
            .Click();

        Render<FormCraftComponent<OrderModel>>(parameters => parameters
                .Add(p => p.Model, new OrderModel { Items = { new OrderItem { ProductName = "in-collection" } } })
                .Add(p => p.Configuration, collectionConfig))
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

        // Assert - same parameters, and the same element actually rendered
        Presentation(itemRender.FindComponent<MudTextField<string>>().Instance)
            .ShouldBe(Presentation(standaloneRender.FindComponent<MudTextField<string>>().Instance));

        standaloneRender.FindAll("textarea").Count.ShouldBe(1);
        itemRender.FindAll("textarea").Count.ShouldBe(1);
        standaloneRender.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(4);
    }

    /// <summary>
    /// The presentation attributes both render paths are expected to honour identically, and which
    /// the test above actually configures. Add to this list whenever a field component gains one.
    /// <para>
    /// <c>Required</c> joined the set in #190: both paths must render it <c>false</c> even for a
    /// <c>.Required(...)</c> field, because validation is server-side and forms render
    /// <c>novalidate</c>. It was previously listed as a known divergence while the test never
    /// configured it — so the comparison passed vacuously over a real disagreement, which is the
    /// trap the list below warns about.
    /// </para>
    /// <para>
    /// Deliberately NOT compared, because the two paths are known to disagree today — each is
    /// tracked separately, and listing one here without configuring it would assert nothing while
    /// looking like coverage:
    /// </para>
    /// <list type="bullet">
    /// <item>The raw <c>"Required"</c> attribute — collection path only. <c>.Required(...)</c> is
    /// compared above and agrees, but <c>.WithAttribute("Required", true)</c> is read solely by
    /// <c>CollectionFieldComponent</c>; no component-path renderer looks for that key, so the
    /// opt-in is honoured inside an item form and ignored outside one.</item>
    /// <item><c>EnablePasswordToggle</c> — component path only: <c>.AsPassword()</c> puts a
    /// visibility eye on a standalone field and nothing on an item field. The masking itself is
    /// compared (see <c>InputType</c> below); only the toggle affordance diverges.</item>
    /// <item><c>Mask</c> — neither path renders one. FormCraft stores it as a string, MudBlazor's
    /// parameter wants an <c>IMask</c>, and the component path's <c>GetMask()</c> is an
    /// unimplemented stub that nothing calls. Listed so it is not mistaken for coverage.</item>
    /// </list>
    /// <para>
    /// <c>InputType</c>, <c>Lines</c>, <c>MaxLength</c> and <c>Autocomplete</c> moved out of that
    /// list and into the compared set in #189 — before it, a <c>.AsPassword()</c> item field
    /// rendered its characters in clear text inside a collection.
    /// </para>
    /// <para>
    /// <c>OnAdornmentClick</c> is honoured by both paths since #192, but is absent from the list
    /// below on purpose: the two paths build their callbacks separately, so comparing the values
    /// would compare two different delegates and prove nothing. It is covered by
    /// <see cref="CollectionItemField_Should_Fire_The_Adornment_Handler_Like_A_Standalone_Field"/>,
    /// which asserts each path actually invokes the configured handler.
    /// </para>
    /// <para>
    /// That parity holds when a handler IS configured. Without one the paths still differ in
    /// MARKUP, though not in behaviour: <c>MudBlazorTextFieldComponent.razor</c> binds
    /// <c>OnAdornmentClick</c> unconditionally, so an ordinary field's adornment is always a
    /// focusable <c>&lt;button&gt;</c> — inert, but in the tab order — while the collection path
    /// emits an empty callback and MudBlazor draws a plain icon. Predates #192 on the component
    /// side and is left alone rather than change an unrelated render path; both are click-inert,
    /// which is what the two "…Without_A_Handler_Should_Stay_Inert" tests pin down.
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
        // MudTextField has no Autocomplete parameter; both paths emit a raw lowercase HTML
        // attribute, so the unmatched-attribute bag is where the comparison has to read it.
        field.UserAttributes.GetValueOrDefault("autocomplete"),
    ];

    private class OrderModel
    {
        public List<OrderItem> Items { get; set; } = new();
    }

    private class OrderItem
    {
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }

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
