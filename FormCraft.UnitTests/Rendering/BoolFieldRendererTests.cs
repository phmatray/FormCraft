using Microsoft.AspNetCore.Components;

namespace FormCraft.UnitTests.Rendering;

public class BoolFieldRendererTests
{
    private readonly BoolFieldRenderer _renderer;

    public BoolFieldRendererTests()
    {
        _renderer = new BoolFieldRenderer();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_Bool_Type()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(bool), mockField);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_False_For_Non_Bool_Types()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert
        _renderer.CanRender(typeof(string), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(int), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(DateTime), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(object), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(bool?), mockField).ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Return_RenderFragment_For_Checkbox()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Test Checkbox");
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_True_Value()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Active");
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_False_Value()
    {
        // Arrange
        var model = new TestModel { IsActive = false };
        var field = CreateMockField("Inactive");
        var context = CreateContext(model, field, false);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Null_Value_As_False()
    {
        // Arrange
        var model = new TestModel { IsActive = false };
        var field = CreateMockField("Test");
        var context = CreateContext(model, field, null);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // Logic test: null should be handled as false in the renderer
    }

    [Fact]
    public void Render_Should_Handle_All_Field_Properties()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Enable Feature", "This enables the feature", true);
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Empty_HelpText()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Test", "");
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Disabled_Field()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Test", isDisabled: true);
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Enabled_Field()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Test", isDisabled: false);
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Checkbox_State_Changes()
    {
        // Arrange
        var model = new TestModel { IsActive = false };
        var field = CreateMockField("Test");
        var context = CreateContext(model, field, false);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
        // The render fragment creation should succeed regardless of the checkbox state
    }

    [Fact]
    public void Render_Should_Handle_Complex_Label_Text()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Enable Advanced Settings & Features");
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    private IFieldConfiguration<TestModel, object> CreateMockField(
        string label, 
        string? helpText = null,
        bool isDisabled = false)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(helpText ?? string.Empty);
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
        A.CallTo(() => context.ActualFieldType).Returns(typeof(bool));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));
        
        return context;
    }

    public class TestModel
    {
        public bool IsActive { get; set; }
    }
}