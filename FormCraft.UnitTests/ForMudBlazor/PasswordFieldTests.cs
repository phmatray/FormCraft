using Bunit;
using MudBlazor;
using MudBlazor.Services;
using Shouldly;
using Xunit;

namespace FormCraft.UnitTests.ForMudBlazor;

public class PasswordFieldTests : BunitContext
{
    public class TestModel
    {
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public PasswordFieldTests()
    {
        Services.AddFormCraft();
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Password_Field_Should_Use_Password_InputType_When_WithInputType_Is_Called()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithInputType("password"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.InputType.ShouldBe(InputType.Password);
    }

    [Fact]
    public void Password_Field_Should_Use_Password_InputType_When_AsPassword_Is_Called()
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
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.InputType.ShouldBe(InputType.Password);
    }

    [Fact]
    public void Password_Field_Should_Have_Visibility_Toggle_When_Enabled()
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
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.InputType.ShouldBe(InputType.Password);
        mudTextField.Instance.Adornment.ShouldBe(Adornment.End);
        mudTextField.Instance.AdornmentIcon.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Email_Field_Should_Use_Email_InputType()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithInputType("email"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.InputType.ShouldBe(InputType.Email);
    }

    [Fact]
    public void Field_With_Custom_Adornment_Should_Display_Icon()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Username, field => field
                .WithLabel("Username")
                .WithAdornment(Icons.Material.Filled.Person, Adornment.Start, Color.Primary))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.Adornment.ShouldBe(Adornment.Start);
        mudTextField.Instance.AdornmentIcon.ShouldBe(Icons.Material.Filled.Person);
        mudTextField.Instance.AdornmentColor.ShouldBe(Color.Primary);
    }

    [Fact]
    public void Password_Field_With_Toggle_Should_Override_Custom_Adornment()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword(enableVisibilityToggle: true)
                .WithAdornment(Icons.Material.Filled.Lock, Adornment.Start, Color.Primary))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - Password toggle should take priority
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.InputType.ShouldBe(InputType.Password);
        mudTextField.Instance.Adornment.ShouldBe(Adornment.End); // Toggle always at end
        mudTextField.Instance.AdornmentIcon.ShouldBe(Icons.Material.Filled.Visibility); // Not Lock
    }

    [Fact]
    public async Task Password_Field_Should_Update_Model_When_User_Types()
    {
        // Arrange
        var model = new TestModel { Password = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithInputType("password"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();

        // Act - Simulate user typing in the password field by invoking ValueChanged
        mudTextField.Instance.Value.ShouldBe(""); // Initially empty
        await mudTextField.InvokeAsync(() => mudTextField.Instance.SetText("MySecretPassword123"));

        // Assert - Model should be updated with the new value
        model.Password.ShouldBe("MySecretPassword123");
    }

    [Fact]
    public async Task Email_Field_Should_Update_Model_When_User_Types()
    {
        // Arrange
        var model = new TestModel { Email = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithInputType("email"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();

        // Act - Simulate user typing in the email field
        mudTextField.Instance.Value.ShouldBe(""); // Initially empty
        await mudTextField.InvokeAsync(() => mudTextField.Instance.SetText("user@example.com"));

        // Assert - Model should be updated with the new value
        model.Email.ShouldBe("user@example.com");
    }

    [Fact]
    public async Task Username_Field_Should_Update_Model_When_User_Types()
    {
        // Arrange
        var model = new TestModel { Username = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Username, field => field
                .WithLabel("Username"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();

        // Act - Simulate user typing
        mudTextField.Instance.Value.ShouldBe(""); // Initially empty
        await mudTextField.InvokeAsync(() => mudTextField.Instance.SetText("john_doe"));

        // Assert - Model should be updated
        model.Username.ShouldBe("john_doe");
    }
}