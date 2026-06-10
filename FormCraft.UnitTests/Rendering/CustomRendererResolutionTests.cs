namespace FormCraft.UnitTests.Rendering;

/// <summary>
/// Regression tests for custom renderer resolution: WithCustomRenderer(IFieldRenderer)
/// must use the supplied instance (it used to be stored under a never-read key), and
/// custom renderers for value types must also match the nullable variant (int? vs int).
/// </summary>
public class CustomRendererResolutionTests
{
    private readonly IServiceProvider _serviceProvider;

    public CustomRendererResolutionTests()
    {
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void WithCustomRenderer_Instance_Should_Be_Used_For_Rendering()
    {
        // Arrange
        var model = new TestModel();
        var suppliedRenderer = A.Fake<IFieldRenderer>();
        var expectedFragment = new RenderFragment(builder => builder.AddContent(0, "from supplied instance"));
        A.CallTo(() => suppliedRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .Returns(expectedFragment);

        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithCustomRenderer(suppliedRenderer))
            .Build();

        var fallbackRenderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => fallbackRenderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._))
            .Returns(true);

        var service = new FieldRendererService(new[] { fallbackRenderer }, _serviceProvider);

        // Act
        var result = service.RenderField(model, config.Fields.First(),
            EventCallback.Factory.Create<object?>(this, _ => { }),
            EventCallback.Factory.Create(this, () => { }));

        // Assert - the supplied instance renders; the fallback is never consulted
        result.ShouldBe(expectedFragment);
        A.CallTo(() => suppliedRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => fallbackRenderer.Render(A<IFieldRenderContext<TestModel>>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public void Custom_Renderer_For_Value_Type_Should_Match_Nullable_Field()
    {
        // Arrange - renderer declares int, field is int?
        var model = new TestModel();
        var customRenderer = A.Fake<ICustomFieldRenderer>();
        A.CallTo(() => customRenderer.ValueType).Returns(typeof(int));
        var expectedFragment = new RenderFragment(builder => builder.AddContent(0, "custom int renderer"));
        A.CallTo(() => customRenderer.Render(A<IFieldRenderContext>._)).Returns(expectedFragment);

        var services = new ServiceCollection();
        services.AddSingleton(customRenderer.GetType(), customRenderer);
        var provider = services.BuildServiceProvider();

        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.OptionalAge, field => field.WithLabel("Age"))
            .Build();
        config.Fields.First().CustomRendererType = customRenderer.GetType();

        var service = new FieldRendererService(Array.Empty<IFieldRenderer>(), provider);

        // Act
        var result = service.RenderField(model, config.Fields.First(),
            EventCallback.Factory.Create<object?>(this, _ => { }),
            EventCallback.Factory.Create(this, () => { }));

        // Assert
        result.ShouldBe(expectedFragment);
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int? OptionalAge { get; set; }
    }
}
