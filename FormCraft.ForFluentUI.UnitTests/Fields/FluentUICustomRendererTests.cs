namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// The three custom renderers - slider, rating and colour picker - used via
/// <c>.WithCustomRenderer(...)</c> rather than registered as <c>IFieldRenderer</c> (#278).
/// </summary>
public class FluentUICustomRendererTests : FluentUITestBase
{
    [Fact]
    public void Slider_Renderer_Should_Render_A_Fluent_Slider()
    {
        // Act
        var component = RenderSlider();

        // Assert
        component.FindComponents<FluentSlider<double>>().ShouldNotBeEmpty();
    }

    [Fact]
    public void Slider_Renderer_Should_Honour_Min_Max_And_Step()
    {
        // Act
        var component = RenderSlider(f => f
            .WithLabel("Volume")
            .WithAttribute("Min", 10.0)
            .WithAttribute("Max", 50.0)
            .WithAttribute("Step", 5.0)
            .WithCustomRenderer(typeof(FluentUISliderRenderer)));

        // Assert
        var slider = component.FindComponent<FluentSlider<double>>().Instance;
        slider.Min.ShouldBe(10.0);
        slider.Max.ShouldBe(50.0);
        slider.Step.ShouldBe(5.0);
    }

    [Fact]
    public void Slider_Renderer_Should_Default_Its_Range_When_Unconfigured()
    {
        // Act
        var component = RenderSlider();

        // Assert
        var slider = component.FindComponent<FluentSlider<double>>().Instance;
        slider.Min.ShouldBe(0.0);
        slider.Max.ShouldBe(100.0);
    }

    [Fact]
    public void Rating_Renderer_Should_Render_One_Control_Per_Point()
    {
        // Act
        var component = RenderRating();

        // Assert - the default maximum is 5
        component.FindAll("[data-testid=formcraft-rating-star]").Count.ShouldBe(5);
    }

    [Fact]
    public void Rating_Renderer_Should_Honour_A_Configured_Maximum()
    {
        // Act
        var component = RenderRating(f => f
            .WithLabel("Score")
            .WithAttribute("MaxValue", 3)
            .WithCustomRenderer(typeof(FluentUIRatingRenderer)));

        // Assert
        component.FindAll("[data-testid=formcraft-rating-star]").Count.ShouldBe(3);
    }

    [Fact]
    public async Task Rating_Renderer_Should_Write_The_Chosen_Score_To_The_Model()
    {
        // Arrange
        var model = new PreferencesModel();
        var component = RenderRating(model: model);

        // Act - click the fourth star
        await component.FindAll("[data-testid=formcraft-rating-star]")[3].ClickAsync(new());

        // Assert
        model.Score.ShouldBe(4);
    }

    [Fact]
    public void Rating_Renderer_Should_Expose_Each_Point_To_Assistive_Technology()
    {
        // Arrange - a star that is only a glyph is unusable without sight
        var component = RenderRating();

        // Act
        var stars = component.FindAll("[data-testid=formcraft-rating-star]");

        // Assert
        stars.Select(s => s.GetAttribute("aria-label")).ShouldBe(
        [
            "Rate 1 out of 5", "Rate 2 out of 5", "Rate 3 out of 5", "Rate 4 out of 5", "Rate 5 out of 5",
        ]);
    }

    [Fact]
    public void Rating_Renderer_Should_Mark_The_Current_Score_As_Pressed()
    {
        // Arrange
        var component = RenderRating(model: new PreferencesModel { Score = 2 });

        // Act
        var stars = component.FindAll("[data-testid=formcraft-rating-star]");

        // Assert - the first two are set, the rest are not
        stars.Select(s => s.GetAttribute("aria-pressed")).ShouldBe(
            ["true", "true", "false", "false", "false"]);
    }

    [Fact]
    public void ColorPicker_Renderer_Should_Render_A_Fluent_Colour_Input()
    {
        // Act
        var component = RenderColorPicker();

        // Assert
        component.FindComponents<FluentColorPickerInput>().ShouldNotBeEmpty();
    }

    [Fact]
    public void ColorPicker_Renderer_Should_Show_The_Models_Current_Colour()
    {
        // Act
        var component = RenderColorPicker(new PreferencesModel { Colour = "#ff0000" });

        // Assert
        component.FindComponent<FluentColorPickerInput>().Instance.Value.ShouldBe("#ff0000");
    }

    private IRenderedComponent<FormCraftComponent<PreferencesModel>> RenderSlider(
        Action<FieldBuilder<PreferencesModel, double>>? configure = null)
    {
        configure ??= f => f.WithLabel("Volume").WithCustomRenderer(typeof(FluentUISliderRenderer));

        var config = FormBuilder<PreferencesModel>.Create()
            .AddField(x => x.Volume, configure)
            .Build();

        return Render<FormCraftComponent<PreferencesModel>>(p => p
            .Add(c => c.Model, new PreferencesModel())
            .Add(c => c.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<PreferencesModel>> RenderRating(
        Action<FieldBuilder<PreferencesModel, int>>? configure = null,
        PreferencesModel? model = null)
    {
        configure ??= f => f.WithLabel("Score").WithCustomRenderer(typeof(FluentUIRatingRenderer));

        var config = FormBuilder<PreferencesModel>.Create()
            .AddField(x => x.Score, configure)
            .Build();

        return Render<FormCraftComponent<PreferencesModel>>(p => p
            .Add(c => c.Model, model ?? new PreferencesModel())
            .Add(c => c.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<PreferencesModel>> RenderColorPicker(
        PreferencesModel? model = null)
    {
        var config = FormBuilder<PreferencesModel>.Create()
            .AddField(x => x.Colour, f => f
                .WithLabel("Colour")
                .WithCustomRenderer(typeof(FluentUIColorPickerRenderer)))
            .Build();

        return Render<FormCraftComponent<PreferencesModel>>(p => p
            .Add(c => c.Model, model ?? new PreferencesModel())
            .Add(c => c.Configuration, config));
    }

    /// <summary>Model exercising the three custom renderers.</summary>
    public class PreferencesModel
    {
        /// <summary>Rendered by the slider.</summary>
        public double Volume { get; set; }

        /// <summary>Rendered by the rating.</summary>
        public int Score { get; set; }

        /// <summary>Rendered by the colour picker.</summary>
        public string Colour { get; set; } = string.Empty;
    }
}
