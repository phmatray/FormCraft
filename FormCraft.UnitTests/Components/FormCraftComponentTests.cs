using MudBlazor.Services;

namespace FormCraft.UnitTests.Components;

public class FormCraftComponentTests : Bunit.TestContext
{
    private readonly IFieldRendererService _fieldRendererService;

    public FormCraftComponentTests()
    {
        _fieldRendererService = A.Fake<IFieldRendererService>();
        Services.AddSingleton(_fieldRendererService);
        // Add MudBlazor services required by FormCraftComponent
        Services.AddMudServices();
        
        // Setup JSInterop for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void FormCraftComponent_Should_Render_With_Default_Parameters()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        // Act
        var component = RenderComponent<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.ShouldNotBeNull();
        component.Find("form").ShouldNotBeNull();
        // FormCraftComponent renders fields directly in mb-4 divs
        component.FindAll(".mb-4").Count.ShouldBe(1);
    }

    [Fact]
    public void FormCraftComponent_Should_Render_Fields_In_Order()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
                .WithOrder(2)
            .AddField(x => x.Email)
                .WithLabel("Email")
                .WithOrder(1)
            .AddField(x => x.Age)
                .WithLabel("Age")
                .WithOrder(3)
            .Build();

        // Act
        var component = RenderComponent<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        // Check the order of rendered fields by their labels
        var fields = component.FindAll(".mb-4");
        fields.Count.ShouldBe(3);
        
        // The fields should be rendered in order: Email (1), Name (2), Age (3)
        var labels = component.FindAll(".mud-input-label");
        labels[0].TextContent.ShouldContain("Email");
        labels[1].TextContent.ShouldContain("Name");
        labels[2].TextContent.ShouldContain("Age");
    }

    [Fact]
    public void FormCraftComponent_Should_Respect_Field_Visibility_Conditions()
    {
        // Arrange
        var model = new TestModel { ShowOptionalField = false };
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .AddField(x => x.Email)
                .WithLabel("Email")
                .VisibleWhen(m => m.ShowOptionalField)
            .Build();

        // Act
        var component = RenderComponent<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindAll(".mb-4").Count.ShouldBe(1); // Only Name field should be visible

        // Check that only the Name field is rendered
        var labels = component.FindAll(".mud-input-label");
        labels.Count.ShouldBe(1);
        labels[0].TextContent.ShouldContain("Name");
    }

    [Fact]
    public void FormCraftComponent_Should_Respect_IsVisible_Property()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .AddField(x => x.Email)
                .WithLabel("Email")
            .Build();

        // Manually set Email field as not visible
        config.Fields.First(f => f.FieldName == "Email").IsVisible = false;

        // Act
        var component = RenderComponent<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindAll(".mb-4").Count.ShouldBe(1); // Only Name field should be visible
        
        // Check that only the Name field is rendered
        var labels = component.FindAll(".mud-input-label");
        labels.Count.ShouldBe(1);
        labels[0].TextContent.ShouldContain("Name");
    }

    [Fact]
    public void FormCraftComponent_Should_Render_MudBlazor_Components()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .AddField(x => x.Age)
                .WithLabel("Age")
            .Build();

        // Act
        var component = RenderComponent<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        // Check that MudBlazor components are rendered
        component.FindAll(".mud-input").Count.ShouldBeGreaterThan(0);
        component.FindAll(".mud-input-label").Count.ShouldBe(2);
    }

    [Fact]
    public void FormCraftComponent_Should_Handle_Submit_Button()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        // Act
        var component = RenderComponent<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.ShowSubmitButton, true)
            .Add(p => p.SubmitButtonText, "Submit"));

        // Assert
        var submitButton = component.Find(".mud-button");
        submitButton.ShouldNotBeNull();
        submitButton.TextContent.ShouldContain("Submit");
    }

    [Fact(Skip = "MudBlazor component interactions require more complex setup")]
    public void FormCraftComponent_Should_Update_Model_Values()
    {
        // Arrange
        var model = new TestModel { Name = "Initial" };
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        // Act
        var component = RenderComponent<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Find the input element
        var inputElement = component.Find("input");
        inputElement.ShouldNotBeNull();
        
        // Get initial value
        inputElement.GetAttribute("value").ShouldBe("Initial");
        
        // Trigger value change
        inputElement.Change("Updated Value");

        // Assert - the model should be updated
        model.Name.ShouldBe("Updated Value");
    }

    [Fact]
    public void FormCraftComponent_Should_Include_DynamicFormValidator()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
                .Required()
            .Build();

        // Act
        var component = RenderComponent<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        // The component should contain validation components
        component.ShouldNotBeNull();
        // Check for DataAnnotationsValidator
        component.FindComponents<DataAnnotationsValidator>().Count.ShouldBe(1);
        // Check for DynamicFormValidator
        component.FindComponents<DynamicFormValidator<TestModel>>().Count.ShouldBe(1);
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool ShowOptionalField { get; set; }
    }
}