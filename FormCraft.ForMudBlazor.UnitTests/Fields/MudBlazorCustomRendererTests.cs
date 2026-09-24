namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// The three custom renderers — slider, rating and colour picker — used via
/// <c>.AsSlider(...)</c>/<c>.AsRating(...)</c>/<c>.AsColorPicker()</c> rather than registered as
/// <c>IFieldRenderer</c>. Before this file: 0% covered
/// (<c>MudBlazorSliderComponent.razor</c>/<c>MudBlazorRatingRenderer.cs</c>), touched only by
/// <see cref="FieldConfigurationParityTests"/>'s exemption list (they cache nothing derived from
/// configuration, so that suite has no row for them). Mirrors
/// <c>FormCraft.ForFluentUI.UnitTests.Fields.FluentUICustomRendererTests</c> deliberately (issue
/// #461, AC5/AC6).
/// </summary>
public class MudBlazorCustomRendererTests : MudBlazorTestBase
{
    [Fact]
    public void Slider_Renderer_Should_Render_A_MudSlider()
    {
        var component = RenderSlider();

        component.FindComponents<MudSlider<double>>().ShouldNotBeEmpty();
    }

    [Fact]
    public void Slider_Renderer_Should_Honour_Min_Max_And_Step()
    {
        // .AsSlider(min:,max:,step:) is ambiguous here: core's own generic numeric-field AsSlider
        // (FormCraft.FieldBuilderExtensions, a same-named, unrelated "native <input type=range>"
        // concept) and MudBlazor's AsSlider both match a 3-named-argument call on a double field.
        // WithCustomRenderer + the raw attribute keys is what MudBlazor's AsSlider does internally,
        // and is also literally what the issue's plan asks for ("same .WithAttribute('Min'/'Max'/
        // 'Step', ...) keys").
        var component = RenderSlider(f => f
            .WithLabel("Volume")
            .WithCustomRenderer<MudBlazorSliderRenderer>()
            .WithAttribute("Min", 10.0)
            .WithAttribute("Max", 50.0)
            .WithAttribute("Step", 5.0));

        var slider = component.FindComponent<MudSlider<double>>().Instance;
        slider.Min.ShouldBe(10.0);
        slider.Max.ShouldBe(50.0);
        slider.Step.ShouldBe(5.0);
    }

    /// <summary>
    /// The row above proves the ATTRIBUTE KEYS <c>.AsSlider(...)</c> writes; this one proves the
    /// EXTENSION METHOD ITSELF still writes them — the two facts previously agreed only because both
    /// re-implemented the same attribute keys by hand (verification-gap review, #461): renaming a key
    /// inside <c>MudBlazorFieldBuilderExtensions.AsSlider</c> or dropping its
    /// <c>.WithCustomRenderer&lt;MudBlazorSliderRenderer&gt;()</c> call would leave every other fact
    /// here green. Fully qualified for the same ambiguity reason as the row above.
    /// </summary>
    [Fact]
    public void AsSlider_Extension_Itself_Should_Configure_The_Real_MudSlider()
    {
        var config = FormBuilder<PreferencesModel>.Create()
            .AddField(x => x.Volume, field => MudBlazorFieldBuilderExtensions.AsSlider(
                field.WithLabel("Volume"), min: 10, max: 50, step: 5))
            .Build();

        var component = Render<FormCraftComponent<PreferencesModel>>(p => p
            .Add(c => c.Model, new PreferencesModel())
            .Add(c => c.Configuration, config));

        var slider = component.FindComponent<MudSlider<double>>().Instance;
        slider.Min.ShouldBe(10.0);
        slider.Max.ShouldBe(50.0);
        slider.Step.ShouldBe(5.0);
    }

    [Fact]
    public void Slider_Renderer_Should_Default_Its_Range_When_Unconfigured()
    {
        var component = RenderSlider();

        var slider = component.FindComponent<MudSlider<double>>().Instance;
        slider.Min.ShouldBe(0.0);
        slider.Max.ShouldBe(100.0);
        slider.Step.ShouldBe(1.0);
    }

    [Fact]
    public async Task Slider_Moving_It_Should_Write_The_Model()
    {
        var model = new PreferencesModel();
        var component = RenderSlider(model: model);
        var slider = component.FindComponent<MudSlider<double>>();

        await slider.InvokeAsync(() => slider.Instance.ValueChanged.InvokeAsync(42.0));

        model.Volume.ShouldBe(42.0);
    }

