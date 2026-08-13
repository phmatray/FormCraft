namespace FormCraft.UnitTests.Validation;

/// <summary>
/// Pins how many times a validation pass actually runs each collection item field's validators, and
/// what messages that pass produces (#329).
///
/// <para>
/// The invocation count has to be asserted with a <b>counting validator</b> rather than by inspecting
/// messages, and that is the whole point of this file: the duplicate work is invisible in the output.
/// <c>ValidateModelAsync</c> feeds the two passes into two <i>different</i> identifiers — flat strings
/// on the collection's own field, nested ones on <c>Items[i].Field</c> (#91) — so each message is
/// still added once and the form looks correct while every validator has run twice. A validator with
/// side effects (an API call, a counter, a write) is where that becomes visible, and by then it is a
/// bug report rather than a test failure.
/// </para>
/// </summary>
public class CollectionValidationPassTests : BunitContext
{
    public CollectionValidationPassTests()
    {
        Services.AddFormCraft();
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Invoke_Each_Item_Field_Validator_Once_Per_Pass()
    {
        // Arrange - two rows, one item field, one counting validator.
        var counter = new CountingValidator();
        var model = new OrderModel
        {
            Items = { new OrderItem { ProductName = "Widget" }, new OrderItem { ProductName = "Gadget" } }
        };
        var editContext = new EditContext(model);
        var validator = RenderValidator(editContext, BuildConfiguration(counter));

        // Act - one full validation pass.
        await validator.Instance.ValidateModelAsync();

        // Assert - two rows means two invocations, not four. Asserting the per-row values (rather
        // than just the total) also proves the pass visited both rows rather than one row twice.
        counter.Seen.ShouldBe(["Widget", "Gadget"]);
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Attach_Flat_Messages_To_The_Collection_Field()
    {
        // Arrange - rows 0 and 2 are empty, row 1 is filled.
        var model = new OrderModel
        {
            Items =
            {
                new OrderItem { ProductName = "" },
                new OrderItem { ProductName = "Widget" },
                new OrderItem { ProductName = "" }
            }
        };
        var editContext = new EditContext(model);
        var validator = RenderValidator(editContext, BuildConfiguration(new CountingValidator()));

        // Act
        var isValid = await validator.Instance.ValidateModelAsync();

        // Assert - the flat, human-formatted strings on the collection's own identifier, in order.
        // These are what a restructure is most likely to reword or reorder by accident.
        isValid.ShouldBeFalse();
        var flat = editContext.GetValidationMessages(editContext.Field(nameof(OrderModel.Items))).ToList();
        flat.ShouldBe(
        [
            "Items [1] - Product: Product name is required",
            "Items [3] - Product: Product name is required"
        ]);
    }

    [Fact]
    public async Task ValidateModelAsync_Should_Attach_Nested_Messages_To_Each_Items_Identifier()
    {
        // Arrange - same shape as above; this pins the #91 nested identifiers instead.
        var model = new OrderModel
        {
            Items =
            {
                new OrderItem { ProductName = "" },
                new OrderItem { ProductName = "Widget" },
                new OrderItem { ProductName = "" }
            }
        };
        var editContext = new EditContext(model);
        var validator = RenderValidator(editContext, BuildConfiguration(new CountingValidator()));

        // Act
        await validator.Instance.ValidateModelAsync();

        // Assert - each message lands on its own row's identifier, and the valid row has none.
        editContext.GetValidationMessages(new FieldIdentifier(model, "Items[0].ProductName"))
            .ShouldBe(["Product name is required"]);
        editContext.GetValidationMessages(new FieldIdentifier(model, "Items[1].ProductName"))
            .ShouldBeEmpty();
        editContext.GetValidationMessages(new FieldIdentifier(model, "Items[2].ProductName"))
            .ShouldBe(["Product name is required"]);
    }

    [Fact]
    public void Editing_One_Row_Should_Not_Invoke_Another_Rows_Validators()
    {
        // Arrange - two rows; only row 0 is edited.
        var counter = new CountingValidator();
        var model = new OrderModel
        {
            Items = { new OrderItem { ProductName = "Widget" }, new OrderItem { ProductName = "Gadget" } }
        };
        var editContext = new EditContext(model);
        RenderValidator(editContext, BuildConfiguration(counter));

        // Act - the notification a keystroke in row 0 produces (#91's nested identifier). The
        // handler is `async void`, so there is no task to await; asserting directly is still
        // deterministic because CountingValidator completes synchronously, which makes every await
        // in the chain continue synchronously. Deliberately not WaitForAssertion: this path changes
        // no rendered output, so a wait would poll once and then report a timeout instead of the
        // assertion failure.
        editContext.NotifyFieldChanged(new FieldIdentifier(model, "Items[0].ProductName"));

        // Assert - only the edited cell is validated. Revalidating the whole collection to report on
        // one cell costs items × fields per keystroke and, with an async validator, that many awaited
        // calls per character.
        counter.Seen.ShouldBe(["Widget"]);
    }

    [Fact]
    public void Editing_A_Row_Into_An_Invalid_Value_Should_Report_On_That_Cell()
    {
        // Arrange - the positive direction of the field-changed path. Its counterpart above asserts
        // which validators did NOT run, which a no-op error-reporting branch would satisfy just as
        // well; only this test fails if the new single-cell path stops producing messages.
        var model = new OrderModel { Items = { new OrderItem { ProductName = "Widget" } } };
        var editContext = new EditContext(model);
        RenderValidator(editContext, BuildConfiguration(new CountingValidator()));

        // Act - the user clears the cell, then the notification a keystroke raises.
        model.Items[0].ProductName = "";
        editContext.NotifyFieldChanged(new FieldIdentifier(model, "Items[0].ProductName"));

        // Assert - the message appears on that cell's own identifier.
        editContext.GetValidationMessages(new FieldIdentifier(model, "Items[0].ProductName"))
            .ShouldBe(["Product name is required"]);
    }

    [Fact]
    public async Task Editing_A_Row_Should_Report_Every_Configuration_Declared_For_That_Field()
    {
        // Arrange - two configurations for the SAME property. A full pass runs both, so the
        // field-changed path must too; resolving the changed cell to the first match only would drop
        // the second message on the first keystroke and restore it on the next submit.
        var model = new OrderModel { Items = { new OrderItem { ProductName = "" } } };
        var editContext = new EditContext(model);
        var configuration = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product").Required("FIRST"))
                    .AddField(x => x.ProductName, field => field.WithLabel("Product").Required("SECOND"))))
            .Build();

        var validator = Render<DynamicFormValidator<OrderModel>>(parameters => parameters
            .AddCascadingValue(editContext)
            .Add(p => p.Configuration, configuration));

        // Act - a full pass first, then the single-cell path for the same cell.
        await validator.Instance.ValidateModelAsync();
        var afterFullPass = editContext
            .GetValidationMessages(new FieldIdentifier(model, "Items[0].ProductName")).ToList();

        editContext.NotifyFieldChanged(new FieldIdentifier(model, "Items[0].ProductName"));
        var afterKeystroke = editContext
            .GetValidationMessages(new FieldIdentifier(model, "Items[0].ProductName")).ToList();

        // Assert - the two paths agree. That equality is the real contract: whichever messages a
        // submit produces for a cell, editing that cell must produce the same ones.
        afterFullPass.ShouldBe(["FIRST", "SECOND"]);
        afterKeystroke.ShouldBe(afterFullPass);
    }

