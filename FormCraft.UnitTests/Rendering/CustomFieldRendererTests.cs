namespace FormCraft.UnitTests.Rendering;

public class CustomFieldRendererTests
{
    [Fact]
    public void FieldConfiguration_Should_Store_CustomRendererType()
    {
        // Arrange
        var config = new FieldConfiguration<TestModel, string>(x => x.TestProperty);

        // Act
        config.CustomRendererType = typeof(TestCustomRenderer);

        // Assert
        config.CustomRendererType.ShouldBe(typeof(TestCustomRenderer));
    }

    [Fact]
    public void WithCustomRenderer_Extension_Should_Set_CustomRendererType()
    {
        // Arrange & Act
        var formBuilder = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.TestProperty)
            .WithCustomRenderer<TestModel, string, TestCustomRenderer>()
            .Build();

        // Assert
        var field = formBuilder.Fields.FirstOrDefault();
        field.ShouldNotBeNull();
        field.CustomRendererType.ShouldBe(typeof(TestCustomRenderer));
    }

    [Fact]
    public void FieldRendererService_Should_Use_CustomRenderer_When_Specified()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<TestCustomRenderer>();
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();
        var model = new TestModel { TestProperty = "test" };

        var fieldConfig = new FieldConfiguration<TestModel, string>(x => x.TestProperty)
        {
            CustomRendererType = typeof(TestCustomRenderer)
        };

        var wrapper = new FieldConfigurationWrapper<TestModel, string>(fieldConfig);

        // Act
        var renderFragment = rendererService.RenderField(
            model,
            wrapper,
            EventCallback.Factory.Create<object?>(this, _ => { }),
            EventCallback.Factory.Create(this, () => { })
        );

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public void CustomFieldRendererBase_GetValue_Should_Return_Correct_Value()
    {
        // Arrange
        var renderer = new TestCustomRenderer();
        var context = A.Fake<IFieldRenderContext>();
        A.CallTo(() => context.CurrentValue).Returns("test value");

        // Act
        var value = renderer.TestGetValue(context);

        // Assert
        value.ShouldBe("test value");
    }

    [Fact]
    public async Task CustomFieldRendererBase_SetValue_Should_Invoke_Callback()
    {
        // Arrange
        var renderer = new TestCustomRenderer();
        var callbackInvoked = false;
        var newValue = "";

        var context = A.Fake<IFieldRenderContext>();
        A.CallTo(() => context.OnValueChanged).Returns(
            EventCallback.Factory.Create<object?>(this, (object? value) =>
            {
                callbackInvoked = true;
                newValue = value as string ?? "";
            })
        );

        // Act
        await renderer.TestSetValue(context, "new value");

        // Assert
        callbackInvoked.ShouldBeTrue();
        newValue.ShouldBe("new value");
    }

    [Fact]
    public void CustomFieldRenderer_ValueType_Should_Return_Correct_Type()
    {
        // Arrange
        var renderer = new TestCustomRenderer();

        // Assert
        renderer.ValueType.ShouldBe(typeof(string));
    }

    private class TestModel
    {
        public string TestProperty { get; set; } = "";
    }

    private class TestCustomRenderer : CustomFieldRendererBase<string>
    {
        public override RenderFragment Render(IFieldRenderContext context)
        {
            return builder => builder.AddContent(0, "Test Renderer");
        }

        public string? TestGetValue(IFieldRenderContext context) => GetValue(context);
        public Task TestSetValue(IFieldRenderContext context, string? value) => SetValue(context, value);
    }
}