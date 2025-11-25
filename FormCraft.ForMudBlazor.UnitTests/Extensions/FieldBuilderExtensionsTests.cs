namespace FormCraft.ForMudBlazor.UnitTests.Extensions;

public class FieldBuilderExtensionsTests : MudBlazorTestBase
{

    [Fact]
    public void AsPassword_Should_Set_Password_InputType()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword(enableVisibilityToggle: false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.InputType.ShouldBe(InputType.Password);
    }

    [Fact]
    public void AsPassword_With_Toggle_Should_Add_Adornment()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword(enableVisibilityToggle: true))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Adornment.ShouldBe(Adornment.End);
        mudTextField.Instance.AdornmentIcon.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void WithAdornment_Should_Set_Start_Icon()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.Start))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Adornment.ShouldBe(Adornment.Start);
        mudTextField.Instance.AdornmentIcon.ShouldBe(Icons.Material.Filled.Email);
    }

    [Fact]
    public void WithAdornment_Should_Set_End_Icon()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithAdornment(Icons.Material.Filled.Person, Adornment.End))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Adornment.ShouldBe(Adornment.End);
        mudTextField.Instance.AdornmentIcon.ShouldBe(Icons.Material.Filled.Person);
    }

    [Fact]
    public void WithAdornment_Should_Set_Color()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.Start, Color.Secondary))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.AdornmentColor.ShouldBe(Color.Secondary);
    }

    [Fact]
    public void AsSlider_Should_Configure_Slider_Field()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Rating, field => field
                .WithLabel("Rating")
                .AsSlider(min: 0.0, max: 10.0, step: 1.0, showValueLabel: true))
            .Build();

        // Assert - Configuration should be built without error
        config.ShouldNotBeNull();
        var field = config.Fields.First(f => f.FieldName == "Rating");
        field.AdditionalAttributes["Min"].ShouldBe(0.0);
        field.AdditionalAttributes["Max"].ShouldBe(10.0);
        field.AdditionalAttributes["Step"].ShouldBe(1.0);
        field.AdditionalAttributes["ShowValueLabel"].ShouldBe(true);
    }

    [Fact]
    public void ChainedExtensions_Should_Work_Together()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithPlaceholder("Enter your password")
                .AsPassword(enableVisibilityToggle: true)
                .Required("Password is required"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Label.ShouldBe("Password");
        mudTextField.Instance.Placeholder.ShouldBe("Enter your password");
        mudTextField.Instance.InputType.ShouldBe(InputType.Password);
        mudTextField.Instance.Adornment.ShouldBe(Adornment.End);
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public double Rating { get; set; }
    }
}
