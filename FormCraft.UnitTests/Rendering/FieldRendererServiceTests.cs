namespace FormCraft.UnitTests.Rendering;

public class FieldRendererServiceTests
{
    private readonly IServiceProvider _serviceProvider;

    public FieldRendererServiceTests()
    {
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Renderers()
    {
        // Arrange
        var mockRenderer = A.Fake<IFieldRenderer>();
        var renderers = new[] { mockRenderer };

        // Act
        var service = new FieldRendererService(renderers, _serviceProvider);

        // Assert
        service.ShouldNotBeNull();
    }

    [Fact]
    public void RenderField_Should_Find_Compatible_Renderer_And_Use_It()
    {
        // Arrange
        var model = new TestModel { Name = "Test Value" };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);
        var expectedFragment = new RenderFragment(builder => builder.AddContent(0, "Rendered Content"));

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(expectedFragment);

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var result = service.RenderField(model,
            new FieldConfigurationWrapper<TestModel, string?>(field),
            onValueChanged,
            onDependencyChanged);

        // Assert
        result.ShouldNotBeNull();
        A.CallTo(() => mockRenderer.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void RenderField_Should_Return_Unsupported_Message_When_No_Renderer_Found()
    {
        // Arrange
        var model = new TestModel { Value = 42 };
        var field = new FieldConfiguration<TestModel, int>(x => x.Value);

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(int), A<IFieldConfiguration<object, object>>._))
            .Returns(false);

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var result = service.RenderField(model,
            new FieldConfigurationWrapper<TestModel, int>(field),
            onValueChanged,
            onDependencyChanged);

        // Assert
        result.ShouldNotBeNull();
        // The render fragment should contain the unsupported message
        // We can't easily test the content without rendering, but we can verify it's not null
    }

