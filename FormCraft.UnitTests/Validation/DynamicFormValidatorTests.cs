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
    public async Task ValidateModelAsync_Should_Not_Throw_For_A_Nested_Binding_With_A_Null_Intermediate()
    {
        // Arrange - an ordinary field (no custom template) bound to a nested expression whose
        // intermediate is null. Before #397 FieldValueGetterCache<TModel>.GetOrCompile(field)(model)
        // was invoked with no guard here, so this threw an unhandled NullReferenceException straight
        // through the whole validation pass instead of just treating the field's value as null.
        var model = new TestModel { Nested = null };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Nested!.Value, field => field.Required("Nested value is required"))
            .Build();

        var validator = RenderValidator(editContext, config);

        // Act
        var isValid = await validator.Instance.ValidateModelAsync();

        // Assert - the failed read is validated as null, not silently skipped: a Required() field
        // still reports invalid (review finding, #397). A no-throw assertion alone would also pass a
        // regression that skips validating a field whose read failed, since a field with no validator
        // exercised proves nothing either way.
        isValid.ShouldBeFalse();
        editContext.GetValidationMessages().ShouldContain("Nested value is required");
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Propagate_An_Exception_From_A_Genuinely_Faulting_Accessor()
    {
        // Arrange - a genuinely broken getter, not a null intermediate. Before #425 the read was
        // swallowed and validated as null, so a field with no Required() rule passed silently.
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Faulting, field => field.WithLabel("Faulting"))
            .Build();

        var validator = RenderValidator(editContext, config);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await validator.Instance.ValidateModelAsync());
    }

    // No propagation sibling for HandleFieldChanged (#425): its own outer catch guards the async-void
    // boundary, where an escaping exception would crash the Blazor circuit, so it swallows a genuine
    // fault by design. The read it performs is proven to propagate at the TryGetValue seam instead.
    [Fact]
    public void HandleFieldChanged_Should_Not_Throw_For_A_Nested_Binding_With_A_Null_Intermediate()
    {
        // Arrange - the field-changed path (a single field's re-validation) reads the same cache
        // through the same unguarded shape ValidateModelAsync used to (#397's "fixed along the way"):
        // it was never named by the issue's three call sites, but it is the same file, the same
        // hazard, and the same fix.
        var model = new TestModel { Nested = null };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Nested!.Value, field => field.Required("Nested value is required"))
            .Build();

        RenderValidator(editContext, config);

        // Act - FieldName is the expression's last member ("Value"), not the dotted path
        // (FieldConfiguration.cs), so that is what HandleFieldChanged matches on. Validators complete
        // synchronously, so the async void handler completes synchronously too; asserting immediately
        // is deterministic (see CollectionValidationPassTests).
        Should.NotThrow(() =>
            editContext.NotifyFieldChanged(new FieldIdentifier(model, "Value")));

        // Assert - the failed read is validated as null, not silently skipped (review finding, #397).
        editContext.GetValidationMessages(new FieldIdentifier(model, "Value"))
            .ShouldContain("Nested value is required");
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

        public NestedModel? Nested { get; set; }

        public string Faulting => throw new InvalidOperationException("boom");
    }

    public class NestedModel
    {
        public string Value { get; set; } = string.Empty;
    }
}
