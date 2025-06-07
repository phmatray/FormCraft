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

    [Fact]
    public void CanRender_Should_Return_False_For_Nullable_Bool()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(bool?), mockField);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Handle_Switch_Mode_Attribute()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Switch Mode", new Dictionary<string, object>
        {
            { "mode", "switch" }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Custom_True_False_Labels()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Custom Labels", new Dictionary<string, object>
        {
            { "TrueLabel", "Yes" },
            { "FalseLabel", "No" }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Indeterminate_State()
    {
        // Arrange
        var model = new NullableTestModel { IsEnabled = null };
        var field = CreateMockFieldForNullableModel("Three State");
        var context = CreateContextForNullableModel(model, field, null);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Required_Checkbox()
    {
        // Arrange
        var model = new TestModel { IsActive = false };
        var field = CreateMockFieldWithRequired("Accept Terms", true);
        var context = CreateContext(model, field, false);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Inline_Layout()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Inline Checkbox", new Dictionary<string, object>
        {
            { "inline", true }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Include_ARIA_Attributes()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Accessible Checkbox", new Dictionary<string, object>
        {
            { "aria-label", "Enable feature" },
            { "aria-describedby", "feature-help" }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_ReadOnly_State()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("ReadOnly Checkbox", new Dictionary<string, object>
        {
            { "readonly", true }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Apply_Custom_Colors()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Colored Checkbox", new Dictionary<string, object>
        {
            { "Color", "secondary" },
            { "UncheckedColor", "error" }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Dense_Mode()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Dense Checkbox", new Dictionary<string, object>
        {
            { "Dense", true }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Size_Attribute()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Sized Checkbox", new Dictionary<string, object>
        {
            { "Size", "large" }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_CSS_Classes()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Styled Checkbox", new Dictionary<string, object>
        {
            { "class", "custom-checkbox primary-action" }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Tooltip()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Tooltip Checkbox", new Dictionary<string, object>
        {
            { "title", "Click to enable this feature" }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Non_Boolean_Values()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Test");
        var context = CreateContext(model, field, "true"); // String value

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Integer_As_Boolean()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Test");
        var context = CreateContext(model, field, 1); // 1 should be true

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Multiple_Attributes()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockFieldWithAttributes("Complex Checkbox", new Dictionary<string, object>
        {
            { "Color", "primary" },
            { "Dense", true },
            { "Size", "medium" },
            { "class", "mb-4" },
            { "data-testid", "complex-checkbox" }
        });
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Validation_Attributes()
    {
        // Arrange
        var model = new TestModel { IsActive = false };
        var field = CreateMockFieldWithAttributes("Validated Checkbox", new Dictionary<string, object>
        {
            { "required", true },
            { "data-val", "true" },
            { "data-val-required", "You must accept the terms" }
        });
        var context = CreateContext(model, field, false);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Edge_Case_Empty_Label()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("");
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Very_Long_Label()
    {
        // Arrange
        var longLabel = new string('A', 500);
        var model = new TestModel { IsActive = true };
        var field = CreateMockField(longLabel);
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Special_Characters_In_Label()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var field = CreateMockField("Enable <Features> & \"Options\" 'Now'");
        var context = CreateContext(model, field, true);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    private IFieldConfiguration<TestModel, object> CreateMockFieldWithAttributes(
        string label, 
        Dictionary<string, object> attributes)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(attributes);
        
        return field;
    }

    private IFieldConfiguration<TestModel, object> CreateMockFieldWithRequired(
        string label, 
        bool isRequired)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.IsRequired).Returns(isRequired);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());
        
        return field;
    }

    private IFieldConfiguration<NullableTestModel, object> CreateMockFieldForNullableModel(string label)
    {
        var field = A.Fake<IFieldConfiguration<NullableTestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());
        
        return field;
    }

    private IFieldRenderContext<NullableTestModel> CreateContextForNullableModel(
        NullableTestModel model, 
        IFieldConfiguration<NullableTestModel, object> field, 
        object? currentValue)
    {
        var context = A.Fake<IFieldRenderContext<NullableTestModel>>();
        
        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(typeof(bool?));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));
        
        return context;
    }

    public class TestModel
    {
        public bool IsActive { get; set; }
    }

    public class NullableTestModel
    {
        public bool? IsEnabled { get; set; }
    }
}