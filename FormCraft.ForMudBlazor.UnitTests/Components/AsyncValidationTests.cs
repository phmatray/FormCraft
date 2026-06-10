namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Regression tests for the submit-time validation path: truly asynchronous
/// validators must complete (and block submission) before OnValidSubmit fires,
/// and hidden fields must not be validated.
/// </summary>
public class AsyncValidationTests : MudBlazorTestBase
{
    [Fact]
    public async Task Submit_Should_Not_Invoke_OnValidSubmit_When_Async_Validator_Fails()
    {
        // Arrange - validator always fails, but only after yielding
        var submitted = false;
        var model = new TestModel { Name = "taken" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithAsyncValidator(async _ =>
                {
                    await Task.Delay(50);
                    return false;
                }, "Name is taken"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.OnValidSubmit, (TestModel _) => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        submitted.ShouldBeFalse();
        component.WaitForAssertion(() =>
            component.Markup.ShouldContain("Name is taken"));
    }

    [Fact]
    public async Task Submit_Should_Invoke_OnValidSubmit_When_Async_Validator_Passes()
    {
        // Arrange
        var submitted = false;
        var model = new TestModel { Name = "available" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithAsyncValidator(async _ =>
                {
                    await Task.Delay(50);
                    return true;
                }, "Name is taken"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.OnValidSubmit, (TestModel _) => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        component.WaitForAssertion(() => submitted.ShouldBeTrue());
    }

    [Fact]
    public async Task Submit_Should_Skip_Validators_Of_Hidden_Fields()
    {
        // Arrange - Email is required but hidden; its validator must not block submit
        var submitted = false;
        var model = new TestModel { Name = "John", ShowEmail = false };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .Required("Name is required"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .Required("Email is required")
                .VisibleWhen(m => m.ShowEmail))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.OnValidSubmit, (TestModel _) => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        component.WaitForAssertion(() => submitted.ShouldBeTrue());
    }

    [Fact]
    public void Validation_Error_Should_Clear_When_Field_Becomes_Valid()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .Required("Name is required"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        component.Find("form").Submit();
        component.WaitForAssertion(() =>
            component.Markup.ShouldContain("Name is required"));

        // Act - fix the field
        component.FindAll("input")[0].Input("John");

        // Assert - the stale error must clear without another submit
        component.WaitForAssertion(() =>
            component.Markup.ShouldNotContain("Name is required"));
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool ShowEmail { get; set; }
    }
}
