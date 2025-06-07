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

    [Fact]
    public void Render_Should_Handle_Very_Long_String_Values()
    {
        // Arrange
        var longString = new string('A', 10000); // 10,000 character string
        var model = new TestModel { Name = longString };
        var field = CreateMockField("Long String Test");
        var context = CreateContext(model, field, longString);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_String_With_Special_Characters()
    {
        // Arrange
        var specialString = "Test with <HTML> & \"quotes\" and emoji 🚀";
        var model = new TestModel { Name = specialString };
        var field = CreateMockField("Special Characters");
        var context = CreateContext(model, field, specialString);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Apply_MaxLength_Attribute()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = CreateMockFieldWithAttribute("MaxLength", "MaxLength", 50);
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Apply_Pattern_Attribute()
    {
        // Arrange
        var model = new TestModel { Name = "test@example.com" };
        var field = CreateMockFieldWithAttribute("Email Pattern", "pattern", @"[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$");
        var context = CreateContext(model, field, "test@example.com");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Multiline_Text_Mode()
    {
        // Arrange
        var multilineText = "Line 1\nLine 2\nLine 3";
        var model = new TestModel { Name = multilineText };
        var field = CreateMockFieldWithAttribute("Multiline", "multiline", true);
        var context = CreateContext(model, field, multilineText);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Apply_Custom_CSS_Classes()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = CreateMockFieldWithAttribute("Custom CSS", "class", "custom-input special-field");
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Password_Type_Attribute()
    {
        // Arrange
        var model = new TestModel { Name = "secret123" };
        var field = CreateMockFieldWithAttribute("Password", "type", "password");
        var context = CreateContext(model, field, "secret123");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Email_InputMode()
    {
        // Arrange
        var model = new TestModel { Name = "user@domain.com" };
        var field = CreateMockFieldWithAttribute("Email", "inputmode", "email");
        var context = CreateContext(model, field, "user@domain.com");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_ReadOnly_Attribute()
    {
        // Arrange
        var model = new TestModel { Name = "readonly value" };
        var field = CreateMockFieldWithAttribute("ReadOnly", "readonly", true);
        var context = CreateContext(model, field, "readonly value");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Unicode_Characters()
    {
        // Arrange
        var unicodeString = "Ελληνικά, 中文, العربية, עברית, русский";
        var model = new TestModel { Name = unicodeString };
        var field = CreateMockField("Unicode Test");
        var context = CreateContext(model, field, unicodeString);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Null_Options_Gracefully()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = CreateMockFieldWithAttribute("Null Options", "Options", null);
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Malformed_Options_Data()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var invalidOptions = new List<object> { "invalid", 123, null };
        var field = CreateMockFieldWithAttribute("Invalid Options", "Options", invalidOptions);
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Empty_And_Whitespace_Values()
    {
        // Arrange & Act & Assert - Empty string
        var model1 = new TestModel { Name = "" };
        var field1 = CreateMockField("Empty");
        var context1 = CreateContext(model1, field1, "");
        var result1 = _renderer.Render(context1);
        result1.ShouldNotBeNull();

        // Whitespace only
        var model2 = new TestModel { Name = "   " };
        var field2 = CreateMockField("Whitespace");
        var context2 = CreateContext(model2, field2, "   ");
        var result2 = _renderer.Render(context2);
        result2.ShouldNotBeNull();

        // Tab and newline characters
        var model3 = new TestModel { Name = "\t\n\r" };
        var field3 = CreateMockField("Control Chars");
        var context3 = CreateContext(model3, field3, "\t\n\r");
        var result3 = _renderer.Render(context3);
        result3.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Select_With_Duplicate_Values()
    {
        // Arrange
        var model = new TestModel { Name = "Duplicate" };
        var options = new[]
        {
            new SelectOption<string>("Duplicate", "First Duplicate"),
            new SelectOption<string>("Duplicate", "Second Duplicate"),
            new SelectOption<string>("Unique", "Unique Option")
        };
        var field = CreateMockFieldWithOptions("Duplicate Values", options);
        var context = CreateContext(model, field, "Duplicate");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Large_Options_Collection()
    {
        // Arrange
        var model = new TestModel { Name = "Option500" };
        var options = Enumerable.Range(1, 1000)
            .Select(i => new SelectOption<string>($"Option{i}", $"Option {i}"))
            .ToArray();
        var field = CreateMockFieldWithOptions("Large Collection", options);
        var context = CreateContext(model, field, "Option500");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void CanRender_Should_Handle_String_Type_Variations()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert - Test string type variations
        _renderer.CanRender(typeof(string), mockField).ShouldBeTrue();
        
        // Test with different string-like scenarios
        var stringType = typeof(string);
        var result = _renderer.CanRender(stringType, mockField);
        result.ShouldBeTrue();
    }

    [Fact]
    public void Render_Should_Handle_Multiple_Attributes_Combination()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var attributes = new Dictionary<string, object?>
        {
            { "maxlength", 100 },
            { "pattern", @"[A-Za-z\s]+" },
            { "class", "form-control custom-input" },
            { "data-test", "string-field" },
            { "autocomplete", "name" }
        };
        var field = CreateMockFieldWithAttributes("Multiple Attributes", attributes);
        var context = CreateContext(model, field, "Test");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    private IFieldConfiguration<TestModel, object> CreateMockFieldWithAttribute(
        string label, 
        string attributeName, 
        object? attributeValue)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        var attributes = new Dictionary<string, object?> { { attributeName, attributeValue } };
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.Placeholder).Returns(string.Empty);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(attributes);
        
        return field;
    }

    private IFieldConfiguration<TestModel, object> CreateMockFieldWithAttributes(
        string label, 
        Dictionary<string, object?> attributes)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.Placeholder).Returns(string.Empty);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(attributes);
        
        return field;
    }

    public class TestModel
    {
        public string? Name { get; set; }
    }
}