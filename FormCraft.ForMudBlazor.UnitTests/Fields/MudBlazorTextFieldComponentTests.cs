using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

public class MudBlazorTextFieldComponentTests : MudBlazorTestBase
{
    private readonly CapturingLoggerProvider _logs = new();

    public MudBlazorTextFieldComponentTests()
    {
        Services.AddLogging(builder => builder.AddProvider(_logs));
    }

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
    public void TextField_Adornment_Without_A_Handler_Should_Render_A_Plain_Icon()
    {
        // Arrange - an adornment configured with no handler is decorative, so it must not be a
        // focus stop. Before #216 this path bound OnAdornmentClick unconditionally, so MudBlazor
        // drew a real <button>: inert to click, but in the tab order for keyboard and screen-reader
        // users. The collection path has always drawn a plain icon here.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.Start))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel { Email = "someone@example.com" })
            .Add(p => p.Configuration, config));

        // Assert - the adornment still renders; it just isn't clickable or focusable
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.Start);
        textField.AdornmentIcon.ShouldBe(Icons.Material.Filled.Email);
        textField.OnAdornmentClick.HasDelegate.ShouldBeFalse();
        component.FindAll(AdornmentButton).ShouldBeEmpty();
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

        // Assert - the second call described a plain icon, so there is nothing left to click and
        // the first handler cannot run. Before #216 a button survived the reconfiguration and this
        // asserted the weaker "clicking it fires nothing"; no button at all is the stronger claim.
        component.FindAll(AdornmentButton).ShouldBeEmpty();
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

    [Theory]
    [InlineData("number")]
    [InlineData("date")]
    [InlineData("time")]
    public void TextField_Should_Map_The_Numeric_And_Temporal_Input_Types(string configured)
    {
        // Arrange - #210. `number`, `date` and `time` fell through to InputType.Text, so a string
        // field configured with one lost its mobile keypad or native picker with nothing reported.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Value")
                .WithInputType(configured))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - on the element, which is what selects the keypad/picker.
        component.Find("input").GetAttribute("type").ShouldBe(configured);
    }

    [Fact]
    public void An_AutoGenerated_Numeric_Field_Should_Still_Render_A_Numeric_Component()
    {
        // Arrange - the scope correction for #210. AutoFormBuilderExtensions emits "number" for
        // numeric properties, which reads as though widening TextInputTypeMap changes auto-generated
        // forms. It does not: an int property is rendered by MudNumericField, which never consults
        // that map at all — the map is reached only from the text path, i.e. `string` fields. Pinned
        // so the scope of this change is measured rather than argued.
        var model = new NumericModel();
        var config = FormBuilder<NumericModel>.Create().AddFieldsAuto().Build();

        // Act
        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponents<MudNumericField<int>>().ShouldNotBeEmpty();
        component.FindComponents<MudTextField<string>>().ShouldBeEmpty();
    }

    [Fact]
    public void TextField_With_AsPassword_And_Lines_Should_Stay_Masked()
    {
        // Arrange - #207. MudBlazor swaps to a <textarea> past Lines > 1, and a textarea carries no
        // `type` attribute, so the masking vanished and the password was rendered in clear text.
        // A masked textarea does not exist, so `.AsPassword()` — an explicit security request —
        // wins over `Lines`, which is presentation.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword()
                .AsTextArea(lines: 4))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - assert the rendered element, not merely MudTextField.InputType: the parameter
        // was already being forwarded correctly before this fix, and the field still displayed the
        // characters, because the element MudBlazor chose ignored it.
        component.FindAll("textarea").ShouldBeEmpty();
        component.Find("input").GetAttribute("type").ShouldBe("password");
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(1);
    }

    [Fact]
    public void TextField_With_Lines_Then_AsPassword_Should_Stay_Masked()
    {
        // Arrange - the other call order. AsTextArea lives in the core FormCraft project and
        // AsPassword in FormCraft.ForMudBlazor, so no builder method ever observes both settings;
        // reconciling them is necessarily the render path's job, and it must not depend on order.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsTextArea(lines: 4)
                .AsPassword())
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindAll("textarea").ShouldBeEmpty();
        component.Find("input").GetAttribute("type").ShouldBe("password");
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(1);
    }

    [Fact]
    public void TextField_With_Lines_And_No_Password_Should_Still_Render_A_Textarea()
    {
        // Arrange - the guard on the guard. The fix must be scoped to masked fields only; an
        // ordinary multi-line field is the overwhelmingly common case and must be untouched.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Description, field => field
                .WithLabel("Description")
                .AsTextArea(lines: 4))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(4);
        component.FindAll("textarea").Count.ShouldBe(1);
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Bind_A_Pattern_Mask()
    {
        // Arrange - #211. `GetMask()` was a stub that returned null with a "For now" comment, and the
        // .razor never bound it, so `.WithAttribute("Mask", …)` was read into a property and dropped.
        // The attribute looked supported and did nothing.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - the pattern, not the instance: MudBlazor's IMask carries the mask string, and it
        // is the only part a caller configured.
        var mask = component.FindComponent<MudTextField<string>>().Instance.Mask;
        mask.ShouldBeOfType<PatternMask>();
        mask.Mask.ShouldBe("(000) 000-0000");
    }

    [Fact]
    public void TextField_Without_A_Mask_Should_Bind_No_Mask()
    {
        // Arrange - the guard on the guard. MudTextField swaps its whole input implementation for a
        // MudMask once Mask is non-null, which also makes it ignore MaxLines and Sizing. Resolving an
        // empty pattern into a PatternMask("") rather than null would therefore reroute every
        // unmasked field in the library through a different component.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask.ShouldBeNull();
    }

    [Fact]
    public void TextField_With_A_Mask_And_Lines_Should_Render_A_Masked_Textarea()
    {
        // Arrange - #211, the mask/multi-line interaction. Unlike `.AsPassword()` + `Lines` (#207),
        // this combination IS honoured: MudTextField picks its input implementation on `Mask == null`
        // alone and never consults `Lines`, so a masked field always renders a MudMask — and MudMask
        // itself opens a <textarea> past one line while still running its masking. So both settings
        // survive, and neither is dropped. Measured rather than assumed, because the intuition that a
        // textarea "cannot be masked" is wrong here and was briefly written into this repo's docs.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Description, field => field
                .WithLabel("Description")
                .AsTextArea(lines: 4)
                .WithAttribute("Mask", "0000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - the textarea is the MASK's own element, not a fallback that discarded it.
        component.FindAll("textarea").Count.ShouldBe(1);
        component.FindAll("input").ShouldBeEmpty();
        component.FindComponents<MudMask>().Count.ShouldBe(1);
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Write_The_Masked_Text_To_The_Model()
    {
        // Arrange - the feature's actual contract, end to end. Every other mask test in this repo
        // stops at "the right IMask object is attached", which would still pass if the
        // oninput → MudMask → MudTextField → OnLocalValueChanged → Context.OnValueChanged chain were
        // broken anywhere along it. This drives the rendered element instead.
        //
        // Note WHAT lands in the model: the masked text, delimiters and all, because
        // PatternMask.CleanDelimiters defaults to false and FormCraft does not change it. A model
        // bound to a masked field therefore stores "(555) 123-4567", not "5551234567" — which is a
        // behaviour change for anyone whose storage or validation expects raw digits.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act
        component.Find("input").Input("5551234567");

        // Assert
        model.Phone.ShouldBe("(555) 123-4567");
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Blank_An_Existing_Value_That_Does_Not_Fit()
    {
        // Arrange - the upgrade hazard, pinned so it is known rather than discovered. A field whose
        // stored value does not conform to the pattern renders EMPTY, while the model quietly keeps
        // the original: MudBlazor's first parameter pass runs the value through the mask to build the
        // display text, and does not write the result back. The user sees a blank field, submits
        // without touching it, and the non-conforming value survives.
        //
        // This is not new behaviour in MudBlazor — it is newly REACHABLE, because until #211 the Mask
        // attribute did nothing at all. Documented in the README's behaviour-change note; a
        // diagnostic for it is filed as a follow-up rather than built here.
        var model = new TestModel { Phone = "N/A" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find("input").GetAttribute("value").ShouldBeNullOrEmpty();
        model.Phone.ShouldBe("N/A");
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Warn_When_It_Blanks_A_Stored_Value()
    {
        // Arrange - the other half of the test above (#266). Pinning the blanking made the hazard
        // known to whoever reads the suite; this makes it known to the developer whose form is doing
        // it, which is the person who can act on it. The message has to name the field AND the
        // pattern: a form of thirty fields otherwise sends them hunting.
        var model = new TestModel { Phone = "N/A" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Phone");
        warnings[0].ShouldContain("(000) 000-0000");
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Not_Warn_When_It_Reformats_A_Stored_Value()
    {
        // Arrange - the load-bearing negative case. A conforming value is reformatted rather than
        // rejected, which is the mask doing its job; warning here would fire on every correctly
        // masked field in every form, and a diagnostic that cries on the happy path gets muted --
        // taking the real signal with it.
        var model = new TestModel { Phone = "5551234567" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - the value really did survive the mask, so this is a no-warning case rather than a
        // second blanking that happened to go unreported.
        component.Find("input").GetAttribute("value").ShouldBe("(555) 123-4567");
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Not_Warn_For_An_Empty_Value()
    {
        // Arrange - nothing was stored, so nothing was lost. This is the overwhelmingly common state
        // of a masked field before anyone types into it, and it must stay silent.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TextField_Without_A_Mask_Should_Not_Warn_For_Any_Value()
    {
        // Arrange - no mask means no masking, so no value can be rejected by one. Guards against a
        // rule that keys off the value alone.
        var model = new TestModel { Phone = "N/A" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field.WithLabel("Phone"))
            .Build();

        // Act
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Warn_When_A_Rejected_Value_Arrives_After_First_Render()
    {
        // Arrange - #283(a). The canonical legacy-data case, and the one #266 could not see: the
        // field renders before the fetch resolves, so CurrentValue is empty at OnInitialized and the
        // diagnostic returns early. The value then lands, the field renders blank, the model keeps
        // "N/A" -- the exact divergence #266 exists to report, unreported.
        //
        //     protected override async Task OnInitializedAsync()
        //         => _model.Phone = await _api.GetPhoneAsync();   // "N/A"
        //
        // The spec's stated reason for the initial-value framing -- "a field the user legitimately
        // cleared must not warn on a later render" -- is already covered independently, because the
        // rule requires a NON-BLANK stored value. Clearing a field can never satisfy it, so emitting
        // later costs nothing.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Nothing was stored at first render, so there was nothing to report yet. Asserted rather
        // than assumed: a warning here would make the post-arrival count below meaningless.
        _logs.Warnings.ShouldBeEmpty();

        // Act - the fetch resolves and the model is populated from outside the component.
        model.Phone = "N/A";
        component.Render();

        // Assert - reported once, naming the field and the pattern like the OnInitialized path does.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Phone");
        warnings[0].ShouldContain("(000) 000-0000");
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Warn_Only_Once_Across_Later_Renders()
    {
        // Arrange - #283(a). Moving the emit off OnInitialized removes the thing that used to make
        // "once per component lifetime" true for free. A plain field has no CollectionItemFieldScope
        // and therefore no latch, so without a component-level one the warning would re-fire on every
        // external model change -- flooding the console of the developer it is meant to help, which
        // is how a useful diagnostic gets muted.
        var model = new TestModel { Phone = "N/A" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act - a second non-conforming value arrives, then a plain re-render.
        model.Phone = "unknown";
        component.Render();
        component.Render();

        // Assert
        _logs.Warnings.Count.ShouldBe(1);
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Not_Warn_When_The_User_Types_Into_It()
    {
        // Arrange - #283(a). The load-bearing negative for the widened emit point: this reports
        // STORED data, never live editing. A user part-way through typing holds a value the mask has
        // not finished formatting, and re-checking on every keystroke would report the field being
        // filled in correctly. ShouldReloadValue() is what tells an external model change apart from
        // an in-flight edit, and this pins that the emit sits downstream of it.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act - a partial entry, then a completed one.
        component.Find("input").Input("555");
        component.Find("input").Input("5551234567");

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Not_Warn_When_The_User_Clears_It()
    {
        // Arrange - #283(a). The case the "initial value" framing was chosen to protect, kept
        // honest now that the framing is gone: a field the user emptied on purpose must stay silent.
        // It does so through the rule rather than the emit point -- Applies requires a non-blank
        // stored value, and a cleared field has none.
        var model = new TestModel { Phone = "5551234567" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act - cleared from outside the component, the harsher of the two ways to reach a blank
        // value: it goes through the external-change path the new emit point sits on.
        model.Phone = string.Empty;
        component.Render();

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Warn_When_It_Discards_Part_Of_A_Stored_Value()
    {
        // Arrange - #283(b), end-to-end against the real MudBlazor mask rather than the rule alone.
        // "+1 555 123 4567" does not blank: it renders "(155) 512-3456", a perfectly plausible phone
        // number that is NOT the one stored. The mask consumed the country code as the area code and
        // dropped the final digit off the end. Nothing on screen looks wrong, which is exactly why
        // this needs reporting more than the blank case does.
        var model = new TestModel { Phone = "+1 555 123 4567" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - the divergence is real: the input shows one number, the model keeps another.
        component.Find("input").GetAttribute("value").ShouldBe("(155) 512-3456");
        model.Phone.ShouldBe("+1 555 123 4567");

        // And the message names what is DISPLAYED, not "renders empty" -- the developer has to be
        // able to recognise the field, and this one looks entirely healthy.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Phone");
        warnings[0].ShouldContain("(000) 000-0000");
        warnings[0].ShouldContain("(155) 512-3456");
        warnings[0].ShouldNotContain("renders empty");
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Warn_When_It_Discards_Leading_Junk()
    {
        // Arrange - #283(b), the second row of the issue's table. Here the SURVIVING digits are the
        // right ones, so the rendered number is correct; what was silently dropped is the "N/A"
        // marker that told a reader the record had no usable phone number.
        var model = new TestModel { Phone = "N/A5551234567" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find("input").GetAttribute("value").ShouldBe("(555) 123-4567");
        _logs.Warnings.Count.ShouldBe(1);
    }

    [Fact]
    public void TextField_With_A_Mask_Should_Not_Warn_When_The_Stored_Value_Has_Its_Own_Separators()
    {
        // Arrange - #283(b)'s load-bearing negative, and the shape of real legacy data. The stored
        // value is already punctuated, just differently from the mask. Every significant character
        // survives, so this is a reformat and must stay silent -- a rule comparing raw strings, or
        // asking whether the stored value survives as a subsequence of the rendered one, reports it
        // as a discard and fires on data that is perfectly fine.
        var model = new TestModel { Phone = "555 123 4567" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find("input").GetAttribute("value").ShouldBe("(555) 123-4567");
        _logs.Warnings.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextField_With_A_CleanDelimiters_Mask_Should_Not_Warn_On_A_Reformat(bool cleanDelimiters)
    {
        // Arrange - the #265 edge case the spec called out: a mask that strips its own literals out
        // of the value it reports must not read as having DISCARDED them. It cannot, because the rule
        // removes the mask's literals from both sides before comparing -- so the verdict is identical
        // whichever way the flag is set, even though the two write different things to the model.
        // Asserted through the builder rather than on the rule alone, because CleanDelimiters is a
        // setting a caller chooses and this is the path they take.
        var model = new TestModel { Phone = "5551234567" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask("(000) 000-0000", cleanDelimiters: cleanDelimiters))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find("input").GetAttribute("value").ShouldBe("(555) 123-4567");
        _logs.Warnings.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextField_With_A_CleanDelimiters_Mask_Should_Still_Warn_On_A_Discard(bool cleanDelimiters)
    {
        // Arrange - the other half: CleanDelimiters must not SUPPRESS a real discard either. Same
        // stored value as the discard test above, run through the typed builder with the flag both
        // ways.
        var model = new TestModel { Phone = "N/A5551234567" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask("(000) 000-0000", cleanDelimiters: cleanDelimiters))
            .Build();

        // Act
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        _logs.Warnings.Count.ShouldBe(1);
    }

    [Fact]
    public void TextField_With_A_Blank_Mask_Should_Bind_No_Mask()
    {
        // Arrange - a whitespace-only pattern is not "a mask of one space", it is a
        // configuration accident (a trimmed-to-blank setting, an empty config binding). Honouring it
        // literally produces a field that routes through MudMask and then accepts no input at all,
        // which is strictly worse than ignoring it.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", "   "))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask.ShouldBeNull();
    }

    [Fact]
    public void TextField_With_WithMask_Should_Bind_A_PatternMask_Keeping_Delimiters()
    {
        // Arrange - the typed builder replacing `.WithAttribute("Mask", …)` (#265). The default must
        // reproduce #211's behaviour exactly, delimiters and all: this overload is additive, and a
        // caller migrating off the magic string must not find the model quietly storing something
        // else afterwards.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask("0000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mask = component.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<PatternMask>();
        mask.Mask.ShouldBe("0000-0000");
        mask.CleanDelimiters.ShouldBeFalse();
    }

    [Fact]
    public void TextField_With_WithMask_CleanDelimiters_Should_Bind_A_Stripping_PatternMask()
    {
        // Arrange - the knob #211 left unreachable. CleanDelimiters is what decides whether
        // GetCleanText() strips the literals, and with no way to set it the model always received
        // the delimited text — punctuation and all — with no opt-out.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask("0000-0000", cleanDelimiters: true))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mask = component.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<PatternMask>();
        mask.Mask.ShouldBe("0000-0000");
        mask.CleanDelimiters.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TextField_With_A_Blank_WithMask_Pattern_Should_Bind_No_Mask(string pattern)
    {
        // Arrange - the typed builder inherits #211's blank rule rather than restating it: a blank
        // pattern reaching PatternMask("") would reroute an unmasked field through MudMask and drop
        // MaxLines with it. Asserted through the new entry point because that is a second caller of
        // Resolve, and a rule enforced on only one of them is the divergence class this repo keeps
        // closing.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask(pattern))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask.ShouldBeNull();
    }

    [Fact]
    public void TextField_With_A_Blank_WithMask_Pattern_Should_Ignore_CleanDelimiters()
    {
        // Arrange - cleanDelimiters is meaningless without a pattern. It must not resurrect a mask
        // the blank rule just suppressed, or "no mask + an option" would render a field that
        // accepts no input — the exact outcome the blank rule exists to prevent.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask("  ", cleanDelimiters: true))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask.ShouldBeNull();
    }

    [Fact]
    public void TextField_With_A_Supplied_Mask_Should_Bind_That_Mask()
    {
        // Arrange - the third gap #265 closes. `.WithAttribute("Mask", new RegexMask(…))` is the
        // natural thing for a MudBlazor user to write and it compiled, built and rendered while
        // doing nothing: both paths read the attribute as string?, whose `value is T` test fails for
        // an IMask and falls back to null. So RegexMask, BlockMask and MultiMask were unreachable.
        //
        // The regex is open-ended (`{0,4}`, not `{4}`) because MudBlazor matches it against partial
        // input: an exact quantifier never matches a shorter prefix and blocks every keystroke.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask(() => new RegexMask("^[0-9]{0,4}$")))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - the type and the pattern, not the instance.
        var mask = component.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<RegexMask>();
        mask.Mask.ShouldBe("^[0-9]{0,4}$");
    }

    [Fact]
    public void TextField_With_A_Pattern_After_A_Supplied_Mask_Should_Use_The_Pattern()
    {
        // Arrange - the last WithMask on a field wins, in BOTH orders. The two overloads write
        // different attribute keys, so without each clearing the other's the answer would be fixed
        // by precedence rather than by call order — and a caller refining a shared helper's mask
        // would find their own later, more specific call silently ignored. This is the order that
        // would break.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask(() => new RegexMask("^[0-9]{0,4}$"))
                .WithMask("0000-0000"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<PatternMask>()
            .Mask.ShouldBe("0000-0000");
    }

    [Fact]
    public void TextField_With_A_Supplied_Mask_After_A_Pattern_Should_Use_The_Supplied_Mask()
    {
        // Arrange - the mirror of the test above, so the rule is pinned symmetrically rather than
        // in the one direction where precedence and last-write-wins happen to agree.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask("0000-0000")
                .WithMask(() => new RegexMask("^[0-9]{0,4}$")))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask
            .ShouldBeOfType<RegexMask>()
            .Mask.ShouldBe("^[0-9]{0,4}$");
    }

    [Fact]
    public void TextField_With_A_Factory_Producing_A_Blank_Mask_Should_Bind_No_Mask()
    {
        // Arrange - the blank rule is about the OUTCOME, not about which overload produced it. A
        // factory building its pattern from configuration can hand back PatternMask(""), and letting
        // that through would reroute an otherwise ordinary field via MudMask and drop MaxLines with
        // it — the same damage the string overload's guard prevents.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask(() => new PatternMask("")))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask.ShouldBeNull();
    }

    [Fact]
    public void TextField_With_CleanDelimiters_Should_Write_The_Stripped_Text_To_The_Model()
    {
        // Arrange - the feature's headline promise, asserted end-to-end rather than on the bound
        // parameter. `CleanDelimiters == true` on the mask is a claim about a chain FormCraft does
        // not own — PatternMask.GetCleanText, MudMask's ConvertGet, MudTextField's masked-value
        // callback, then FormCraft's binding — and the README states the OUTCOME, so that is what
        // has to be pinned. Its delimited twin is TextField_With_A_Mask_Should_Write_The_Masked_
        // Text_To_The_Model.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithMask("(000) 000-0000", cleanDelimiters: true))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act
        component.Find("input").Input("5551234567");

        // Assert
        model.Phone.ShouldBe("5551234567");
    }

    private class NumericModel
    {
        public int Quantity { get; set; }
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
