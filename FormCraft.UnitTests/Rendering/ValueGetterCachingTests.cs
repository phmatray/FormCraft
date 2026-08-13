namespace FormCraft.UnitTests.Rendering;

/// <summary>
/// Pins what a renderer observes through <see cref="IFieldRenderContext{TModel}.CurrentValue" /> so the
/// per-render <c>Expression.Compile()</c> in <see cref="FieldRendererService" /> can be cached away
/// without changing behaviour (#269).
///
/// The distinction these tests exist to protect: the compiled <b>getter</b> may be cached, the
/// <b>value</b> it returns may never be. Caching a value instead of a getter would freeze every field
/// at its first-rendered content, which is the one way this optimization could silently break a form.
/// </summary>
public class ValueGetterCachingTests
{
    private readonly IServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();

    [Fact]
    public void RenderField_Should_Read_The_Model_Again_When_The_Value_Changed_Between_Renders()
    {
        // Arrange
        var model = new TestModel { Name = "before" };
        var field = new FieldConfigurationWrapper<TestModel, string?>(
            new FieldConfiguration<TestModel, string?>(x => x.Name));
        var (service, contexts) = CreateService();

        // Act - the same configuration instance rendered twice, with the model mutated in between.
        Render(service, model, field);
        model.Name = "after";
        Render(service, model, field);

        // Assert
        contexts.Count.ShouldBe(2);
        contexts[0].CurrentValue.ShouldBe("before");
        contexts[1].CurrentValue.ShouldBe("after");
    }

    [Fact]
    public void RenderField_Should_Keep_A_Null_Nullable_Value_Null_Across_Renders()
    {
        // Arrange - a nullable value type left null must not be coerced to its default (#150).
        var model = new TestModel();
        var field = new FieldConfigurationWrapper<TestModel, int?>(
            new FieldConfiguration<TestModel, int?>(x => x.NullableValue));
        var (service, contexts) = CreateService();

        // Act
        Render(service, model, field);
        model.NullableValue = 0;
        Render(service, model, field);

        // Assert - null and zero stay distinguishable on both renders.
        contexts[0].CurrentValue.ShouldBeNull();
        contexts[1].CurrentValue.ShouldBe(0);
    }

    [Fact]
    public void RenderField_Should_Not_Rebuild_The_Value_Getter_On_Every_Render()
    {
        // Arrange - a configuration that hands out a DIFFERENT expression on every read, each
        // reading a different property. That makes a recompile visible in the rendered value: a
        // getter compiled a second time necessarily targets a different property than the first,
        // so "did it recompile?" becomes "did the observed value change?" — a question that cannot
        // be answered by accident, and that stays honest no matter how the service resolves the
        // field type.
        var model = new TestModel
        {
            Name = "first read",
            Other = "second read",
            Third = "third read",
            Fourth = "fourth read"
        };

        var expressions = new Queue<Expression<Func<TestModel, object>>>(
        [
            m => (object)m.Name!,
            m => (object)m.Other!,
            m => (object)m.Third!,
            m => (object)m.Fourth!
        ]);
        var reads = 0;

        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => field.FieldName).Returns(nameof(TestModel.Name));
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());
        A.CallTo(() => field.ValueExpression).ReturnsLazily(() =>
        {
            reads++;
            return expressions.Count > 1 ? expressions.Dequeue() : expressions.Peek();
        });

        var (service, contexts) = CreateService();

        // Act
        Render(service, model, field);
        Render(service, model, field);

        // Assert - the second render reused the getter compiled for the first, so it observes the
        // same property. Recompiling would pick up a later expression and change the value.
        reads.ShouldBeGreaterThan(0, "the service never read the field's expression at all");
        contexts.Count.ShouldBe(2);
        contexts[0].CurrentValue.ShouldNotBeNull();
        contexts[1].CurrentValue.ShouldBe(
            contexts[0].CurrentValue,
            "the second render recompiled the value getter instead of reusing the cached one");
    }

    [Fact]
    public void RenderField_Should_Not_Share_A_Cached_Getter_Between_Two_Configurations_Of_The_Same_Property()
    {
        // Arrange - two separate configurations reporting the same field name, reading different
        // properties. A cache keyed by property name (or by model type) would serve the second
        // configuration the first one's getter.
        var model = new TestModel { Name = "from Name", Other = "from Other" };
        var (service, contexts) = CreateService();

        var first = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => first.FieldName).Returns(nameof(TestModel.Name));
        A.CallTo(() => first.AdditionalAttributes).Returns(new Dictionary<string, object>());
        A.CallTo(() => first.ValueExpression).Returns(m => (object)m.Name!);

        var second = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => second.FieldName).Returns(nameof(TestModel.Name));
        A.CallTo(() => second.AdditionalAttributes).Returns(new Dictionary<string, object>());
        A.CallTo(() => second.ValueExpression).Returns(m => (object)m.Other!);

        // Act
        Render(service, model, first);
        Render(service, model, second);

        // Assert
        contexts[0].CurrentValue.ShouldBe("from Name");
        contexts[1].CurrentValue.ShouldBe("from Other");
    }

    [Fact]
    public void ValueExpression_Should_Return_The_Same_Instance_On_Repeated_Access()
    {
        // Arrange
        var wrapper = new FieldConfigurationWrapper<TestModel, string?>(
            new FieldConfiguration<TestModel, string?>(x => x.Name));

        // Act
        var first = wrapper.ValueExpression;
        var second = wrapper.ValueExpression;

        // Assert - the wrapped configuration's expression is fixed at construction, so the object
        // typed projection around it is built once rather than rebuilt per access.
        second.ShouldBeSameAs(first);
    }

    private (FieldRendererService Service, List<IFieldRenderContext<TestModel>> Contexts) CreateService()
    {
        var contexts = new List<IFieldRenderContext<TestModel>>();

        var renderer = A.Fake<IFieldRenderer>();
        A.CallTo(() => renderer.CanRender(A<Type>._, A<IFieldConfiguration<object, object>>._))
            .Returns(true);
        A.CallTo(() => renderer.Render(A<IFieldRenderContext<TestModel>>._))
            .ReturnsLazily((IFieldRenderContext<TestModel> context) =>
            {
                contexts.Add(context);
                return new RenderFragment(builder => builder.AddContent(0, "rendered"));
            });

        return (new FieldRendererService([renderer], _serviceProvider), contexts);
    }

    private static void Render(
        FieldRendererService service,
        TestModel model,
        IFieldConfiguration<TestModel, object> field)
    {
        var receiver = new object();
        service.RenderField(
            model,
            field,
            EventCallback.Factory.Create<object?>(receiver, _ => { }),
            EventCallback.Factory.Create(receiver, () => { }));
    }

    public class TestModel
    {
        public string? Name { get; set; }
        public string? Other { get; set; }
        public string? Third { get; set; }
        public string? Fourth { get; set; }
        public int? NullableValue { get; set; }
    }
}
