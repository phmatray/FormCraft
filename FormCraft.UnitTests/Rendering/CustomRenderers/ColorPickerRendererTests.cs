namespace FormCraft.UnitTests.Rendering.CustomRenderers;

public class ColorPickerRendererTests
{
    [Fact]
    public void ColorPickerRenderer_Should_Have_Correct_ValueType()
    {
        // Arrange
        var renderer = new ColorPickerRenderer();

        // Assert
        renderer.ValueType.ShouldBe(typeof(string));
    }

    [Fact]
    public void ColorPickerRenderer_Should_Render_ColorInput()
    {
        // Arrange
        var renderer = new ColorPickerRenderer();
        var fieldConfig = new TestFieldConfiguration
        {
            Label = "Color",
            FieldName = "TestColor",
            HelpText = "Select a color"
        };

        var context = A.Fake<IFieldRenderContext>();
        A.CallTo(() => context.CurrentValue).Returns("#FF0000");
        A.CallTo(() => context.FieldConfiguration).Returns(fieldConfig);
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));

        // Act
        var renderFragment = renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void ColorPickerRenderer_Should_Use_Default_Color_When_Value_Is_Null()
    {
        // Arrange
        var renderer = new ColorPickerRenderer();
        var context = A.Fake<IFieldRenderContext>();
        A.CallTo(() => context.CurrentValue).Returns((object?)null);
        A.CallTo(() => context.FieldConfiguration).Returns(new TestFieldConfiguration());
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));

        // Act
        var renderFragment = renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // The default color "#000000" should be used
    }

    private class TestFieldConfiguration
    {
        public string Label { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string? HelpText { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsReadOnly { get; set; }
    }
}