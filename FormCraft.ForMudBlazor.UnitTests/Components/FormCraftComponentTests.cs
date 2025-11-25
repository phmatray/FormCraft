namespace FormCraft.ForMudBlazor.UnitTests.Components;

public class FormCraftComponentTests : MudBlazorTestBase
{

    [Fact]
    public void FormCraftComponent_Should_Render_Form()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.ShouldNotBeNull();
        component.Find("form").ShouldNotBeNull();
    }

    [Fact]
    public void FormCraftComponent_Should_Render_All_Fields()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email"))
            .AddField(x => x.Age, field => field
                .WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var fields = component.FindAll(".mb-4");
        fields.Count.ShouldBe(3);
    }

    [Fact]
    public void FormCraftComponent_Should_Order_Fields_By_Order_Property()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithOrder(3))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithOrder(1))
            .AddField(x => x.Age, field => field
                .WithLabel("Age")
                .WithOrder(2))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var labels = component.FindAll(".mud-input-label");
        labels[0].TextContent.ShouldContain("Email");
        labels[1].TextContent.ShouldContain("Age");
        labels[2].TextContent.ShouldContain("Name");
    }

    [Fact]
    public void FormCraftComponent_Should_Hide_Fields_When_Visibility_Condition_False()
    {
        // Arrange
        var model = new TestModel { ShowEmail = false };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .VisibleWhen(m => m.ShowEmail))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var fields = component.FindAll(".mb-4");
        fields.Count.ShouldBe(1);

        var labels = component.FindAll(".mud-input-label");
        labels.Count.ShouldBe(1);
        labels[0].TextContent.ShouldContain("Name");
    }

    [Fact]
    public void FormCraftComponent_Should_Show_Fields_When_Visibility_Condition_True()
    {
        // Arrange
        var model = new TestModel { ShowEmail = true };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .VisibleWhen(m => m.ShowEmail))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var fields = component.FindAll(".mb-4");
        fields.Count.ShouldBe(2);
    }

    [Fact]
    public void FormCraftComponent_Should_Render_Submit_Button()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.ShowSubmitButton, true)
            .Add(p => p.SubmitButtonText, "Submit Form"));

        // Assert
        var submitButton = component.Find(".mud-button");
        submitButton.ShouldNotBeNull();
        submitButton.TextContent.ShouldContain("Submit Form");
    }

    [Fact]
    public void FormCraftComponent_Should_Hide_Submit_Button_When_ShowSubmitButton_Is_False()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.ShowSubmitButton, false));

        // Assert
        var buttons = component.FindAll("button[type='submit']");
        buttons.Count.ShouldBe(0);
    }

    [Fact]
    public void FormCraftComponent_Should_Not_Include_DataAnnotationsValidator()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponents<DataAnnotationsValidator>().Count.ShouldBe(0);
    }

    [Fact]
    public void FormCraftComponent_Should_Have_Novalidate_Attribute()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var form = component.Find("form");
        form.Attributes.FirstOrDefault(a => a.Name == "novalidate")?.Value.ShouldBe("novalidate");
    }

    [Fact]
    public void FormCraftComponent_Should_Render_Different_Field_Types()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .AddField(x => x.Age, field => field
                .WithLabel("Age"))
            .AddField(x => x.IsActive, field => field
                .WithLabel("Is Active"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponents<MudTextField<string>>().Count.ShouldBe(1);
        component.FindComponents<MudNumericField<int>>().Count.ShouldBe(1);
        component.FindComponents<MudCheckBox<bool>>().Count.ShouldBe(1);
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public bool ShowEmail { get; set; }
        public DateTime BirthDate { get; set; }
    }
}
