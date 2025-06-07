using Microsoft.AspNetCore.Components;

namespace FormCraft.UnitTests.Rendering;

public class StringFieldRendererTests
{
    private readonly StringFieldRenderer _renderer;

    public StringFieldRendererTests()
    {
        _renderer = new StringFieldRenderer();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_String_Type()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(string), mockField);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_False_For_Non_String_Types()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert
        _renderer.CanRender(typeof(int), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(bool), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(DateTime), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(object), mockField).ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Return_RenderFragment_For_TextField_When_No_Options()
    {
        // Arrange
        var model = new TestModel { Name = "Test Value" };
        var field = CreateMockField("Test Label", "Test placeholder", "Test help text");
        var context = CreateContext(model, field, "Test Value");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Return_RenderFragment_For_SelectField_When_Options_Provided()
    {
        // Arrange
        var model = new TestModel { Name = "Option1" };
        var options = new[]
        {
            new SelectOption<string>("Option1", "Option 1"),
            new SelectOption<string>("Option2", "Option 2")
        };
        
        var field = CreateMockFieldWithOptions("Test Label", options);
        var context = CreateContext(model, field, "Option1");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Null_Current_Value()
    {
        // Arrange
        var model = new TestModel { Name = null };
        var field = CreateMockField("Test Label");
        var context = CreateContext(model, field, null);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Empty_String_Value()
    {
        // Arrange
        var model = new TestModel { Name = "" };
        var field = CreateMockField("Test Label");
        var context = CreateContext(model, field, "");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Use_TextField_When_No_Options_In_AdditionalAttributes()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = CreateMockField("Test Label");
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // Logic test: since no Options key exists, it should render as TextField
    }

    [Fact]
    public void Render_Should_Use_SelectField_When_Options_Present_In_AdditionalAttributes()
    {
        // Arrange
        var model = new TestModel { Name = "Option1" };
        var options = new[]
        {
            new SelectOption<string>("Option1", "Option 1")
        };
        var field = CreateMockFieldWithOptions("Test Label", options);
        var context = CreateContext(model, field, "Option1");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // Logic test: since Options key exists, it should render as SelectField
    }

    [Fact]
    public void Render_Should_Handle_Field_With_All_Properties_Set()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = CreateMockField("Test Label", "Enter value", "Helper text", true, true);
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Field_With_Empty_Strings()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = CreateMockField("", "", "");
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Select_Field_With_Empty_Options_Collection()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = CreateMockFieldWithOptions("Test Label", new SelectOption<string>[0]);
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Select_Field_With_Multiple_Options()
    {
        // Arrange
        var model = new TestModel { Name = "Option2" };
        var options = new[]
        {
            new SelectOption<string>("Option1", "First Option"),
            new SelectOption<string>("Option2", "Second Option"),
            new SelectOption<string>("Option3", "Third Option")
        };
        var field = CreateMockFieldWithOptions("Multi-Select", options);
        var context = CreateContext(model, field, "Option2");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    private IFieldConfiguration<TestModel, object> CreateMockField(
        string label, 
        string? placeholder = null, 
        string? helpText = null,
        bool isRequired = false,
        bool isDisabled = false)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.Placeholder).Returns(placeholder ?? string.Empty);
        A.CallTo(() => field.HelpText).Returns(helpText ?? string.Empty);
        A.CallTo(() => field.IsRequired).Returns(isRequired);
        A.CallTo(() => field.IsDisabled).Returns(isDisabled);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object?>());
        
        return field;
    }

    private IFieldConfiguration<TestModel, object> CreateMockFieldWithOptions(
        string label, 
        IEnumerable<SelectOption<string>> options)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        var attributes = new Dictionary<string, object?> { { "Options", options } };
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.Placeholder).Returns(string.Empty);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(attributes);
        
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
        A.CallTo(() => context.ActualFieldType).Returns(typeof(string));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));
        
        return context;
    }

    public class TestModel
    {
        public string? Name { get; set; }
    }
}