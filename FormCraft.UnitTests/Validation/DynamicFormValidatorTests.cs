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

        // Act - FieldName is the expression's full dotted path ("Nested.Value") since #437, so that
        // is what HandleFieldChanged matches on. Validators complete
        // synchronously, so the async void handler completes synchronously too; asserting immediately
        // is deterministic (see CollectionValidationPassTests).
        Should.NotThrow(() =>
            editContext.NotifyFieldChanged(new FieldIdentifier(model, "Nested.Value")));

        // Assert - the failed read is validated as null, not silently skipped (review finding, #397).
        editContext.GetValidationMessages(new FieldIdentifier(model, "Nested.Value"))
            .ShouldContain("Nested value is required");
    }

    [Fact]
    public void HandleFieldChanged_Should_Run_The_Changed_Nested_Fields_Own_Validator_Not_A_Same_Suffix_Siblings()
    {
        // Arrange - two nested fields sharing a last segment. Before #437 both were named "Value",
        // so the FieldName lookup always resolved to Billing (registered first): editing Shipping ran
        // Billing's validator and filed its message under the identifier both fields shared.
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Billing.Value, field => field.WithValidator(_ => false, "Billing is bad"))
            .AddField(x => x.Shipping.Value, field => field.WithValidator(_ => false, "Shipping is bad"))
            .Build();

        RenderValidator(editContext, config);

        // Act - validators complete synchronously, so the async void handler does too.
        editContext.NotifyFieldChanged(new FieldIdentifier(model, "Shipping.Value"));

        // Assert
        editContext.GetValidationMessages(new FieldIdentifier(model, "Shipping.Value"))
            .ShouldBe(new[] { "Shipping is bad" });
        editContext.GetValidationMessages().ShouldNotContain("Billing is bad");
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
    public async Task ValidateModelAsync_Should_Keep_A_Count_Rule_Error_When_A_Cell_Edit_Lands_During_The_Pass()
    {
        // Arrange - MaxItems is broken, and the count rule lives ONLY in the collection's flat set.
        // Item 0's Name parks the pass on a gate (review finding on #443).
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new TestModel { Items = [new ItemModel(), new ItemModel()] };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithMaxItems(1)
                .WithItemForm(item => item
                    .AddField(x => x.Name, field => field.WithAsyncValidator(_ => gate.Task, "Name rejected"))
                    .AddField(x => x.Code, field => field.WithLabel("Code"))))
            .Build();
        var validator = RenderValidator(editContext, config);

        var pass = validator.Instance.ValidateModelAsync();

        // Act - a cell edit on another item field lands while the pass is parked.
        editContext.NotifyFieldChanged(new FieldIdentifier(model, "Items[0].Code"));
        gate.SetResult(true);
        var isValid = await pass;

        // Assert - the pass's own count-rule error must survive its flush.
        isValid.ShouldBeFalse();
        editContext.GetValidationMessages(editContext.Field(nameof(TestModel.Items))).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleFieldChanged_Should_Not_Let_An_Older_Edit_Overwrite_A_Newer_One()
    {
        // Arrange - "slow" parks its validator; any other value completes at once (#443). Inline
        // continuations, so SetResult resumes the parked handler before it returns wherever the
        // test thread has no synchronization context; the delay below covers the case where it does.
        var gate = new TaskCompletionSource<bool>();
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.WithAsyncValidator(
                async value =>
                {
                    if (value == "slow")
                    {
                        await gate.Task;
                    }

                    return value.Length > 0;
                },
                "Name rejected"))
            .Build();
        RenderValidator(editContext, config);
        var name = editContext.Field(nameof(TestModel.Name));

        // Act - an older edit parks, a newer one finishes first, then the older one resumes.
        model.Name = "slow";
        editContext.NotifyFieldChanged(name);
        model.Name = string.Empty;
        editContext.NotifyFieldChanged(name);
        editContext.GetValidationMessages(name).ShouldContain("Name rejected");

        gate.SetResult(true);
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

        // Assert - the stale "slow is valid" result must not clear the current value's error.
        editContext.GetValidationMessages(name).ShouldContain("Name rejected");
    }

    [Fact]
    public async Task HandleFieldChanged_Should_Not_Overwrite_A_Pass_That_Finished_While_It_Was_Parked()
    {
        // Arrange - Email's only validator parks on a gate for an empty value (#445, the reverse
        // direction of #443's HandleFieldChanged_Should_Not_Let_An_Older_Edit_Overwrite_A_Newer_One):
        // a field-changed handler takes its stamp and starts reading "", then a full pass runs to
        // completion against a fixed value BEFORE the handler resumes.
        // RunContinuationsAsynchronously (review finding, #445): without it, SetResult below can run
        // the parked handler's whole continuation - including TryWrite - inline on the test thread,
        // making the Delay afterwards redundant rather than a genuine synchronization point. With it,
        // the continuation always resumes on the thread pool, so the Delay is what actually orders
        // the assertion after the handler's write attempt.
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Email, field => field.WithAsyncValidator(
                async value =>
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        await gate.Task;
                        return false;
                    }

                    return true;
                },
                "Email is required"))
            .Build();
        var validator = RenderValidator(editContext, config);
        var email = editContext.Field(nameof(TestModel.Email));

        // Act - the handler takes its stamp, reads the empty value and parks on the gate.
        editContext.NotifyFieldChanged(email);

        // The user fixes the value, then a full pass runs to completion while the handler above is
        // still parked - it reads the new value directly, so it never touches the gate.
        model.Email = "ada@example.com";
        var isValid = await validator.Instance.ValidateModelAsync();
        isValid.ShouldBeTrue();
        editContext.GetValidationMessages(email).ShouldBeEmpty();

        // The parked handler now resumes and tries to write its stale "Email is required" result.
        gate.SetResult(true);
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

        // Assert - the pass's newer (now-valid) result must survive the handler's stale write.
        editContext.GetValidationMessages(email).ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Not_Refuse_A_Field_Changed_Handler_Still_Running_When_The_Pass_Flushes()
    {
        // Arrange - review finding on #445: a flush stamp taken AFTER the pass's own awaits complete
        // is always greater than the stamp of a handler that started DURING the pass, so such a
        // handler's genuinely newer result would lose the TryWrite race it should win (#443). Email
        // fails fast on an empty value; a fixed value parks on its OWN gate so the handler can be
        // caught mid-flight. Name's validator parks the PASS itself, opening the window in which the
        // handler starts.
        var gatePass = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateHandler = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new TestModel();
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Email, field => field.WithAsyncValidator(
                async value =>
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return false;
                    }

                    await gateHandler.Task;
                    return true;
                },
                "Email is required"))
            .AddField(x => x.Name, field => field.WithAsyncValidator(_ => gatePass.Task, "Name rejected"))
            .Build();
        var validator = RenderValidator(editContext, config);
        var email = editContext.Field(nameof(TestModel.Email));

        // Act - the pass evaluates Email (fails fast on "") then parks on Name.
        var pass = validator.Instance.ValidateModelAsync();

        // While the pass is parked, the user fixes Email; the handler takes a stamp AFTER
        // passStartedAt, reads the fixed value and parks on its own gate.
        model.Email = "ada@example.com";
        editContext.NotifyFieldChanged(email);

        // The pass now completes and flushes - Email's message is still what the pass itself
        // observed, since it read Email before the fix.
        gatePass.SetResult(true);
        var isValid = await pass;
        isValid.ShouldBeFalse();
        editContext.GetValidationMessages(email).ShouldContain("Email is required");

        // The handler resumes and finds the fixed value valid.
        gateHandler.SetResult(true);
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

        // Assert - the handler started after the pass did, so it must win once it resumes; it must
        // not be refused by a flush stamp taken later than its own.
        editContext.GetValidationMessages(email).ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Stamp_A_Valid_Collection_Cell_So_A_Parked_Handler_Cannot_Resurrect_A_Stale_Error()
    {
        // Arrange - #447, a follow-up from #445's own review. #445 stamps every ordinary-field
        // identifier a pass evaluates, valid or not, so a field-changed handler parked since before
        // the pass started cannot overwrite a "now valid" result with a stale one. A collection cell
        // the pass finds VALID never appears in ItemErrors at all, so it was never added to that
        // stamped set - this cell's stale "Name rejected" below was free to win once the handler
        // resumed.
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new TestModel { Items = [new ItemModel()] };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.Name, field => field.WithAsyncValidator(
                        async value =>
                        {
                            if (string.IsNullOrEmpty(value))
                            {
                                await gate.Task;
                                return false;
                            }

                            return true;
                        },
                        "Name rejected"))))
            .Build();
        var validator = RenderValidator(editContext, config);
        var cell = new FieldIdentifier(model, "Items[0].Name");

        // Act - the handler takes its stamp, reads the empty value and parks on the gate.
        editContext.NotifyFieldChanged(cell);

        // The user fixes the value, then a full pass runs to completion while the handler above is
        // still parked - it reads the fixed value directly, so it never touches the gate.
        model.Items[0].Name = "Ada";
        var isValid = await validator.Instance.ValidateModelAsync();
        isValid.ShouldBeTrue();
        editContext.GetValidationMessages(cell).ShouldBeEmpty();

        // The parked handler now resumes and tries to write its stale "Name rejected" result.
        gate.SetResult(true);
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

        // Assert - the pass's newer (now-valid) result must survive the handler's stale write.
        editContext.GetValidationMessages(cell).ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleFieldChanged_Should_Not_Write_A_Message_For_A_Field_Hidden_Before_It_Resumes()
    {
        // Arrange - #447. HandleFieldChanged never checks IsFieldVisible, unlike ValidateModelAsync's
        // own field loop. If a field's VisibilityCondition flips to hidden while a handler is parked
        // on it, the next full pass also skips the now-hidden field (same guard), so it is never
        // stamped either - nothing then refuses the parked handler's stale write for a field the user
        // can no longer see or correct.
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new TestModel { ShowName = true };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .VisibleWhen(m => m.ShowName)
                .WithAsyncValidator(_ => gate.Task, "Name rejected"))
            .Build();
        var validator = RenderValidator(editContext, config);
        var name = editContext.Field(nameof(TestModel.Name));

        // Act - the handler takes its stamp and parks on the gate.
        editContext.NotifyFieldChanged(name);

        // The field becomes hidden, then a full pass runs to completion while the handler above is
        // still parked - ValidateModelAsync's own IsFieldVisible guard skips it entirely.
        model.ShowName = false;
        var isValid = await validator.Instance.ValidateModelAsync();
        isValid.ShouldBeTrue();
        editContext.GetValidationMessages(name).ShouldBeEmpty();

        // The parked handler now resumes and finds the value invalid.
        gate.SetResult(false);
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

        // Assert - a field nobody can see must not gain a validation message just because a handler
        // that started before it was hidden resumes after.
        editContext.GetValidationMessages(name).ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateCollectionItemFieldAsync_Should_Not_Write_A_Message_For_A_Cell_Whose_Collection_Is_Hidden_Before_It_Resumes()
    {
        // Arrange - #447, review finding on this PR: HandleFieldChanged's collection-cell branch
        // had no visibility guard at all, unlike the ordinary-field branch fixed above. Collection
        // visibility (ICollectionFieldConfigurationBase.IsVisible) is a plain mutable flag with no
        // VisibilityCondition, and it can flip to hidden while a per-cell handler is parked on an
        // async validator - the pass's own collection loop already skips a hidden collection
        // entirely, so nothing stamped this cell either, leaving the handler's stale write free to
        // win once it resumed.
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new TestModel { Items = [new ItemModel()] };
        var editContext = new EditContext(model);
        var config = FormBuilder<TestModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.Name, field => field.WithAsyncValidator(_ => gate.Task, "Name rejected"))))
            .Build();
        var validator = RenderValidator(editContext, config);
        var cell = new FieldIdentifier(model, "Items[0].Name");
        var collectionField = ((ICollectionFormConfiguration<TestModel>)config).CollectionFields[0];

        // Act - the handler takes its stamp and parks on the gate.
        editContext.NotifyFieldChanged(cell);

        // The collection becomes hidden, then a full pass runs to completion while the handler
        // above is still parked - the pass's own collection loop skips a hidden collection entirely.
        collectionField.IsVisible = false;
        var isValid = await validator.Instance.ValidateModelAsync();
        isValid.ShouldBeTrue();
        editContext.GetValidationMessages(cell).ShouldBeEmpty();

        // The parked handler now resumes and finds the value invalid.
        gate.SetResult(false);
        await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

        // Assert - a cell nobody can see must not gain a validation message just because a handler
        // that started before its collection was hidden resumes after.
        editContext.GetValidationMessages(cell).ShouldBeEmpty();
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

        public bool ShowName { get; set; } = true;

        public NestedModel? Nested { get; set; }

        public NestedModel Billing { get; set; } = new();

        public NestedModel Shipping { get; set; } = new();

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

        public string Code { get; set; } = string.Empty;
    }
}
