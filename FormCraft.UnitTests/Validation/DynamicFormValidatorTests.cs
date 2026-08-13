namespace FormCraft.UnitTests.Validation;

/// <summary>
/// Behavioural tests for <see cref="DynamicFormValidator{TModel}"/> now that it lives in core (#279).
/// </summary>
/// <remarks>
/// <para>
/// The component is 242 lines that reference nothing from any UI framework — only
/// <c>Microsoft.AspNetCore.Components</c> — yet it shipped inside <c>FormCraft.ForMudBlazor</c>. That
/// placement is what forced <c>FormCraft.ForFluentUI</c> to write its own copy covering the
/// non-collection half; #279 moves the original to core and deletes the copy.
/// </para>
/// <para>
/// The declaring-assembly assertion is load-bearing for the same reason as in
/// <c>NativeRequiredBuilderTests</c>: this project references <c>FormCraft.ForMudBlazor</c> and
/// globally imports its namespace, so every behavioural assertion below would have bound to the old
/// MudBlazor type and passed without the move. Only the assembly assertion can tell them apart.
/// </para>
/// <para>
/// The existing <c>FormCraft.UnitTests.Components.DynamicFormValidatorTests</c> covers property
/// setting and disposal; these cover what the component actually does, which is what a move has to
/// preserve.
/// </para>
/// </remarks>
public class DynamicFormValidatorTests : BunitContext
{
    public DynamicFormValidatorTests()
    {
        Services.AddFormCraft();
    }

    [Fact]
    public void DynamicFormValidator_Should_Be_Declared_By_The_Core_Assembly()
    {
        // The point of the move: an adapter that does not reference MudBlazor must still be able to
        // use it.
        typeof(DynamicFormValidator<TestModel>).Assembly.ShouldBe(typeof(FormBuilder<>).Assembly);
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Report_A_Required_Field_As_Invalid()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.Required("Name is required"))
            .Build();

        var validator = RenderValidator(editContext, config);

        // Act
        var isValid = await validator.Instance.ValidateModelAsync();

        // Assert
        isValid.ShouldBeFalse();
        editContext.GetValidationMessages().ShouldContain("Name is required");
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Report_A_Satisfied_Required_Field_As_Valid()
    {
        // Arrange
        var model = new TestModel { Name = "Ada" };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.Required("Name is required"))
            .Build();

        var validator = RenderValidator(editContext, config);

        // Act
        var isValid = await validator.Instance.ValidateModelAsync();

        // Assert
        isValid.ShouldBeTrue();
        editContext.GetValidationMessages().ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Await_An_Async_Validator_Before_Returning()
    {
        // Arrange - the reason this method exists alongside EditContext.Validate(), which is
        // synchronous and returns before an async validator's first await completes. A move that
        // dropped the await would still pass a "field is required" test.
        var completed = false;
        var model = new TestModel { Name = "Ada" };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.WithAsyncValidator(
                async _ =>
                {
                    await Task.Delay(20);
                    completed = true;
                    return false;
                },
                "Async validator rejected the value"))
            .Build();

        var validator = RenderValidator(editContext, config);

        // Act
        var isValid = await validator.Instance.ValidateModelAsync();

        // Assert
        completed.ShouldBeTrue();
        isValid.ShouldBeFalse();
        editContext.GetValidationMessages().ShouldContain("Async validator rejected the value");
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Skip_A_Hidden_Field()
    {
        // Arrange - a hidden required field must not block submission with an error nobody can see.
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .Required("Name is required")
                .VisibleWhen(_ => false))
            .Build();

        var validator = RenderValidator(editContext, config);

        // Act
        var isValid = await validator.Instance.ValidateModelAsync();

        // Assert
        isValid.ShouldBeTrue();
        editContext.GetValidationMessages().ShouldBeEmpty();
    }

    [Fact]
    public void OnInitialized_Should_Throw_Without_A_Cascading_EditContext()
    {
        // Arrange - the component is only meaningful inside an EditForm, and says so.
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            Render<DynamicFormValidator<TestModel>>(parameters => parameters
                .Add(p => p.Configuration, config)));

        ex.Message.ShouldContain(nameof(EditContext));
    }

    private IRenderedComponent<DynamicFormValidator<TestModel>> RenderValidator(
        EditContext editContext,
        IFormConfiguration<TestModel> configuration)
        => Render<DynamicFormValidator<TestModel>>(parameters => parameters
            .AddCascadingValue(editContext)
            .Add(p => p.Configuration, configuration));

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
