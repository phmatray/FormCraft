using Microsoft.AspNetCore.Components;

namespace FormCraft.UnitTests.Rendering;

public class IntFieldRendererTests
{
    private readonly IntFieldRenderer _renderer;

    public IntFieldRendererTests()
    {
        _renderer = new IntFieldRenderer();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_Int_Type()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(int), mockField);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_False_For_Non_Int_Types()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert
        _renderer.CanRender(typeof(string), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(bool), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(DateTime), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(double), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(decimal), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(long), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(int?), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(object), mockField).ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Return_RenderFragment_For_NumericField()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockField("Age");
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Positive_Integer_Value()
    {
        // Arrange
        var model = new TestModel { Age = 42 };
        var field = CreateMockField("Age");
        var context = CreateContext(model, field, 42);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Zero_Value()
    {
        // Arrange
        var model = new TestModel { Age = 0 };
        var field = CreateMockField("Count");
        var context = CreateContext(model, field, 0);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Negative_Integer_Value()
    {
        // Arrange
        var model = new TestModel { Age = -10 };
        var field = CreateMockField("Temperature");
        var context = CreateContext(model, field, -10);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Null_Value_As_Zero()
    {
        // Arrange
        var model = new TestModel { Age = 0 };
        var field = CreateMockField("Count");
        var context = CreateContext(model, field, null);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // Logic test: null should be handled as 0 in the renderer
    }

    [Fact]
    public void Render_Should_Handle_All_Field_Properties()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockField("Age", "Enter your age in years", true, true);
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Empty_HelpText()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockField("Age", "");
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Required_Field()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockField("Age", isRequired: true);
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Disabled_Field()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockField("Age", isDisabled: true);
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Large_Integer_Values()
    {
        // Arrange
        var model = new TestModel { Age = int.MaxValue };
        var field = CreateMockField("Large Number");
        var context = CreateContext(model, field, int.MaxValue);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Minimum_Integer_Values()
    {
        // Arrange
        var model = new TestModel { Age = int.MinValue };
        var field = CreateMockField("Minimum Number");
        var context = CreateContext(model, field, int.MinValue);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Field_With_No_HelpText()
    {
        // Arrange
        var model = new TestModel { Age = 30 };
        var field = CreateMockField("Simple Age");
        var context = CreateContext(model, field, 30);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    private IFieldConfiguration<TestModel, object> CreateMockField(
        string label, 
        string? helpText = null,
        bool isRequired = false,
        bool isDisabled = false)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(helpText ?? string.Empty);
        A.CallTo(() => field.IsRequired).Returns(isRequired);
        A.CallTo(() => field.IsDisabled).Returns(isDisabled);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());
        
        return field;
    }

    private IFieldRenderContext<TestModel> CreateContext(
        TestModel model, 
        IFieldConfiguration<TestModel, object> field, 
        object? currentValue)
    {
        var context = A.Fake<IFieldRenderContext<TestModel>>();
        
        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(typeof(int));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));
        
        return context;
    }

    public class TestModel
    {
        public int Age { get; set; }
    }
}