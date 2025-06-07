namespace FormCraft.UnitTests.Rendering;

public class FieldRenderContextTests
{
    [Fact]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);
        var fieldType = typeof(string);
        var currentValue = "Test Value";
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = new FieldConfigurationWrapper<TestModel, string?>(field),
            ActualFieldType = fieldType,
            CurrentValue = currentValue,
            OnValueChanged = onValueChanged,
            OnDependencyChanged = onDependencyChanged
        };

        // Assert
        context.Model.ShouldBeSameAs(model);
        context.Field.ShouldNotBeNull();
        context.ActualFieldType.ShouldBe(fieldType);
        context.CurrentValue.ShouldBe(currentValue);
        context.OnValueChanged.ShouldBe(onValueChanged);
        context.OnDependencyChanged.ShouldBe(onDependencyChanged);
    }

    [Fact]
    public void Context_Should_Handle_Null_CurrentValue()
    {
        // Arrange
        var model = new TestModel { Name = null };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);

        // Act
        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = new FieldConfigurationWrapper<TestModel, string?>(field),
            ActualFieldType = typeof(string),
            CurrentValue = null,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, _ => { }),
            OnDependencyChanged = EventCallback.Factory.Create(this, () => { })
        };

        // Assert
        context.CurrentValue.ShouldBeNull();
    }

    [Fact]
    public void Context_Should_Support_Different_Field_Types()
    {
        // Arrange
        var model = new TestModel { Value = 42, IsActive = true };
        var intField = new FieldConfiguration<TestModel, int>(x => x.Value);
        var boolField = new FieldConfiguration<TestModel, bool>(x => x.IsActive);

        // Act
        var intContext = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = new FieldConfigurationWrapper<TestModel, int>(intField),
            ActualFieldType = typeof(int),
            CurrentValue = 42,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, _ => { }),
            OnDependencyChanged = EventCallback.Factory.Create(this, () => { })
        };

        var boolContext = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = new FieldConfigurationWrapper<TestModel, bool>(boolField),
            ActualFieldType = typeof(bool),
            CurrentValue = true,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, _ => { }),
            OnDependencyChanged = EventCallback.Factory.Create(this, () => { })
        };

        // Assert
        intContext.ActualFieldType.ShouldBe(typeof(int));
        intContext.CurrentValue.ShouldBe(42);
        
        boolContext.ActualFieldType.ShouldBe(typeof(bool));
        boolContext.CurrentValue.ShouldBe(true);
    }

    [Fact]
    public void Context_Should_Support_Complex_Types()
    {
        // Arrange
        var model = new TestModel { DateCreated = DateTime.Now };
        var field = new FieldConfiguration<TestModel, DateTime>(x => x.DateCreated);
        var currentValue = DateTime.Today;

        // Act
        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = new FieldConfigurationWrapper<TestModel, DateTime>(field),
            ActualFieldType = typeof(DateTime),
            CurrentValue = currentValue,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, _ => { }),
            OnDependencyChanged = EventCallback.Factory.Create(this, () => { })
        };

        // Assert
        context.ActualFieldType.ShouldBe(typeof(DateTime));
        context.CurrentValue.ShouldBe(currentValue);
    }

    [Fact]
    public void Context_Should_Support_Nullable_Types()
    {
        // Arrange
        var model = new TestModel { NullableValue = null };
        var field = new FieldConfiguration<TestModel, int?>(x => x.NullableValue);

        // Act
        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = new FieldConfigurationWrapper<TestModel, int?>(field),
            ActualFieldType = typeof(int?),
            CurrentValue = null,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, _ => { }),
            OnDependencyChanged = EventCallback.Factory.Create(this, () => { })
        };

        // Assert
        context.ActualFieldType.ShouldBe(typeof(int?));
        context.CurrentValue.ShouldBeNull();
    }

    [Fact]
    public void Context_Should_Support_Init_Only_Properties()
    {
        // Arrange
        var model = new TestModel();
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = new FieldConfigurationWrapper<TestModel, string?>(field),
            ActualFieldType = typeof(string),
            CurrentValue = "Test",
            OnValueChanged = onValueChanged,
            OnDependencyChanged = onDependencyChanged
        };

        // Attempting to modify init-only properties after construction should not be possible
        // This test verifies the properties are correctly set and accessible

        // Assert
        context.Model.ShouldBeSameAs(model);
        context.OnValueChanged.ShouldBe(onValueChanged);
        context.OnDependencyChanged.ShouldBe(onDependencyChanged);
    }

    public class TestModel
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public int? NullableValue { get; set; }
    }
}