    [Fact]
    public void Rating_Renderer_Should_Render_A_MudRating()
    {
        var component = RenderRating();

        component.FindComponents<MudRating>().ShouldNotBeEmpty();
    }

    [Fact]
    public void Rating_Renderer_Should_Honour_A_Configured_Maximum()
    {
        var component = RenderRating(f => f
            .WithLabel("Score")
            .AsRating(maxRating: 3));

        component.FindComponent<MudRating>().Instance.MaxValue.ShouldBe(3);
    }

    [Fact]
    public void Rating_Renderer_Should_Default_Its_Maximum_To_Five()
    {
        var component = RenderRating();

        component.FindComponent<MudRating>().Instance.MaxValue.ShouldBe(5);
    }

    [Fact]
    public async Task Rating_Renderer_Should_Write_The_Chosen_Score_To_The_Model()
    {
        var model = new PreferencesModel();
        var component = RenderRating(model: model);
        var rating = component.FindComponent<MudRating>();

        await rating.InvokeAsync(() => rating.Instance.SelectedValueChanged.InvokeAsync(4));

        model.Score.ShouldBe(4);
    }

    /// <summary>
    /// Mirrors other field components' <c>IsDisabled</c>/<c>IsReadOnly</c> semantics (per the
    /// issue's own Edge cases): clicking a star must not change the model when the field is
    /// disabled or read-only.
    /// </summary>
    [Fact]
    public async Task Rating_Renderer_Should_Not_Change_The_Model_When_Disabled()
    {
        var model = new PreferencesModel();
        var component = RenderRating(f => f.WithLabel("Score").AsRating().Disabled(), model);

        await component.InvokeAsync(() => component.FindAll("span.mud-rating-item")[3].ClickAsync(new()));

        model.Score.ShouldBe(0);
    }

    [Fact]
    public async Task Rating_Renderer_Should_Not_Change_The_Model_When_ReadOnly()
    {
        var model = new PreferencesModel();
        var component = RenderRating(f => f.WithLabel("Score").AsRating().ReadOnly(), model);

        await component.InvokeAsync(() => component.FindAll("span.mud-rating-item")[3].ClickAsync(new()));

        model.Score.ShouldBe(0);
    }

    [Fact]
    public void ColorPicker_Renderer_Should_Render_A_MudColorPicker()
    {
        var component = RenderColorPicker();

        component.FindComponents<MudColorPicker>().ShouldNotBeEmpty();
    }

    [Fact]
    public void ColorPicker_Renderer_Should_Show_The_Models_Current_Colour()
    {
        var component = RenderColorPicker(new PreferencesModel { Colour = "#ff0000" });

        component.FindComponent<MudColorPicker>().Instance.Text.ShouldBe("#ff0000");
    }

    [Fact]
    public async Task ColorPicker_Choosing_A_Colour_Should_Write_The_Model()
    {
        var model = new PreferencesModel();
        var component = RenderColorPicker(model);
        var picker = component.FindComponent<MudColorPicker>();

        await picker.InvokeAsync(() => picker.Instance.TextChanged.InvokeAsync("#00ff00"));

        model.Colour.ShouldBe("#00ff00");
    }

    private IRenderedComponent<FormCraftComponent<PreferencesModel>> RenderSlider(
        Action<FieldBuilder<PreferencesModel, double>>? configure = null,
        PreferencesModel? model = null)
    {
        configure ??= f => f.WithLabel("Volume").WithCustomRenderer<MudBlazorSliderRenderer>();

        var config = FormBuilder<PreferencesModel>.Create()
            .AddField(x => x.Volume, configure)
            .Build();

        return Render<FormCraftComponent<PreferencesModel>>(p => p
            .Add(c => c.Model, model ?? new PreferencesModel())
            .Add(c => c.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<PreferencesModel>> RenderRating(
        Action<FieldBuilder<PreferencesModel, int>>? configure = null,
        PreferencesModel? model = null)
    {
        configure ??= f => f.WithLabel("Score").AsRating();

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
            .AddField(x => x.Colour, f => f.WithLabel("Colour").AsColorPicker())
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