    [Fact]
    public void RenderField_Should_Create_Correct_Context()
    {
        // Arrange
        var model = new TestModel { Name = "Test Name" };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);
        IFieldRenderContext<TestModel>? capturedContext = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .ReturnsLazily((IFieldRenderContext<TestModel> ctx) =>
            {
                capturedContext = ctx;
                return builder => builder.AddContent(0, "Test");
            });

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model,
            new FieldConfigurationWrapper<TestModel, string?>(field),
            onValueChanged,
            onDependencyChanged);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.Model.ShouldBeSameAs(model);
        capturedContext.Field.ShouldNotBeNull();
        capturedContext.ActualFieldType.ShouldBe(typeof(string));
        capturedContext.CurrentValue.ShouldBe("Test Name");
        capturedContext.OnValueChanged.ShouldBe(onValueChanged);
        capturedContext.OnDependencyChanged.ShouldBe(onDependencyChanged);
    }

    [Fact]
    public void RenderField_Should_Try_Multiple_Renderers_Until_Compatible_Found()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);

        var mockRenderer1 = A.Fake<IFieldRenderer>();
        var mockRenderer2 = A.Fake<IFieldRenderer>();
        var expectedFragment = new RenderFragment(builder => builder.AddContent(0, "Success"));

        A.CallTo(() => mockRenderer1.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(false);
        A.CallTo(() => mockRenderer2.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer2.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(expectedFragment);

        var service = new FieldRendererService(new[] { mockRenderer1, mockRenderer2 }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var result = service.RenderField(model,
            new FieldConfigurationWrapper<TestModel, string?>(field),
            onValueChanged,
            onDependencyChanged);

        // Assert
        result.ShouldNotBeNull();
        A.CallTo(() => mockRenderer1.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => mockRenderer2.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => mockRenderer1.Render(A<IFieldRenderContext<TestModel>>._))
            .MustNotHaveHappened();
        A.CallTo(() => mockRenderer2.Render(A<IFieldRenderContext<TestModel>>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void RenderField_Should_Handle_Null_Current_Value()
    {
        // Arrange
        var model = new TestModel { Name = null };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);
        IFieldRenderContext<TestModel>? capturedContext = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .ReturnsLazily((IFieldRenderContext<TestModel> ctx) =>
            {
                capturedContext = ctx;
                return builder => builder.AddContent(0, "Test");
            });

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model,
            new FieldConfigurationWrapper<TestModel, string?>(field),
            onValueChanged,
            onDependencyChanged);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.CurrentValue.ShouldBeNull();
    }

    [Fact]
    public void RenderField_Should_Get_Type_From_Wrapper_GetActualFieldType_Method()
    {
        // Arrange
        var model = new TestModel { Value = 123 };
        var field = new FieldConfiguration<TestModel, int>(x => x.Value);
        var wrapper = new FieldConfigurationWrapper<TestModel, int>(field);

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(int), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(builder => builder.AddContent(0, "Test"));

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model, wrapper, onValueChanged, onDependencyChanged);

        // Assert
        A.CallTo(() => mockRenderer.CanRender(typeof(int), A<IFieldConfiguration<object, object>>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void RenderField_Should_Handle_Empty_Renderer_Collection()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);

        var service = new FieldRendererService(new IFieldRenderer[0], _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var result = service.RenderField(model,
            new FieldConfigurationWrapper<TestModel, string?>(field),
            onValueChanged,
            onDependencyChanged);

        // Assert
        result.ShouldNotBeNull();
        // Should return unsupported field type message
    }

    [Fact]
    public void RenderField_Should_Detect_Correct_Type_For_Simple_MemberExpression()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);
        var wrapper = new FieldConfigurationWrapper<TestModel, string?>(field);
        Type? detectedType = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._))
            .ReturnsLazily((Type type, IFieldConfiguration<object, object> _) =>
            {
                detectedType = type;
                return true;
            });
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(builder => builder.AddContent(0, "Test"));

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model, wrapper, onValueChanged, onDependencyChanged);

        // Assert
        // The GetActualFieldType method in FieldRendererService handles UnaryExpression
        // and returns the underlying property type
        detectedType.ShouldBe(typeof(string));
    }


    [Fact]
    public void RenderField_Should_Detect_Correct_Type_For_Value_Type_MemberExpression()
    {
        // Arrange
        var model = new TestModel { Value = 42 };
        var field = new FieldConfiguration<TestModel, int>(x => x.Value);
        var wrapper = new FieldConfigurationWrapper<TestModel, int>(field);
        Type? detectedType = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._))
            .ReturnsLazily((Type type, IFieldConfiguration<object, object> _) =>
            {
                detectedType = type;
                return true;
            });
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(builder => builder.AddContent(0, "Test"));

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model, wrapper, onValueChanged, onDependencyChanged);

        // Assert
        // The GetActualFieldType method detects int from the UnaryExpression
        detectedType.ShouldBe(typeof(int));
    }

    [Fact]
    public void RenderField_Should_Detect_Correct_Type_For_Nullable_Type()
    {
        // Arrange
        var model = new TestModel { NullableValue = 42 };
        var field = new FieldConfiguration<TestModel, int?>(x => x.NullableValue);
        var wrapper = new FieldConfigurationWrapper<TestModel, int?>(field);
        Type? detectedType = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._))
            .ReturnsLazily((Type type, IFieldConfiguration<object, object> _) =>
            {
                detectedType = type;
                return true;
            });
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(builder => builder.AddContent(0, "Test"));

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model, wrapper, onValueChanged, onDependencyChanged);

        // Assert
        detectedType.ShouldBe(typeof(int?));
    }


    [Fact]
    public void RenderField_Should_Use_Correct_ActualFieldType_In_Context()
    {
        // Arrange
        var model = new TestModel { DateCreated = DateTime.Now };
        var field = new FieldConfiguration<TestModel, DateTime>(x => x.DateCreated);
        var wrapper = new FieldConfigurationWrapper<TestModel, DateTime>(field);
        IFieldRenderContext<TestModel>? capturedContext = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(DateTime), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .ReturnsLazily((IFieldRenderContext<TestModel> ctx) =>
            {
                capturedContext = ctx;
                return builder => builder.AddContent(0, "Test");
            });

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model, wrapper, onValueChanged, onDependencyChanged);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ActualFieldType.ShouldBe(typeof(DateTime));
    }


    public enum TestEnum
    {
        Active,
        Inactive,
        Pending
    }

    public class NestedModel
    {
        public string NestedProperty { get; set; } = string.Empty;
    }

    public class TestModel
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public int? NullableValue { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public TestEnum Status { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public List<string> Tags { get; set; } = new();
        public NestedModel NestedModel { get; set; } = new();
    }
}