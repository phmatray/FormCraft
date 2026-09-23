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
    public void RenderField_Should_Not_Throw_For_A_Nested_Binding_With_A_Null_Intermediate()
    {
        // Arrange - an ordinary field (no custom template) bound to a nested expression whose
        // intermediate is null. Before #397, FieldRendererService.GetCurrentValue invoked
        // FieldValueGetterCache<TModel>.GetOrCompile(field)(model) with no guard, so this threw an
        // unhandled NullReferenceException straight through render instead of just this field
        // rendering with no value.
        var model = new TestModel { NestedModel = null! };
        var field = new FieldConfiguration<TestModel, string?>(x => x.NestedModel.NestedProperty);
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
        Should.NotThrow(() => service.RenderField(model,
            new FieldConfigurationWrapper<TestModel, string?>(field),
            onValueChanged,
            onDependencyChanged));

        // Assert - the field renders with no value, the same "nothing to show" a genuinely-null leaf
        // value already produces.
        capturedContext.ShouldNotBeNull();
        capturedContext.CurrentValue.ShouldBeNull();
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
    public void RenderField_Should_Use_Custom_Renderer_And_Bypass_Standard_Renderers()
    {
        // Arrange
        var model = new TestModel { Name = "Test" };
        var field = new FieldConfiguration<TestModel, string?>(x => x.Name);

        var customRenderer = new FakeCustomRenderer();
        var services = new ServiceCollection();
        services.AddSingleton(customRenderer.GetType(), customRenderer);
        var serviceProvider = services.BuildServiceProvider();

        field.CustomRendererType = customRenderer.GetType();

        var standardRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => standardRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._)).Returns(true);

        var service = new FieldRendererService(new[] { standardRenderer }, serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var result = service.RenderField(model,
            new FieldConfigurationWrapper<TestModel, string?>(field),
            onValueChanged,
            onDependencyChanged);

        // Assert
        result.ShouldNotBeNull();
        customRenderer.WasCalled.ShouldBeTrue();
        A.CallTo(() => standardRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._)).MustNotHaveHappened();
        A.CallTo(() => standardRenderer.Render(A<IFieldRenderContext<TestModel>>._)).MustNotHaveHappened();
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


    [Fact]
    public void RenderField_Should_Not_Misroute_A_NonWrapper_Type_Whose_Name_Contains_FieldConfigurationWrapper()
    {
        // Arrange — a generic, non-wrapper IFieldConfiguration implementation whose type name happens
        // to contain "FieldConfigurationWrapper" as a substring. FieldName deliberately names no real
        // property on TestModel, so the old reflection fallback (GetProperty(field.FieldName)) cannot
        // coincidentally produce the right answer either — only routing this through the
        // expression-body branch (reading ValueExpression directly) can.
        var model = new TestModel { Name = "Test" };
        var parameter = Expression.Parameter(typeof(TestModel), "x");
        var valueExpression = Expression.Lambda<Func<TestModel, object>>(
            Expression.Convert(Expression.Property(parameter, nameof(TestModel.Name)), typeof(object)),
            parameter);
        var field = new FieldConfigurationWrapperLookalike<TestModel>("NotARealProperty", valueExpression);
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
        service.RenderField(model, field, onValueChanged, onDependencyChanged);

        // Assert — a type test based on the type NAME containing "FieldConfigurationWrapper" routes
        // this lookalike into the wrapper branch, where neither GetMethod("GetActualFieldType") (this
        // type doesn't declare it) nor the GetProperty("NotARealProperty") fallback (no such property)
        // can resolve anything, producing typeof(object). Identifying the wrapper by what it
        // implements instead correctly falls back to the expression body, resolving typeof(string).
        detectedType.ShouldBe(typeof(string));
    }

    [Fact]
    public void RenderField_Should_Resolve_Field_Type_At_Most_Once_Per_Configuration()
    {
        // Arrange
        var model = new TestModel { Name = "Test", Value = 42 };
        var stringParameter = Expression.Parameter(typeof(TestModel), "x");
        var stringExpression = Expression.Lambda<Func<TestModel, object>>(
            Expression.Convert(Expression.Property(stringParameter, nameof(TestModel.Name)), typeof(object)),
            stringParameter);
        var intParameter = Expression.Parameter(typeof(TestModel), "x");
        var intExpression = Expression.Lambda<Func<TestModel, object>>(
            Expression.Convert(Expression.Property(intParameter, nameof(TestModel.Value)), typeof(object)),
            intParameter);

        var field = new FieldConfigurationWrapperLookalike<TestModel>("Name", stringExpression);
        var detectedTypes = new List<Type>();

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._))
            .ReturnsLazily((Type type, IFieldConfiguration<object, object> _) =>
            {
                detectedTypes.Add(type);
                return true;
            });
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(builder => builder.AddContent(0, "Test"));

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act — render once (the field type resolves and, once cached, should be pinned to `string`),
        // then flip the double's own expression to one that would resolve to `int`, and render the
        // SAME configuration instance again.
        service.RenderField(model, field, onValueChanged, onDependencyChanged);
        field.ValueExpression = intExpression;
        service.RenderField(model, field, onValueChanged, onDependencyChanged);

        // Assert — a re-resolve on the second render would report `int` (the double now reports a
        // different type). Both renders instead report the type resolved on the first, proving the
        // field's type is resolved at most once per configuration instance.
        detectedTypes.Count.ShouldBe(2);
        detectedTypes[0].ShouldBe(typeof(string));
        detectedTypes[1].ShouldBe(typeof(string));
    }

    [Fact]
    public void RenderField_Should_Resolve_The_Real_Wrapper_Type_Even_When_The_Bound_Member_Is_A_Field_Not_A_Property()
    {
        // Arrange — FieldConfiguration<TModel, TValue> accepts a public FIELD, not just a property,
        // as long as the expression body is a direct MemberExpression, and FieldConfigurationWrapper
        // wraps it exactly like any other configuration. The expression-body FALLBACK branch in
        // FieldRendererService only pattern-matches `Member: PropertyInfo`, so it cannot resolve a
        // FieldInfo-backed member on its own — only the IActualFieldTypeSource dispatch (which reads
        // typeof(TValue) directly off the generic parameter, never the expression) gets this right.
        // This pins that a real FieldConfigurationWrapper actually goes through that dispatch, rather
        // than merely producing an answer the fallback happens to agree with (as it would for an
        // ordinary property) — so a future change that dropped the interface from the wrapper would
        // fail this test instead of passing by coincidence.
        var model = new ModelWithPublicField { Count = 7 };
        var fieldConfig = new FieldConfiguration<ModelWithPublicField, int>(x => x.Count);
        var wrapper = new FieldConfigurationWrapper<ModelWithPublicField, int>(fieldConfig);
        Type? detectedType = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._))
            .ReturnsLazily((Type type, IFieldConfiguration<object, object> _) =>
            {
                detectedType = type;
                return true;
            });
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<ModelWithPublicField>>._))
            .Returns(builder => builder.AddContent(0, "Test"));

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model, wrapper, onValueChanged, onDependencyChanged);

        // Assert
        detectedType.ShouldBe(typeof(int));
    }

    [Fact]
    public void RenderField_Should_Not_Share_A_Cache_Entry_Between_Two_Different_Configurations()
    {
        // Arrange — two distinct non-wrapper configuration instances, one bound to a string
        // property and one to an int property, both exercising the cached expression-body
        // resolution path. If the per-configuration cache were keyed by anything coarser than
        // instance identity — a process-wide table, or a key derived from FieldName or TModel alone
        // — resolving the second would answer with the first's cached type instead of its own.
        var model = new TestModel { Name = "Test", Value = 42 };
        var stringParameter = Expression.Parameter(typeof(TestModel), "x");
        var stringExpression = Expression.Lambda<Func<TestModel, object>>(
            Expression.Convert(Expression.Property(stringParameter, nameof(TestModel.Name)), typeof(object)),
            stringParameter);
        var intParameter = Expression.Parameter(typeof(TestModel), "x");
        var intExpression = Expression.Lambda<Func<TestModel, object>>(
            Expression.Convert(Expression.Property(intParameter, nameof(TestModel.Value)), typeof(object)),
            intParameter);

        var stringField = new FieldConfigurationWrapperLookalike<TestModel>("Name", stringExpression);
        var intField = new FieldConfigurationWrapperLookalike<TestModel>("Value", intExpression);
        var detectedTypes = new List<Type>();

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._))
            .ReturnsLazily((Type type, IFieldConfiguration<object, object> _) =>
            {
                detectedTypes.Add(type);
                return true;
            });
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(builder => builder.AddContent(0, "Test"));

        var service = new FieldRendererService(new[] { mockRenderer }, _serviceProvider);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act — resolve both configurations, in both orders, re-resolving the first again last
        service.RenderField(model, stringField, onValueChanged, onDependencyChanged);
        service.RenderField(model, intField, onValueChanged, onDependencyChanged);
        service.RenderField(model, stringField, onValueChanged, onDependencyChanged);

        // Assert — each configuration always resolves to its own type, never the other's
        detectedTypes.ShouldBe(new[] { typeof(string), typeof(int), typeof(string) });
    }

    private class ModelWithPublicField
    {
        public int Count;
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

    /// <summary>
    /// A generic <see cref="IFieldConfiguration{TModel, TValue}"/> (TValue fixed to <see cref="object"/>)
    /// implementation whose type name contains "FieldConfigurationWrapper" as a substring but is NOT
    /// <see cref="FieldConfigurationWrapper{TModel, TValue}"/>. Proves the wrapper test in
    /// <see cref="FieldRendererService"/> identifies the wrapper by what it implements, not by a
    /// substring of its type name (#314), and doubles as a mutable-<see cref="ValueExpression"/> field
    /// for exercising the per-configuration type cache.
    /// </summary>
    private sealed class FieldConfigurationWrapperLookalike<TModel> : IFieldConfiguration<TModel, object>
    {
        public FieldConfigurationWrapperLookalike(string fieldName, Expression<Func<TModel, object>> valueExpression)
        {
            FieldName = fieldName;
            ValueExpression = valueExpression;
        }

        public string FieldName { get; }
        public Expression<Func<TModel, object>> ValueExpression { get; set; }
        public string? Label { get; set; }
        public string? Placeholder { get; set; }
        public string? HelpText { get; set; }
        public string? CssClass { get; set; }
        public bool IsRequired { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsDisabled { get; set; }
        public bool IsReadOnly { get; set; }
        public int Order { get; set; }
        public Dictionary<string, object> AdditionalAttributes { get; } = new();
        public string? InputType { get; set; }
        public IReadOnlyList<IFieldValidator<TModel, object>> Validators { get; } = new List<IFieldValidator<TModel, object>>();
        public void AddValidator(IFieldValidator<TModel, object> validator) { }
        public List<IFieldDependency<TModel>> Dependencies { get; } = new();
        public Func<TModel, bool>? VisibilityCondition { get; set; }
        public Func<TModel, bool>? DisabledCondition { get; set; }
        public RenderFragment<IFieldContext<TModel, object>>? CustomTemplate { get; set; }
        public Type? CustomRendererType { get; set; }
    }

    private class FakeCustomRenderer : ICustomFieldRenderer<string>
    {
        public bool WasCalled { get; private set; }
        public Type ValueType => typeof(string);
        public RenderFragment Render(IFieldRenderContext context)
        {
            WasCalled = true;
            return builder => builder.AddContent(0, "Custom");
        }
    }
}
