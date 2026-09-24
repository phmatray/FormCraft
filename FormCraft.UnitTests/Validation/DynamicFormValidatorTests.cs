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

    [Fact]
    public async Task ValidateModelAsync_Should_Not_Lose_Prior_Messages_When_A_Later_Pass_Throws()
    {
        // Arrange - a first pass, against a config with only a Required field, reports it invalid
        // and populates the message store (#440).
        var model = new TestModel();
        var editContext = new EditContext(model);
        var requiredOnlyConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.Required("Name is required"))
            .Build();

        var validator = RenderValidator(editContext, requiredOnlyConfig);

        var firstPassIsValid = await validator.Instance.ValidateModelAsync();
        firstPassIsValid.ShouldBeFalse();
        editContext.GetValidationMessages().ShouldContain("Name is required");

        // Act - swap in a config whose only field now has a genuinely faulting accessor (not a
        // null intermediate, which TryGetValue already tolerates). The Required field is
        // deliberately absent here: were it still present, today's buggy code would re-add "Name
        // is required" before reaching the throw, passing even though the bug this test targets
        // is real.
        var faultingOnlyConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Faulting, field => field.WithLabel("Faulting"))
            .Build();
        validator.Instance.Configuration = faultingOnlyConfig;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await validator.Instance.ValidateModelAsync());

        // Assert - a mid-pass throw must not wipe the message store: the first pass's message
        // must still be present afterwards.
        editContext.GetValidationMessages().ShouldContain("Name is required");
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Not_Lose_Prior_Messages_When_The_Collection_Loop_Throws()
    {
        // Arrange - same shape as the field-loop version above (#440), but this time the second
        // pass's throw originates in the collection loop, proving the fix covers both loops that
        // feed the one deferred flush.
        var model = new TestModel();
        var editContext = new EditContext(model);
        var requiredOnlyConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.Required("Name is required"))
            .Build();

        var validator = RenderValidator(editContext, requiredOnlyConfig);

        var firstPassIsValid = await validator.Instance.ValidateModelAsync();
        firstPassIsValid.ShouldBeFalse();
        editContext.GetValidationMessages().ShouldContain("Name is required");

        // Act - swap in a config whose only field is a collection with a genuinely faulting
        // accessor. The Required field is deliberately absent, for the same reason as above.
        var faultingCollectionConfig = FormBuilder<TestModel>.Create()
            .AddCollectionField(x => x.FaultingItems, collection => collection
                .WithLabel("Faulting Items")
                .WithItemForm(item => item
                    .AddField(x => x.Name, field => field.WithLabel("Name"))))
            .Build();
        validator.Instance.Configuration = faultingCollectionConfig;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await validator.Instance.ValidateModelAsync());

        // Assert - the collection-loop throw must not wipe the message store either.
        editContext.GetValidationMessages().ShouldContain("Name is required");
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Clear_A_Stale_Message_When_The_Field_Becomes_Valid_On_A_Later_Pass()
    {
        // Arrange - Acceptance Criterion 2 (#440): a pass that completes without throwing must
        // still clear every stale message the previous pass left, not just skip clearing because
        // nothing new failed this time. Same config, same instance, both passes - no swap needed.
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.Required("Name is required"))
            .Build();
        var validator = RenderValidator(editContext, config);

        var firstPassIsValid = await validator.Instance.ValidateModelAsync();
        firstPassIsValid.ShouldBeFalse();
        editContext.GetValidationMessages().ShouldContain("Name is required");

        // Act - the field is now satisfied.
        model.Name = "Ada";
        var secondPassIsValid = await validator.Instance.ValidateModelAsync();

        // Assert - the first pass's stale message must be gone, not merely un-repeated.
        secondPassIsValid.ShouldBeTrue();
        editContext.GetValidationMessages().ShouldBeEmpty();
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
    public async Task HandleFieldChanged_Should_Keep_A_Fields_Prior_Message_When_Its_Validator_Throws()
    {
        // Arrange - a first pass leaves "Name is bad" in the store (#443).
        // A raw IFieldValidator, not WithValidator(Func<...>): CustomValidator catches its own
        // delegate's exception and turns it into a message, which would hide the bug.
        var faulty = new ThrowsWhenArmedValidator<TestModel>("Name is bad");
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.WithValidator(faulty))
            .Build();
        var validator = RenderValidator(editContext, config);
        (await validator.Instance.ValidateModelAsync()).ShouldBeFalse();

        // Act - the validator now throws; HandleFieldChanged's outer catch swallows it.
        faulty.Armed = true;
        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Name)));

        // Assert - the throw must not leave the field cleared and silently "valid".
        editContext.GetValidationMessages(editContext.Field(nameof(TestModel.Name)))
            .ShouldContain("Name is bad");
    }

    [Fact]
    public async Task ValidateCollectionItemFieldAsync_Should_Keep_A_Cells_Prior_Message_When_Its_Validator_Throws()
    {
        // Arrange - the collection-cell path had the same clear-before-validate shape (#443).
        var faulty = new ThrowsWhenArmedValidator<ItemModel>("Item name is bad");
        var model = new TestModel { Items = [new ItemModel()] };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.Name, field => field.WithValidator(faulty))))
            .Build();
        var validator = RenderValidator(editContext, config);
        (await validator.Instance.ValidateModelAsync()).ShouldBeFalse();
        var cell = new FieldIdentifier(model, "Items[0].Name");
        editContext.GetValidationMessages(cell).ShouldContain("Item name is bad");

        // Act
        faulty.Armed = true;
        editContext.NotifyFieldChanged(cell);

        // Assert
        editContext.GetValidationMessages(cell).ShouldContain("Item name is bad");
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Not_Clobber_A_Field_Update_That_Landed_During_The_Pass()
    {
        // Arrange - Email is validated first and fails; Name's async validator then parks the pass
        // on a gate, so the pass has computed (but not flushed) Email's stale message (#443).
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Email, field => field.Required("Email is required"))
            .AddField(x => x.Name, field => field.WithAsyncValidator(_ => gate.Task, "Name rejected"))
            .Build();
        var validator = RenderValidator(editContext, config);

        var pass = validator.Instance.ValidateModelAsync();

        // Act - the user fixes Email while the pass is parked; HandleFieldChanged clears its message.
        model.Email = "ada@example.com";
        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.Email)));
        editContext.GetValidationMessages(editContext.Field(nameof(TestModel.Email))).ShouldBeEmpty();

        gate.SetResult(true);
        await pass;

        // Assert - the pass's flush must not resurrect the stale "Email is required".
        editContext.GetValidationMessages(editContext.Field(nameof(TestModel.Email))).ShouldBeEmpty();
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

    private sealed class ThrowsWhenArmedValidator<TModel>(string message) : IFieldValidator<TModel, string>
    {
        public bool Armed { get; set; }

        public string? ErrorMessage { get; set; } = message;

        public Task<ValidationResult> ValidateAsync(TModel model, string value, IServiceProvider services)
            => Armed
                ? throw new InvalidOperationException("validator bug")
                : Task.FromResult(ValidationResult.Failure(ErrorMessage!));
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public NestedModel? Nested { get; set; }

        public List<ItemModel> Items { get; set; } = [];

        public string Faulting => throw new InvalidOperationException("boom");

        // A settable property, unlike Faulting above: CollectionFieldConfiguration's constructor
        // compiles a setter delegate too, and Expression.Assign refuses a get-only member.
        public List<ItemModel> FaultingItems
        {
            get => throw new InvalidOperationException("boom");
            set { }
        }
    }

    public class NestedModel
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ItemModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
