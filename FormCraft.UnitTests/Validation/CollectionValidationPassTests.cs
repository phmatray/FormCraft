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

    public class OrderItem
    {
        public string ProductName { get; set; } = "";
    }
}
