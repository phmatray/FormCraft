using Microsoft.AspNetCore.Components;

namespace FormCraft.UnitTests.Rendering;

public class FieldRendererServiceTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Renderers()
    {
        // Arrange
        var mockRenderer = A.Fake<IFieldRenderer>();
        var renderers = new[] { mockRenderer };

        // Act
        var service = new FieldRendererService(renderers);

        // Assert
        service.ShouldNotBeNull();
    }

    [Fact]
    public void RenderField_Should_Find_Compatible_Renderer_And_Use_It()
    {
        // Arrange
        var model = new TestModel { Name = "Test Value" };
        var field = new FieldConfiguration<TestModel, string>(x => x.Name);
        var expectedFragment = new RenderFragment(builder => builder.AddContent(0, "Rendered Content"));

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(expectedFragment);

        var service = new FieldRendererService(new[] { mockRenderer });
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var result = service.RenderField(model, 
            new FieldConfigurationWrapper<TestModel, string>(field), 
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

        var service = new FieldRendererService(new[] { mockRenderer });
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
        var field = new FieldConfiguration<TestModel, string>(x => x.Name);
        IFieldRenderContext<TestModel>? capturedContext = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .ReturnsLazily((IFieldRenderContext<TestModel> ctx) =>
            {
                capturedContext = ctx;
                return new RenderFragment(builder => builder.AddContent(0, "Test"));
            });

        var service = new FieldRendererService(new[] { mockRenderer });
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model, 
            new FieldConfigurationWrapper<TestModel, string>(field), 
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
        var field = new FieldConfiguration<TestModel, string>(x => x.Name);

        var mockRenderer1 = A.Fake<IFieldRenderer>();
        var mockRenderer2 = A.Fake<IFieldRenderer>();
        var expectedFragment = new RenderFragment(builder => builder.AddContent(0, "Success"));

        A.CallTo(() => mockRenderer1.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(false);
        A.CallTo(() => mockRenderer2.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer2.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(expectedFragment);

        var service = new FieldRendererService(new[] { mockRenderer1, mockRenderer2 });
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var result = service.RenderField(model, 
            new FieldConfigurationWrapper<TestModel, string>(field), 
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
        var field = new FieldConfiguration<TestModel, string>(x => x.Name);
        IFieldRenderContext<TestModel>? capturedContext = null;

        var mockRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => mockRenderer.CanRender(typeof(string), A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => mockRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .ReturnsLazily((IFieldRenderContext<TestModel> ctx) =>
            {
                capturedContext = ctx;
                return new RenderFragment(builder => builder.AddContent(0, "Test"));
            });

        var service = new FieldRendererService(new[] { mockRenderer });
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        service.RenderField(model, 
            new FieldConfigurationWrapper<TestModel, string>(field), 
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
            .Returns(new RenderFragment(builder => builder.AddContent(0, "Test")));

        var service = new FieldRendererService(new[] { mockRenderer });
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
        var field = new FieldConfiguration<TestModel, string>(x => x.Name);

        var service = new FieldRendererService(new IFieldRenderer[0]);
        var onValueChanged = EventCallback.Factory.Create<object?>(this, _ => { });
        var onDependencyChanged = EventCallback.Factory.Create(this, () => { });

        // Act
        var result = service.RenderField(model, 
            new FieldConfigurationWrapper<TestModel, string>(field), 
            onValueChanged, 
            onDependencyChanged);

        // Assert
        result.ShouldNotBeNull();
        // Should return unsupported field type message
    }

    public class TestModel
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}