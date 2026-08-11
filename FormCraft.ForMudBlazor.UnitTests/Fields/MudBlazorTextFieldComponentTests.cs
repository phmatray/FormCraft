namespace FormCraft.ForMudBlazor.UnitTests.Fields;

public class MudBlazorTextFieldComponentTests : MudBlazorTestBase
{

    [Fact]
    public void TextField_Should_Render_With_Label()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Full Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.Label.ShouldBe("Full Name");
    }

    [Fact]
    public void TextField_Should_Render_With_Placeholder()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithPlaceholder("Enter your name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.Placeholder.ShouldBe("Enter your name");
    }

    [Fact]
    public void TextField_Should_Render_With_HelperText()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithHelpText("Please enter your full legal name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.HelperText.ShouldBe("Please enter your full legal name");
    }

    [Fact]
    public void TextField_Should_Render_As_ReadOnly()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .ReadOnly())
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.ReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void TextField_Should_Use_Email_InputType()
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
    public void TextField_Should_Use_Password_InputType()
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
    public void TextField_Should_Use_Tel_InputType()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithInputType("tel"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.InputType.ShouldBe(InputType.Telephone);
    }

    [Fact]
    public void TextField_Should_Render_As_Multiline()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Description, field => field
                .WithLabel("Description")
                .WithAttribute("Lines", 5))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.Lines.ShouldBe(5);
    }

    [Fact]
    public void TextField_Should_Have_MaxLength()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithAttribute("MaxLength", 100))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.MaxLength.ShouldBe(100);
    }

    [Fact]
    public void TextField_AsPassword_Should_Enable_Visibility_Toggle()
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
    public void TextField_WithAdornment_Should_Display_Icon()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.Start, Color.Primary))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.Adornment.ShouldBe(Adornment.Start);
        mudTextField.Instance.AdornmentIcon.ShouldBe(Icons.Material.Filled.Email);
        mudTextField.Instance.AdornmentColor.ShouldBe(Color.Primary);
    }

    /// <summary>
    /// The click selector for a rendered adornment icon. MudBlazor draws the adornment as a real
    /// button, so these tests click the DOM rather than invoking the callback directly — asserting
    /// the wiring end to end, which is the whole point of #192.
    /// </summary>
    private const string AdornmentButton = "button.mud-input-adornment-icon-button";

    [Fact]
    public void TextField_Adornment_Click_Should_Invoke_The_Configured_Handler()
    {
        // Arrange - before #192 the handler was accepted by WithAdornment and dropped, so this
        // clicked a live button that called nothing
        var received = new List<string?>();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: received.Add))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel { Email = "someone@example.com" })
            .Add(p => p.Configuration, config));

        // Act
        component.Find(AdornmentButton).Click();

        // Assert - fired once, and with the field's current value
        received.ShouldHaveSingleItem().ShouldBe("someone@example.com");
    }

    [Fact]
    public void TextField_Adornment_Click_Should_Pass_The_Value_Typed_By_The_User()
    {
        // Arrange - the handler receives the CURRENT value, not the one the model started with;
        // a search icon that searches the initial value would be useless
        var received = new List<string?>();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: received.Add))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel { Email = "before" })
            .Add(p => p.Configuration, config));

        // Act
        component.Find("input").Input("after");
        component.Find(AdornmentButton).Click();

        // Assert
        received.ShouldHaveSingleItem().ShouldBe("after");
    }

    [Fact]
    public void TextField_Adornment_Without_A_Handler_Should_Stay_Inert()
    {
        // Arrange - an adornment configured with no handler renders and clicks harmlessly
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.Start))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel { Email = "someone@example.com" })
            .Add(p => p.Configuration, config));

        // Act & Assert
        Should.NotThrow(() => component.Find(AdornmentButton).Click());
    }

    [Fact]
    public void TextField_Adornment_Reconfigured_Without_A_Handler_Should_Not_Fire_The_Old_One()
    {
        // Arrange - the helper-then-refine shape, asserted where it actually matters: clicking.
        // A partial overwrite would leave a decorative icon still running the first handler.
        var received = new List<string?>();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: received.Add)
                .WithAdornment(Icons.Material.Filled.Email, Adornment.End))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel { Email = "someone@example.com" })
            .Add(p => p.Configuration, config));

        // Act
        component.Find(AdornmentButton).Click();

        // Assert - the second call described a plain icon, so nothing runs
        received.ShouldBeEmpty();
    }

    [Fact]
    public void TextField_Password_Toggle_Should_Keep_Its_Own_Adornment_Handler()
    {
        // Arrange - the password toggle owns the adornment slot and must not be displaced by the
        // configuration lookup #192 adds
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword(enableVisibilityToggle: true))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, config));

        var textField = component.FindComponent<MudTextField<string>>();
        textField.Instance.InputType.ShouldBe(InputType.Password);

        // Act - clicking the toggle reveals the password
        component.Find(AdornmentButton).Click();

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType.ShouldBe(InputType.Text);
    }

    [Fact]
    public async Task TextField_Should_Update_Model_On_Input()
    {
        // Arrange
        var model = new TestModel { Name = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();

        // Act
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("John Doe"));

        // Assert
        model.Name.ShouldBe("John Doe");
    }

    [Fact]
    public async Task TextField_Should_Update_Model_On_ValueChanged()
    {
        // Arrange
        var model = new TestModel { Name = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();

        // Act - Simulate the ValueChanged event directly
        await mudTextField.InvokeAsync(() =>
            mudTextField.Instance.ValueChanged.InvokeAsync("Test Value"));

        // Assert
        model.Name.ShouldBe("Test Value");
    }

    [Fact]
    public async Task TextField_Should_Preserve_Value_After_Multiple_Inputs()
    {
        // Arrange
        var model = new TestModel { Name = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();

        // Act - Simulate typing character by character
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("H"));
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("He"));
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("Hel"));
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("Hell"));
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("Hello"));

        // Assert
        model.Name.ShouldBe("Hello");
        mudTextField.Instance.Value.ShouldBe("Hello");
    }

    [Fact]
    public async Task PasswordField_Should_Update_Model_On_Input()
    {
        // Arrange
        var model = new TestModel { Password = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword(enableVisibilityToggle: true))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();

        // Act
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("secret123"));

        // Assert
        model.Password.ShouldBe("secret123");
    }

    [Fact]
    public async Task PasswordField_Should_Preserve_Value_After_Multiple_Inputs()
    {
        // Arrange
        var model = new TestModel { Password = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword(enableVisibilityToggle: true))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();

        // Act - Simulate typing character by character
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("p"));
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("pa"));
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("pas"));
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("pass"));

        // Assert
        model.Password.ShouldBe("pass");
        mudTextField.Instance.Value.ShouldBe("pass");
    }

    [Fact]
    public async Task TextField_Value_Should_Reflect_In_Component_After_Update()
    {
        // Arrange
        var model = new TestModel { Name = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();

        // Act
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("Updated"));

        // Re-render to ensure component state is synced
        component.Render();

        // Assert - Both model and component should have the value
        model.Name.ShouldBe("Updated");

        // Re-find the component after render
        mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Value.ShouldBe("Updated");
    }

    [Fact]
    public void TextField_Should_Not_Be_Disabled_Or_ReadOnly_By_Default()
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
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.Disabled.ShouldBeFalse("TextField should not be disabled by default");
        mudTextField.Instance.ReadOnly.ShouldBeFalse("TextField should not be read-only by default");
    }

    [Fact]
    public void PasswordField_Should_Not_Be_Disabled_Or_ReadOnly_By_Default()
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
        mudTextField.Instance.Disabled.ShouldBeFalse("PasswordField should not be disabled by default");
        mudTextField.Instance.ReadOnly.ShouldBeFalse("PasswordField should not be read-only by default");
    }

    [Fact]
    public void TextField_Should_Display_Initial_Value()
    {
        // Arrange
        var model = new TestModel { Name = "Jane Doe" };
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
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.ShouldNotBeNull();
        mudTextField.Instance.Value.ShouldBe("Jane Doe");
    }

    [Fact]
    public void TextField_Should_Emit_Autocomplete_Attribute_On_Rendered_Input()
    {
        // Arrange - issue #153: WithAutocomplete must reach the rendered <input>
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithInputType("password")
                .WithAutocomplete("current-password"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var input = component.Find("input");
        input.GetAttribute("autocomplete").ShouldBe("current-password");
    }

    [Fact]
    public void TextField_Should_Not_Emit_Autocomplete_Attribute_When_Not_Configured()
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
        var input = component.Find("input");
        input.HasAttribute("autocomplete").ShouldBeFalse();
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
