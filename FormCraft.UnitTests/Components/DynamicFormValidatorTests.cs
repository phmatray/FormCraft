using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.UnitTests.Components;

public class DynamicFormValidatorTests : Bunit.TestContext
{
    [Fact]
    public void DynamicFormValidator_Should_Throw_When_No_EditContext()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            RenderComponent<DynamicFormValidator<TestModel>>(parameters => parameters
                .Add(p => p.Configuration, config));
        });

        exception.Message.ShouldContain("requires a cascading parameter of type EditContext");
    }

    [Fact]
    public void DynamicFormValidator_Should_Initialize_Successfully_With_EditContext()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        // Act
        var component = RenderComponent<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<DynamicFormValidator<TestModel>>(0);
                builder.AddAttribute(1, "Configuration", config);
                builder.CloseComponent();
            }));

        // Assert
        component.ShouldNotBeNull();
        // No exception should be thrown
    }

    [Fact]
    public void DynamicFormValidator_Should_Dispose_Event_Handlers()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        // Act
        var component = RenderComponent<EditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<DynamicFormValidator<TestModel>>(0);
                builder.AddAttribute(1, "Configuration", config);
                builder.CloseComponent();
            }));

        // Dispose the component
        component.Dispose();

        // Assert
        // No exception should be thrown and component should dispose cleanly
        // This test primarily ensures the Dispose method doesn't throw
    }

    public interface ITestService
    {
        string GetValue();
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}