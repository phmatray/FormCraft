namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests for the configurable MudBlazor ShrinkLabel (#177): the .WithShrinkLabel(...)
/// field extension, the form-level FormCraftComponent.DefaultShrinkLabel parameter, and
/// the precedence between them. Follow-up to the configurable Variant (#146) — with
/// Variant.Text a permanently shrunk label has nothing to anchor to, so consumers need
/// to be able to let it float.
/// </summary>
public class ShrinkLabelConfigurationTests : MudBlazorTestBase
{
    [Fact]
    public void TextField_Should_Default_To_ShrinkLabel_True()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - unchanged from v3.1.0: the label stays pinned unless asked otherwise
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    [Fact]
    public void WithShrinkLabel_False_Should_Apply_To_TextField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithShrinkLabel(false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void WithShrinkLabel_Should_Default_Its_Argument_To_True()
    {
        // Arrange - the parameterless call mirrors AsPassword(enableVisibilityToggle: true)
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithShrinkLabel())
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    [Fact]
    public void DefaultShrinkLabel_False_Should_Apply_To_Fields_Without_Explicit_Setting()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultShrinkLabel, false));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void FieldLevel_False_Should_Override_FormLevel_True()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithShrinkLabel(false))
            .Build();

        // Act - form says true (the default), field says false
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultShrinkLabel, true));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void FieldLevel_True_Should_Override_FormLevel_False()
    {
        // Arrange - the other direction, which a null-coalescing bug would silently break
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithShrinkLabel(true))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultShrinkLabel, false));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    [Fact]
    public void DefaultShrinkLabel_Should_Default_To_True()
    {
        // Arrange - a form that never mentions ShrinkLabel keeps the v3.1.0 rendering
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Instance.DefaultShrinkLabel.ShouldBeTrue();
    }

    /// <summary>
    /// Renders the form next to a MudPopoverProvider, which the picker components require.
    /// </summary>
    private IRenderedComponent<FormCraftComponent<TestModel>> RenderForm(
        IFormConfiguration<TestModel> config, bool? defaultShrinkLabel = null)
    {
        var model = new TestModel();
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<FormCraftComponent<TestModel>>(1);
            builder.AddComponentParameter(2, "Model", model);
            builder.AddComponentParameter(3, "Configuration", config);
            if (defaultShrinkLabel is { } shrink)
            {
                builder.AddComponentParameter(4, "DefaultShrinkLabel", shrink);
            }
            builder.CloseComponent();
        });

        return cut.FindComponent<FormCraftComponent<TestModel>>();
    }

    // Each component gets a Theory rather than one Fact asserting both states: a bUnit
    // context accepts only ONE MudPopoverProvider, so two renders in a single test throw
    // "already a subscriber to ... 'mud-overlay-to-popover-provider'". One render per test.

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void NumericField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Age, f => ApplyShrinkLabel(f.WithLabel("Age"), fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudNumericField<int>>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void NullableNumericField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.OptionalAge, f => ApplyShrinkLabel(f.WithLabel("Optional Age"), fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudNumericField<int?>>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void SelectField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Country, f => ApplyShrinkLabel(
                f.WithLabel("Country").WithOptions(("US", "United States"), ("BE", "Belgium")),
                fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudSelect<string>>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void MultiSelectField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Categories, f => ApplyShrinkLabel(
                f.WithLabel("Categories").AsMultiSelect(("tech", "Technology"), ("health", "Healthcare")),
                fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudSelect<string>>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void DateTimeField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.BirthDate, f => ApplyShrinkLabel(f.WithLabel("Birth Date"), fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudDatePicker>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void DateOnlyField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.StartDate, f => ApplyShrinkLabel(f.WithLabel("Start Date"), fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudDatePicker>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void TimeOnlyField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.StartTime, f => ApplyShrinkLabel(f.WithLabel("Start Time"), fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudTimePicker>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void AutocompleteField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, f => ApplyShrinkLabel(
                f.WithLabel("City").AsAutocomplete(SearchCitiesAsync), fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudAutocomplete<string>>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void LookupField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        RenderForm(BuildLookupConfig(fieldLevel)).FindComponent<MudTextField<string>>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void ColorPickerField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Color, f => ApplyShrinkLabel(
                f.WithLabel("Color").AsColorPicker(), fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudColorPicker>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void LovField_Should_Honor_ShrinkLabel(bool? fieldLevel, bool expected)
    {
        // The LOV field was the one variant-aware component that never passed ShrinkLabel at
        // all; it now does, for consistency with its 11 siblings. This asserts the value
        // reaching the component only. It does NOT imply a visible change: the LOV input
        // always supplies a "Click to select..." placeholder, and MudBlazor's placeholder
        // rule pins the label on its own — see ShrinkLabelRenderingTests, which measures the
        // rendered outcome and shows it is identical with and without this setting.
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, f => ApplyShrinkLabel(
                f.WithLabel("City").AsLov<TestModel, int, CityDto>(lov => lov
                    .WithDataSource(() => new[] { new CityDto { Id = 1, Name = "Paris" } })
                    .WithKey(c => c.Id)
                    // Cast required: WithDisplay has both Expression<Func<>> and Func<>
                    // overloads, so a bare lambda is ambiguous (CS0121).
                    .WithDisplay((Expression<Func<CityDto, string>>)(c => c.Name))),
                fieldLevel))
            .Build();

        RenderForm(config).FindComponent<MudTextField<string>>()
            .Instance.ShrinkLabel.ShouldBe(expected);
    }

    /// <summary>
    /// Applies <c>.WithShrinkLabel(value)</c> only when the case supplies one, so the
    /// "null" case exercises the genuinely-unconfigured path rather than an explicit true.
    /// </summary>
    private static void ApplyShrinkLabel<TValue>(FieldBuilder<TestModel, TValue> builder, bool? value)
    {
        if (value is { } shrinkLabel)
        {
            builder.WithShrinkLabel(shrinkLabel);
        }
    }

    [Fact]
    public void DefaultShrinkLabel_False_Should_Reach_Pickers_And_Selects()
    {
        // Arrange - the form-level default must cross the cascade into every component,
        // not just the ones with a field-level attribute.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.BirthDate, f => f.WithLabel("Birth Date"))
            .AddField(x => x.Country, f => f
                .WithLabel("Country")
                .WithOptions(("US", "United States")))
            .Build();

        // Act
        var component = RenderForm(config, defaultShrinkLabel: false);

        // Assert
        component.FindComponent<MudDatePicker>().Instance.ShrinkLabel.ShouldBeFalse();
        component.FindComponent<MudSelect<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    private static Task<IEnumerable<SelectOption<string>>> SearchCitiesAsync(
        string text, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<SelectOption<string>>>(
            [new SelectOption<string> { Value = "Paris", Label = "Paris" }]);

    private static IFormConfiguration<TestModel> BuildLookupConfig(bool? shrinkLabel)
        => FormBuilder<TestModel>
            .Create()
            .AddField(x => x.CityId, f => ApplyShrinkLabel(
                f.WithLabel("City")
                    .AsLookup<TestModel, int, CityDto>(
                        dataProvider: _ => Task.FromResult(new LookupResult<CityDto>
                        {
                            Items = [new CityDto { Id = 1, Name = "Paris" }],
                            TotalCount = 1
                        }),
                        valueSelector: c => c.Id,
                        displaySelector: c => c.Name),
                shrinkLabel))
            .Build();

    private class CityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public int? OptionalAge { get; set; }
        public string Country { get; set; } = string.Empty;
        public IEnumerable<string> Categories { get; set; } = [];
        public DateTime BirthDate { get; set; }
        public DateOnly StartDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public string City { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string Color { get; set; } = string.Empty;
    }
}
