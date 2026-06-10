namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Regression tests for numeric field type coverage: float/long/short/byte fields
/// must render (FormCraftComponent used to skip them silently and the renderer
/// component crashed casting its default step), unset Min/Max must not clamp
/// values to zero, and the decimal pattern attribute must be a valid regex.
/// </summary>
public class NumericFieldTypeSupportTests : MudBlazorTestBase
{
    [Fact]
    public void FloatField_Should_Render_As_NumericField()
    {
        var component = RenderForm(x => x.Weight);
        component.FindComponent<MudNumericField<float>>().Instance.Label.ShouldBe("Field");
    }

    [Fact]
    public void LongField_Should_Render_As_NumericField()
    {
        var component = RenderForm(x => x.Population);
        component.FindComponent<MudNumericField<long>>().Instance.Label.ShouldBe("Field");
    }

    [Fact]
    public void ShortField_Should_Render_As_NumericField()
    {
        var component = RenderForm(x => x.Year);
        component.FindComponent<MudNumericField<short>>().Instance.Label.ShouldBe("Field");
    }

    [Fact]
    public void ByteField_Should_Render_As_NumericField()
    {
        var component = RenderForm(x => x.Level);
        component.FindComponent<MudNumericField<byte>>().Instance.Label.ShouldBe("Field");
    }

    [Fact]
    public void DecimalField_Pattern_Should_Be_A_Valid_Regex()
    {
        var component = RenderForm(x => x.Price);
        var pattern = component.FindComponent<MudNumericField<decimal>>().Instance.Pattern;
        if (pattern != null)
        {
            // MudBlazor appends '*' to the configured pattern before emitting the
            // HTML attribute, so the combination must still be a valid regex.
            Should.NotThrow(() => System.Text.RegularExpressions.Regex.IsMatch("1.5", pattern + "*"));
        }
    }

    [Fact]
    public void RendererComponent_Should_Render_Float_Field_Without_Crashing()
    {
        var component = RenderViaRendererService<float>(x => x.Weight);
        component.Markup.ShouldContain("input");
    }

    [Fact]
    public void RendererComponent_Should_Render_Long_Field_Without_Crashing()
    {
        var component = RenderViaRendererService<long>(x => x.Population);
        component.Markup.ShouldContain("input");
    }

    [Fact]
    public void RendererComponent_Should_Not_Clamp_To_Zero_When_No_Range_Configured()
    {
        var component = RenderViaRendererService<int>(x => x.Age);
        var numeric = component.FindComponent<MudNumericField<int>>().Instance;
        numeric.Min.ShouldBe(int.MinValue);
        numeric.Max.ShouldBe(int.MaxValue);
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderForm<TValue>(
        System.Linq.Expressions.Expression<Func<TestModel, TValue>> property)
    {
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(property, field => field.WithLabel("Field"))
            .Build();

        return Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));
    }

    private IRenderedComponent<MudBlazorNumericFieldComponent<TestModel, TValue>> RenderViaRendererService<TValue>(
        System.Linq.Expressions.Expression<Func<TestModel, TValue>> property) where TValue : struct
    {
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(property, field => field.WithLabel("Field"))
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(TValue),
            CurrentValue = default(TValue),
        };

        return Render<MudBlazorNumericFieldComponent<TestModel, TValue>>(parameters => parameters
            .Add(p => p.Context, context));
    }

    private class TestModel
    {
        public int Age { get; set; }
        public decimal Price { get; set; }
        public float Weight { get; set; }
        public long Population { get; set; }
        public short Year { get; set; }
        public byte Level { get; set; }
    }
}
