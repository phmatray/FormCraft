using Microsoft.AspNetCore.Components;

namespace FormCraft.UnitTests.Rendering;

public class DateTimeFieldRendererTests
{
    private readonly DateTimeFieldRenderer _renderer;

    public DateTimeFieldRendererTests()
    {
        _renderer = new DateTimeFieldRenderer();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_DateTime_Type()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(DateTime), mockField);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_Nullable_DateTime_Type()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(DateTime?), mockField);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_False_For_Non_DateTime_Types()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert
        _renderer.CanRender(typeof(string), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(int), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(bool), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(DateOnly), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(TimeOnly), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(DateTimeOffset), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(object), mockField).ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Return_RenderFragment_For_DatePicker()
    {
        // Arrange
        var model = new TestModel { BirthDate = new DateTime(1990, 1, 1) };
        var field = CreateMockField("Birth Date");
        var context = CreateContext(model, field, new DateTime(1990, 1, 1));

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Valid_DateTime_Value()
    {
        // Arrange
        var testDate = new DateTime(2023, 12, 25);
        var model = new TestModel { BirthDate = testDate };
        var field = CreateMockField("Event Date");
        var context = CreateContext(model, field, testDate);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_DateTime_MinValue()
    {
        // Arrange
        var model = new TestModel { BirthDate = DateTime.MinValue };
        var field = CreateMockField("Start Date");
        var context = CreateContext(model, field, DateTime.MinValue);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_DateTime_MaxValue()
    {
        // Arrange
        var model = new TestModel { BirthDate = DateTime.MaxValue };
        var field = CreateMockField("End Date");
        var context = CreateContext(model, field, DateTime.MaxValue);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Null_DateTime_Value()
    {
        // Arrange
        var model = new TestModel { BirthDate = null };
        var field = CreateMockField("Optional Date");
        var context = CreateContext(model, field, null);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // Logic test: null DateTime should be handled gracefully
    }

    [Fact]
    public void Render_Should_Handle_Nullable_DateTime_With_Value()
    {
        // Arrange
        var testDate = new DateTime(2024, 6, 15);
        var model = new TestModel { BirthDate = testDate };
        var field = CreateMockField("Birth Date");
        var context = CreateContext(model, field, (DateTime?)testDate);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_All_Field_Properties()
    {
        // Arrange
        var model = new TestModel { BirthDate = DateTime.Today };
        var field = CreateMockField("Event Date", "Select the date of the event", true, true);
        var context = CreateContext(model, field, DateTime.Today);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Empty_HelpText()
    {
        // Arrange
        var model = new TestModel { BirthDate = DateTime.Today };
        var field = CreateMockField("Date", "");
        var context = CreateContext(model, field, DateTime.Today);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Required_Field()
    {
        // Arrange
        var model = new TestModel { BirthDate = DateTime.Today };
        var field = CreateMockField("Required Date", isRequired: true);
        var context = CreateContext(model, field, DateTime.Today);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Disabled_Field()
    {
        // Arrange
        var model = new TestModel { BirthDate = DateTime.Today };
        var field = CreateMockField("Disabled Date", isDisabled: true);
        var context = CreateContext(model, field, DateTime.Today);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Today_Date()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockField("Today");
        var context = CreateContext(model, field, today);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_DateTime_With_Time_Component()
    {
        // Arrange
        var dateTimeWithTime = new DateTime(2024, 3, 15, 14, 30, 45);
        var model = new TestModel { BirthDate = dateTimeWithTime };
        var field = CreateMockField("Date with Time");
        var context = CreateContext(model, field, dateTimeWithTime);

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
        A.CallTo(() => context.ActualFieldType).Returns(currentValue?.GetType() ?? typeof(DateTime));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));
        
        return context;
    }

    public class TestModel
    {
        public DateTime? BirthDate { get; set; }
    }
}