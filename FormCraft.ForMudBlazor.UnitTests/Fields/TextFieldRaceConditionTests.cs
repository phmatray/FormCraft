namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests specifically designed to reproduce and verify the fix for the
/// race condition where characters disappear when typing rapidly.
///
/// The race condition occurs when:
/// 1. User types a character
/// 2. HandleValueChanged updates local state and notifies parent
/// 3. Parent calls StateHasChanged, triggering child's OnParametersSet
/// 4. OnParametersSet incorrectly reloads stale model value, overwriting user input
///
/// These tests simulate this by calling component.Render() between value changes.
/// </summary>
public class TextFieldRaceConditionTests : MudBlazorTestBase
{
    /// <summary>
    /// This test reproduces the race condition by:
    /// 1. Triggering a value change
    /// 2. Forcing a re-render (which triggers OnParametersSet)
    /// 3. Triggering another value change before the first fully completes
    /// </summary>
    [Fact]
    public async Task TextField_Should_Not_Lose_Characters_When_Parent_ReRenders_During_Typing()
    {
        // Arrange
        var model = new TestModel { Name = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();

        // Act - Simulate rapid typing with interleaved re-renders
        // Type "H"
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("H"));

        // Force a re-render (simulating what happens when parent calls StateHasChanged)
        component.Render();

        // Type "He" before the render cycle fully completes
        mudTextField = component.FindComponent<MudTextField<string>>();
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("He"));

        // Another re-render
        component.Render();

        // Type "Hel"
        mudTextField = component.FindComponent<MudTextField<string>>();
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("Hel"));

        // Assert - The value should be "Hel", not reverted to an earlier state
        model.Name.ShouldBe("Hel");

        // Re-find after renders
        mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Value.ShouldBe("Hel");
    }

    [Fact]
    public async Task TextField_Should_Preserve_Value_When_OnParametersSet_Fires_During_Input()
    {
        // Arrange
        var model = new TestModel { Name = "Initial" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();

        // Verify initial state
        mudTextField.Instance.Value.ShouldBe("Initial");

        // Act - Simulate typing that replaces the initial value
        await mudTextField.InvokeAsync(() => mudTextField.Instance.ValueChanged.InvokeAsync("New"));

        // Force parent to re-render (this triggers OnParametersSet on the child)
        component.Render();

        // Assert - The new value should persist, not revert to "Initial"
        model.Name.ShouldBe("New");
        mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Value.ShouldBe("New");
    }

    [Fact]
    public async Task TextField_Should_Handle_Rapid_Sequential_ValueChanges_Without_Loss()
    {
        // Arrange
        var model = new TestModel { Name = "" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();

        // Act - Rapid sequential value changes with re-renders (simulating fast typing)
        var expectedValues = new[] { "T", "Te", "Tes", "Test", "Test " };
        foreach (var value in expectedValues)
        {
            mudTextField = component.FindComponent<MudTextField<string>>();
            await mudTextField.InvokeAsync(() =>
                mudTextField.Instance.ValueChanged.InvokeAsync(value));

            // Simulate parent re-render after each keystroke
            component.Render();
        }

        // Assert - Final value should be "Test "
        model.Name.ShouldBe("Test ");
        mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Value.ShouldBe("Test ");
    }

    [Fact]
    public async Task TextField_Should_Not_Revert_To_Model_Value_After_User_Edit()
    {
        // Arrange - Start with a value in the model
        var model = new TestModel { Name = "OldValue" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudTextField = component.FindComponent<MudTextField<string>>();

        // Verify initial state
        mudTextField.Instance.Value.ShouldBe("OldValue");

        // Act - User starts typing a new value
        await mudTextField.InvokeAsync(() =>
            mudTextField.Instance.ValueChanged.InvokeAsync("N"));

        // Simulate parent re-render before model is updated
        component.Render();

        mudTextField = component.FindComponent<MudTextField<string>>();
        await mudTextField.InvokeAsync(() =>
            mudTextField.Instance.ValueChanged.InvokeAsync("Ne"));
        component.Render();

        mudTextField = component.FindComponent<MudTextField<string>>();
        await mudTextField.InvokeAsync(() =>
            mudTextField.Instance.ValueChanged.InvokeAsync("New"));
        component.Render();

        // Assert
        model.Name.ShouldBe("New");
        mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Value.ShouldBe("New");
    }

    [Fact]
    public async Task PasswordField_Should_Not_Lose_Characters_When_Parent_ReRenders()
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

        // Act - Simulate typing password with re-renders
        await mudTextField.InvokeAsync(() =>
            mudTextField.Instance.ValueChanged.InvokeAsync("p"));
        component.Render();

        mudTextField = component.FindComponent<MudTextField<string>>();
        await mudTextField.InvokeAsync(() =>
            mudTextField.Instance.ValueChanged.InvokeAsync("pa"));
        component.Render();

        mudTextField = component.FindComponent<MudTextField<string>>();
        await mudTextField.InvokeAsync(() =>
            mudTextField.Instance.ValueChanged.InvokeAsync("pas"));
        component.Render();

        mudTextField = component.FindComponent<MudTextField<string>>();
        await mudTextField.InvokeAsync(() =>
            mudTextField.Instance.ValueChanged.InvokeAsync("pass"));

        // Assert
        model.Password.ShouldBe("pass");
        mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Value.ShouldBe("pass");
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
