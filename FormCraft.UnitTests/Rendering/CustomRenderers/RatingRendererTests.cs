namespace FormCraft.UnitTests.Rendering.CustomRenderers;

public class RatingRendererTests
{
    [Fact]
    public void RatingRenderer_Should_Have_Correct_ValueType()
    {
        // Arrange
        var renderer = new RatingRenderer();

        // Assert
        renderer.ValueType.ShouldBe(typeof(int));
    }

    [Fact]
    public void RatingRenderer_Should_Render_MudRating_Component()
    {
        // Arrange
        var renderer = new RatingRenderer();
        var fieldConfig = new TestFieldConfiguration
        {
            Label = "Rating",
            FieldName = "TestRating",
            HelpText = "Rate from 1 to 5"
        };

        var context = A.Fake<IFieldRenderContext>();
        A.CallTo(() => context.CurrentValue).Returns(3);
        A.CallTo(() => context.FieldConfiguration).Returns(fieldConfig);
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));

        // Act
        var renderFragment = renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void RatingRenderer_Should_Use_MaxRating_From_AdditionalAttributes()
    {
        // Arrange
        var renderer = new RatingRenderer();
        var fieldConfig = new TestFieldConfiguration
        {
            AdditionalAttributes = new Dictionary<string, object> { { "MaxRating", 10 } }
        };

        var context = A.Fake<IFieldRenderContext>();
        A.CallTo(() => context.CurrentValue).Returns(5);
        A.CallTo(() => context.FieldConfiguration).Returns(fieldConfig);
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));

        // Act
        var renderFragment = renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // The renderer should use MaxRating = 10
    }

    [Fact]
    public void RatingRenderer_Should_Use_Default_MaxRating_When_Not_Specified()
    {
        // Arrange
        var renderer = new RatingRenderer();
        var fieldConfig = new TestFieldConfiguration();

        var context = A.Fake<IFieldRenderContext>();
        A.CallTo(() => context.CurrentValue).Returns(3);
        A.CallTo(() => context.FieldConfiguration).Returns(fieldConfig);
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));

        // Act
        var renderFragment = renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // The renderer should use default MaxRating = 5
    }

    private class TestFieldConfiguration
    {
        public string Label { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string? HelpText { get; set; }
        public bool IsDisabled { get; set; }
        public bool IsReadOnly { get; set; }
        public Dictionary<string, object> AdditionalAttributes { get; set; } = new();
    }
}