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
        static void Configure<TOwner>(FieldBuilder<TOwner, string> field)
            where TOwner : new()
            => field
                .WithLabel("Product")
                .WithPlaceholder("e.g. Widget")
                .WithHelpText("The catalogue name")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, Color.Secondary)
                .WithVariant(Variant.Filled);

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
        var standalone = RenderForm(standaloneConfig)
            .FindComponent<MudTextField<string>>().Instance;

        var itemField = Render<FormCraftComponent<OrderModel>>(parameters => parameters
                .Add(p => p.Model, new OrderModel { Items = { new OrderItem() } })
                .Add(p => p.Configuration, collectionConfig))
            .FindComponent<MudTextField<string>>().Instance;

        // Assert - compared as one set, so a newly-honoured attribute on the component path that
        // the collection path ignores shows up here rather than in a bug report
        Presentation(itemField).ShouldBe(Presentation(standalone));

        // Guard the guard: a comparison of two all-default fields would pass while proving nothing.
        standalone.Adornment.ShouldBe(Adornment.Start);
        standalone.Variant.ShouldBe(Variant.Filled);
    }

    /// <summary>
    /// The presentation attributes both render paths are expected to honour identically, and which
    /// the test above actually configures. Add to this list whenever a field component gains one.
    /// <para>
    /// Deliberately NOT compared, because the two paths are known to disagree today — each is
    /// tracked separately, and listing one here without configuring it would assert nothing while
    /// looking like coverage:
    /// </para>
    /// <list type="bullet">
    /// <item><c>Required</c> — the collection path emits it, no component-path renderer does.</item>
    /// <item><c>InputType</c>, <c>Lines</c>, <c>MaxLength</c>, <c>Autocomplete</c> — component path
    /// only; a <c>.AsPassword()</c> item field still renders as plain text inside a collection.</item>
    /// <item><c>OnAdornmentClick</c> — component path only (an explicit non-goal of #184).</item>
    /// </list>
    /// </summary>
    private static object?[] Presentation(MudTextField<string> field) =>
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

    private class OrderModel
    {
        public List<OrderItem> Items { get; set; } = new();
    }

    private class OrderItem
    {
        public string ProductName { get; set; } = string.Empty;
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
