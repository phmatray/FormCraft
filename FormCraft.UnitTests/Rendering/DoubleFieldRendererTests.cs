namespace FormCraft.UnitTests.Rendering;

public class DoubleFieldRendererTests
{
    private readonly DoubleFieldRenderer _renderer;

    public DoubleFieldRendererTests()
    {
        _renderer = new DoubleFieldRenderer();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_Double_Type()
    {
        // Arrange
        var fieldConfig = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(double), fieldConfig);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_Nullable_Double_Type()
    {
        // Arrange
        var fieldConfig = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(double?), fieldConfig);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_False_For_Non_Double_Types()
    {
        // Arrange
        var fieldConfig = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert
        _renderer.CanRender(typeof(string), fieldConfig).ShouldBeFalse();
        _renderer.CanRender(typeof(int), fieldConfig).ShouldBeFalse();
        _renderer.CanRender(typeof(decimal), fieldConfig).ShouldBeFalse();
        _renderer.CanRender(typeof(float), fieldConfig).ShouldBeFalse();
        _renderer.CanRender(typeof(bool), fieldConfig).ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Create_MudNumericField_With_Correct_Attributes()
    {
        // Arrange
        var model = new TestModel { Temperature = 3.14159 };
        var field = CreateMockField("Temperature", "Enter temperature in Celsius", true);
        var context = CreateContext(model, field, 3.14159, typeof(double));

        // Act
        var fragment = _renderer.Render(context);

        // Assert
        fragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Null_Value_For_Non_Nullable()
    {
        // Arrange
        var model = new TestModel { Temperature = 0.0 };
        var field = CreateMockField("Measurement");
        var context = CreateContext(model, field, null, typeof(double));

        // Act
        var fragment = _renderer.Render(context);

        // Assert
        fragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Nullable_Double_Field()
    {
        // Arrange
        var model = new TestModel { OptionalMeasurement = 2.71828 };
        var field = CreateMockField("Optional Measurement");
        var context = CreateContext(model, field, 2.71828, typeof(double?));

        // Act
        var fragment = _renderer.Render(context);

        // Assert
        fragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Null_Value_For_Nullable()
    {
        // Arrange
        var model = new TestModel { OptionalMeasurement = null };
        var field = CreateMockField("Optional Measurement");
        var context = CreateContext(model, field, null, typeof(double?));

        // Act
        var fragment = _renderer.Render(context);

        // Assert
        fragment.ShouldNotBeNull();
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
        A.CallTo(() => field.Placeholder).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(isRequired);
        A.CallTo(() => field.IsDisabled).Returns(isDisabled);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());

        return field;
    }

    private IFieldRenderContext<TestModel> CreateContext(
        TestModel model,
        IFieldConfiguration<TestModel, object> field,
        object? currentValue,
        Type actualFieldType)
    {
        var context = A.Fake<IFieldRenderContext<TestModel>>();

        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(actualFieldType);
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));

        return context;
    }

    public class TestModel
    {
        public double Temperature { get; set; }
        public double? OptionalMeasurement { get; set; }
    }
}