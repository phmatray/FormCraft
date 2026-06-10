namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Regression tests for WithCustomTemplate: templates configured through the
/// typed builder API must actually render (they used to be silently dropped by
/// FieldConfigurationWrapper and ignored by the render pipeline).
/// </summary>
public class CustomTemplateTests : MudBlazorTestBase
{
    [Fact]
    public void WithCustomTemplate_Should_Render_Template_Content()
    {
        // Arrange
        var model = new TestModel { Name = "John" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "my-custom-template");
                    builder.AddContent(2, $"Custom: {context.Value}");
                    builder.CloseElement();
                }))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var custom = component.Find(".my-custom-template");
        custom.TextContent.ShouldBe("Custom: John");
    }

    [Fact]
    public void WithCustomTemplate_Should_Receive_Typed_Configuration()
    {
        // Arrange - the template context must expose the typed field configuration
        var model = new TestModel { Name = "John" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Display Name")
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "template-label");
                    builder.AddContent(2, context.Configuration.Label);
                    builder.CloseElement();
                }))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find(".template-label").TextContent.ShouldBe("Display Name");
    }

    [Fact]
    public async Task WithCustomTemplate_ValueChanged_Should_Update_Model()
    {
        // Arrange - templates must be able to push values back into the model
        var model = new TestModel();
        IFieldContext<TestModel, string>? captured = null;
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithCustomTemplate(context => builder =>
                {
                    captured = context;
                    builder.AddContent(0, "template");
                }))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        captured.ShouldNotBeNull();

        // Act
        await component.InvokeAsync(() => captured!.ValueChanged.InvokeAsync("Jane"));

        // Assert
        model.Name.ShouldBe("Jane");
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
