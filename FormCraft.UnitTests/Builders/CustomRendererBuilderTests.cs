namespace FormCraft.UnitTests.Builders;

/// <summary>
/// Regression tests for the constrained generic <c>WithCustomRenderer&lt;TRenderer&gt;()</c> spelling
/// (#322). Before this fix only <c>WithCustomRenderer(Type)</c> compiled for a caller naming a specific
/// renderer type: an extension of the same generic shape existed, but the instance method
/// <c>WithCustomRenderer(IFieldRenderer)</c> shadowed it during member lookup, so
/// <c>.WithCustomRenderer&lt;TRenderer&gt;()</c> failed with CS0308.
/// </summary>
public class CustomRendererBuilderTests
{
    [Fact]
    public void WithCustomRenderer_Generic_Should_Set_CustomRendererType()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Volume, field => field
                .WithCustomRenderer<TestDoubleRenderer>())
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Volume");
        field.CustomRendererType.ShouldBe(typeof(TestDoubleRenderer));
    }

    [Fact]
    public void WithCustomRenderer_Generic_Method_Should_Constrain_TRenderer_To_ICustomFieldRenderer_Of_TValue()
    {
        // The constraint is what makes a renderer of the wrong TValue fail at COMPILE time (CS0311) --
        // e.g. `.AddField(x => x.Volume, f => f.WithCustomRenderer<ColorPickerRenderer>())`, where
        // Volume is double and ColorPickerRenderer : CustomFieldRendererBase<string>, does not compile.
        // A compile failure can't be asserted at runtime, so this pins the constraint itself instead.
        var method = typeof(FieldBuilder<TestModel, double>)
            .GetMethods()
            .Single(m => m.Name == nameof(FieldBuilder<TestModel, double>.WithCustomRenderer) && m.IsGenericMethodDefinition);

        var constraint = method.GetGenericArguments().Single().GetGenericParameterConstraints().Single();

        // Unsubstituted on a constructed generic type in this runtime -- the constraint reports as
        // ICustomFieldRenderer<TValue>, not ICustomFieldRenderer<double> -- so compare the open
        // generic definition rather than the closed type.
        constraint.IsGenericType.ShouldBeTrue();
        constraint.GetGenericTypeDefinition().ShouldBe(typeof(ICustomFieldRenderer<>));
    }

    public class TestModel
    {
        public double Volume { get; set; }
    }

    private class TestDoubleRenderer : CustomFieldRendererBase<double>
    {
        public override RenderFragment Render(IFieldRenderContext context)
        {
            return builder => builder.AddContent(0, "test");
        }
    }
}