    [Fact]
    public async Task Two_Collections_Of_The_Same_Item_Type_Should_Not_Share_A_Validator()
    {
        // Arrange - two collections whose items are the same type but whose item forms differ. The
        // reflective validator is cached per CONFIGURATION for exactly this reason: caching it per
        // item type would serve the second collection the first one's configuration, and each would
        // report the other's message.
        var model = new TwoListModel
        {
            Items = { new OrderItem { ProductName = "" } },
            Extras = { new OrderItem { ProductName = "" } }
        };
        var editContext = new EditContext(model);

        var configuration = FormBuilder<TwoListModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field
                        .WithLabel("Product")
                        .Required("Items message"))))
            .AddCollectionField(x => x.Extras, collection => collection
                .WithLabel("Extras")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field
                        .WithLabel("Product")
                        .Required("Extras message"))))
            .Build();

        var validator = Render<DynamicFormValidator<TwoListModel>>(parameters => parameters
            .AddCascadingValue(editContext)
            .Add(p => p.Configuration, configuration));

        // Act
        await validator.Instance.ValidateModelAsync();

        // Assert - each collection reports its own configured message.
        editContext.GetValidationMessages(new FieldIdentifier(model, "Items[0].ProductName"))
            .ShouldBe(["Items message"]);
        editContext.GetValidationMessages(new FieldIdentifier(model, "Extras[0].ProductName"))
            .ShouldBe(["Extras message"]);
    }

    private static IFormConfiguration<OrderModel> BuildConfiguration(CountingValidator counter) =>
        FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field
                        .WithLabel("Product")
                        .WithValidator(counter))))
            .Build();

    private IRenderedComponent<DynamicFormValidator<OrderModel>> RenderValidator(
        EditContext editContext,
        IFormConfiguration<OrderModel> configuration)
        => Render<DynamicFormValidator<OrderModel>>(parameters => parameters
            .AddCascadingValue(editContext)
            .Add(p => p.Configuration, configuration));

    /// <summary>
    /// Records the value it was handed on every invocation, so the test can assert both how many
    /// times it ran and which rows it saw.
    /// </summary>
    private sealed class CountingValidator : IFieldValidator<OrderItem, string>
    {
        private readonly List<string> _seen = [];

        public IReadOnlyList<string> Seen => _seen;

        public string? ErrorMessage { get; set; }

        public Task<ValidationResult> ValidateAsync(OrderItem model, string value, IServiceProvider services)
        {
            _seen.Add(value);

            return Task.FromResult(string.IsNullOrWhiteSpace(value)
                ? ValidationResult.Failure("Product name is required")
                : ValidationResult.Success());
        }
    }

    public class OrderModel
    {
        public List<OrderItem> Items { get; set; } = [];
    }

    public class TwoListModel
    {
        public List<OrderItem> Items { get; set; } = [];

        public List<OrderItem> Extras { get; set; } = [];
    }

    public class OrderItem
    {
        public string ProductName { get; set; } = "";
    }
}
