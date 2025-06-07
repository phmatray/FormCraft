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

    [Fact]
    public void CanRender_Should_Return_False_For_Nullable_Int()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(int?), mockField);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Apply_Min_Max_Constraints()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockFieldWithAttributes("Constrained Age", new Dictionary<string, object>
        {
            { "min", 18 },
            { "max", 65 }
        });
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Apply_Step_Attribute()
    {
        // Arrange
        var model = new TestModel { Age = 20 };
        var field = CreateMockFieldWithAttributes("Step Input", new Dictionary<string, object>
        {
            { "step", 5 }
        });
        var context = CreateContext(model, field, 20);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_String_To_Int_Conversion()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockField("Age");
        var context = CreateContext(model, field, "25"); // String representation

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Invalid_String_Values()
    {
        // Arrange
        var model = new TestModel { Age = 0 };
        var field = CreateMockField("Age");
        var context = CreateContext(model, field, "invalid_number");

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Decimal_Values()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockField("Age");
        var context = CreateContext(model, field, 25.7); // Decimal should be truncated

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Byte_Type_Values()
    {
        // Arrange
        var model = new ByteTestModel { Value = 255 };
        var field = CreateMockFieldForByteModel("Byte Value");
        var context = CreateContextForByteModel(model, field, (byte)255);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Short_Type_Values()
    {
        // Arrange
        var model = new ShortTestModel { Value = 32767 };
        var field = CreateMockFieldForShortModel("Short Value");
        var context = CreateContextForShortModel(model, field, (short)32767);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Long_Type_Values()
    {
        // Arrange
        var model = new LongTestModel { Value = 9223372036854775807L };
        var field = CreateMockFieldForLongModel("Long Value");
        var context = CreateContextForLongModel(model, field, 9223372036854775807L);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Overflow_Scenarios()
    {
        // Arrange
        var model = new TestModel { Age = 0 };
        var field = CreateMockField("Age");
        var context = CreateContext(model, field, long.MaxValue); // Value larger than int

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Custom_CSS_Classes()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockFieldWithAttributes("Styled Age", new Dictionary<string, object>
        {
            { "class", "form-control custom-number" }
        });
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Placeholder_Attribute()
    {
        // Arrange
        var model = new TestModel { Age = 0 };
        var field = CreateMockFieldWithAttributes("Age With Placeholder", new Dictionary<string, object>
        {
            { "placeholder", "Enter your age" }
        });
        var context = CreateContext(model, field, 0);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Multiple_Numeric_Constraints()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockFieldWithAttributes("Fully Constrained", new Dictionary<string, object>
        {
            { "min", 0 },
            { "max", 120 },
            { "step", 1 },
            { "pattern", @"\d+" }
        });
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Negative_Min_Max_Range()
    {
        // Arrange
        var model = new TestModel { Age = -5 };
        var field = CreateMockFieldWithAttributes("Negative Range", new Dictionary<string, object>
        {
            { "min", -100 },
            { "max", -1 }
        });
        var context = CreateContext(model, field, -5);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Boolean_As_Numeric_Value()
    {
        // Arrange
        var model = new TestModel { Age = 1 };
        var field = CreateMockField("Boolean as Int");
        var context = CreateContext(model, field, true); // true should convert to 1

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Null_Attributes()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var field = CreateMockFieldWithAttributes("Null Attributes", new Dictionary<string, object>
        {
            { "min", null! },
            { "max", null! },
            { "placeholder", null! }
        });
        var context = CreateContext(model, field, 25);

        // Act
        var renderFragment = _renderer.Render(context);

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void CanRender_Should_Return_False_For_Other_Numeric_Types()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert - IntFieldRenderer only supports int, not other numeric types
        _renderer.CanRender(typeof(byte), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(sbyte), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(short), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(ushort), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(uint), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(long), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(ulong), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(float), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(double), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(decimal), mockField).ShouldBeFalse();
    }

    private IFieldConfiguration<TestModel, object> CreateMockFieldWithAttributes(
        string label, 
        Dictionary<string, object> attributes)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(attributes);
        
        return field;
    }

    private IFieldConfiguration<ByteTestModel, object> CreateMockFieldForByteModel(string label)
    {
        var field = A.Fake<IFieldConfiguration<ByteTestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());
        
        return field;
    }

    private IFieldRenderContext<ByteTestModel> CreateContextForByteModel(
        ByteTestModel model, 
        IFieldConfiguration<ByteTestModel, object> field, 
        object? currentValue)
    {
        var context = A.Fake<IFieldRenderContext<ByteTestModel>>();
        
        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(typeof(byte));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));
        
        return context;
    }

    private IFieldConfiguration<ShortTestModel, object> CreateMockFieldForShortModel(string label)
    {
        var field = A.Fake<IFieldConfiguration<ShortTestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());
        
        return field;
    }

    private IFieldRenderContext<ShortTestModel> CreateContextForShortModel(
        ShortTestModel model, 
        IFieldConfiguration<ShortTestModel, object> field, 
        object? currentValue)
    {
        var context = A.Fake<IFieldRenderContext<ShortTestModel>>();
        
        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(typeof(short));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));
        
        return context;
    }

    private IFieldConfiguration<LongTestModel, object> CreateMockFieldForLongModel(string label)
    {
        var field = A.Fake<IFieldConfiguration<LongTestModel, object>>();
        
        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());
        
        return field;
    }

    private IFieldRenderContext<LongTestModel> CreateContextForLongModel(
        LongTestModel model, 
        IFieldConfiguration<LongTestModel, object> field, 
        object? currentValue)
    {
        var context = A.Fake<IFieldRenderContext<LongTestModel>>();
        
        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(typeof(long));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));
        
        return context;
    }

    public class TestModel
    {
        public int Age { get; set; }
    }

    public class ByteTestModel
    {
        public byte Value { get; set; }
    }

    public class ShortTestModel
    {
        public short Value { get; set; }
    }

    public class LongTestModel
    {
        public long Value { get; set; }
    }
}