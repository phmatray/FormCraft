namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests for the configurable MudBlazor Variant (#146): the .WithVariant(...) field
/// extension, the form-level FormCraftComponent.DefaultVariant parameter, and the
/// precedence between them.
/// </summary>
public class VariantConfigurationTests : MudBlazorTestBase
{
    /// <summary>
    /// Renders the form next to a MudPopoverProvider, which picker components require.
    /// </summary>
    private IRenderedComponent<FormCraftComponent<TestModel>> RenderForm(
        TestModel model, IFormConfiguration<TestModel> config, Variant? defaultVariant = null)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<FormCraftComponent<TestModel>>(1);
            builder.AddComponentParameter(2, "Model", model);
            builder.AddComponentParameter(3, "Configuration", config);
            if (defaultVariant is { } variant)
            {
                builder.AddComponentParameter(4, "DefaultVariant", variant);
            }
            builder.CloseComponent();
        });

        return cut.FindComponent<FormCraftComponent<TestModel>>();
    }

    [Fact]
    public void TextField_Should_Default_To_Outlined_Variant()
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

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Variant.ShouldBe(Variant.Outlined);
    }

    [Fact]
    public void WithVariant_Should_Apply_To_TextField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithVariant(Variant.Filled))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Variant.ShouldBe(Variant.Filled);
    }

    [Fact]
    public void WithVariant_Should_Apply_To_NumericField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Age, field => field
                .WithLabel("Age")
                .WithVariant(Variant.Filled))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudNumericField = component.FindComponent<MudNumericField<int>>();
        mudNumericField.Instance.Variant.ShouldBe(Variant.Filled);
    }

    [Fact]
    public void WithVariant_Should_Apply_To_SelectField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Country, field => field
                .WithLabel("Country")
                .WithOptions(("US", "United States"), ("BE", "Belgium"))
                .WithVariant(Variant.Text))
            .Build();

        // Act
        var component = RenderForm(model, config);

        // Assert
        var mudSelect = component.FindComponent<MudSelect<string>>();
        mudSelect.Instance.Variant.ShouldBe(Variant.Text);
    }

    [Fact]
    public void WithVariant_Should_Apply_To_DateTimeField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.BirthDate, field => field
                .WithLabel("Birth Date")
                .WithVariant(Variant.Filled))
            .Build();

        // Act
        var component = RenderForm(model, config);

        // Assert
        var mudDatePicker = component.FindComponent<MudDatePicker>();
        mudDatePicker.Instance.Variant.ShouldBe(Variant.Filled);
    }

    [Fact]
    public void DefaultVariant_Should_Apply_To_Fields_Without_Explicit_Variant()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .AddField(x => x.Age, field => field.WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultVariant, Variant.Filled));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Variant.ShouldBe(Variant.Filled);
        component.FindComponent<MudNumericField<int>>().Instance.Variant.ShouldBe(Variant.Filled);
    }

    [Fact]
    public void FieldLevel_WithVariant_Should_Override_FormLevel_DefaultVariant()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithVariant(Variant.Text))
            .AddField(x => x.Age, field => field.WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultVariant, Variant.Filled));

        // Assert - explicit field variant wins; sibling falls back to the form default
        component.FindComponent<MudTextField<string>>().Instance.Variant.ShouldBe(Variant.Text);
        component.FindComponent<MudNumericField<int>>().Instance.Variant.ShouldBe(Variant.Filled);
    }

    [Fact]
    public void DefaultVariant_Should_Apply_To_DatePicker_And_Select()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.BirthDate, field => field.WithLabel("Birth Date"))
            .AddField(x => x.Country, field => field
                .WithLabel("Country")
                .WithOptions(("US", "United States"), ("BE", "Belgium")))
            .Build();

        // Act
        var component = RenderForm(model, config, Variant.Text);

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Variant.ShouldBe(Variant.Text);
        component.FindComponent<MudSelect<string>>().Instance.Variant.ShouldBe(Variant.Text);
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public int? OptionalAge { get; set; }
        public string Country { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
    }
}
