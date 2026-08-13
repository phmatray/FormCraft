namespace FormCraft.UnitTests.Validation;

/// <summary>
/// Pins what the validation path observes through each field's value getter, so the per-call
/// <c>Expression.Compile()</c> in <see cref="CollectionFieldValidator{TModel, TItem}" /> and
/// <c>DynamicFormValidator</c> can be cached away without changing behaviour (#312).
///
/// <para>
/// The distinction these tests protect is the same one #269 established for the render path: the
/// compiled <b>getter</b> may be cached, the <b>value</b> it returns may never be. Caching a value
/// here would validate every later pass against the content of the first one — a form that reports
/// stale errors, or none at all, after the user fixes the field.
/// </para>
/// </summary>
public class ValidatorValueGetterCachingTests
{
    private readonly IServiceProvider _services = A.Fake<IServiceProvider>();

    [Fact]
    public async Task ValidateItemsAsync_Should_See_A_Corrected_Value_On_A_Later_Pass()
    {
        // Arrange - a required item field, initially empty.
        var itemForm = FormBuilder<ItemModel>.Create()
            .AddField(x => x.ProductName, field => field.Required("Product name is required"))
            .Build();
        var configuration = new CollectionFieldConfiguration<OrderModel, ItemModel>(x => x.Items)
        {
            ItemFormConfiguration = itemForm
        };
        var validator = new CollectionFieldValidator<OrderModel, ItemModel>(configuration);
        var model = new OrderModel { Items = { new ItemModel { ProductName = "" } } };

        // Act - validate, correct the model, validate again with the same configuration instance.
        var before = await validator.ValidateItemsAsync(model, _services);
        model.Items[0].ProductName = "Widget";
        var after = await validator.ValidateItemsAsync(model, _services);

        // Assert - the second pass reads the model afresh; a cached *value* would still report the
        // empty product name the user has already fixed.
        before.Count.ShouldBe(1);
        before[0].Message.ShouldBe("Product name is required");
        after.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateItemsAsync_Should_Not_Share_A_Getter_Between_Two_Configurations_Of_The_Same_Field_Name()
    {
        // Arrange - two configurations whose fields report the same name but read different members.
        // A cache keyed by field name (or by model type) would serve the second the first's getter.
        var model = new OrderModel { Items = { new ItemModel { First = "from First", Second = "from Second" } } };

        var (firstValidator, firstRecorder) =
            CreateValidator(nameof(ItemModel.ProductName), _ => Expr(m => (object)m.First));
        var (secondValidator, secondRecorder) =
            CreateValidator(nameof(ItemModel.ProductName), _ => Expr(m => (object)m.Second));

        // Act
        await firstValidator.ValidateItemsAsync(model, _services);
        await secondValidator.ValidateItemsAsync(model, _services);

        // Assert
        firstRecorder.Seen.ShouldBe(["from First"]);
        secondRecorder.Seen.ShouldBe(["from Second"]);
    }

    [Fact]
    public async Task ValidateItemsAsync_Should_Not_Rebuild_The_Value_Getter_On_Every_Pass()
    {
        // Arrange - a configuration that hands out a DIFFERENT expression on every read, each
        // reading a different member. A recompile therefore necessarily observes a different value,
        // so "did it recompile?" becomes "did the observed value change?" — which cannot pass by
        // accident. CollectionFieldValidator reads ValueExpression exactly once per item per field,
        // so there is no second reader to muddy the count.
        var model = new OrderModel
        {
            Items =
            {
                new ItemModel
                {
                    First = "first read",
                    Second = "second read",
                    Third = "third read",
                    Fourth = "fourth read"
                }
            }
        };

        var expressions = new Queue<Expression<Func<ItemModel, object>>>(
        [
            Expr(m => (object)m.First),
            Expr(m => (object)m.Second),
            Expr(m => (object)m.Third),
            Expr(m => (object)m.Fourth)
        ]);

        var (validator, recorder) = CreateValidator(
            nameof(ItemModel.ProductName),
            _ => expressions.Count > 1 ? expressions.Dequeue() : expressions.Peek());

        // Act
        await validator.ValidateItemsAsync(model, _services);
        await validator.ValidateItemsAsync(model, _services);

        // Assert - the second pass reused the getter compiled for the first, so it observed the same
        // member. Recompiling would pick up a later expression and change the value.
        recorder.Seen.Count.ShouldBe(2);
        recorder.Seen[0].ShouldBe("first read");
        recorder.Seen[1].ShouldBe(
            recorder.Seen[0],
            "the second validation pass recompiled the value getter instead of reusing the cached one");
    }

    /// <summary>
    /// Builds a validator over a single-field item form whose field reports <paramref name="fieldName" />
    /// and resolves its expression through <paramref name="expressionFactory" /> on every read.
    /// </summary>
    private static (CollectionFieldValidator<OrderModel, ItemModel> Validator, RecordingValidator Recorder) CreateValidator(
        string fieldName,
        Func<int, Expression<Func<ItemModel, object>>> expressionFactory)
    {
        var recorder = new RecordingValidator();
        var reads = 0;

        var field = A.Fake<IFieldConfiguration<ItemModel, object>>();
        A.CallTo(() => field.FieldName).Returns(fieldName);
        A.CallTo(() => field.Validators).Returns(new List<IFieldValidator<ItemModel, object>> { recorder });
        A.CallTo(() => field.ValueExpression).ReturnsLazily(() => expressionFactory(reads++));

        var itemForm = A.Fake<IFormConfiguration<ItemModel>>();
        A.CallTo(() => itemForm.Fields).Returns([field]);

        var configuration = new CollectionFieldConfiguration<OrderModel, ItemModel>(x => x.Items)
        {
            ItemFormConfiguration = itemForm
        };

        return (new CollectionFieldValidator<OrderModel, ItemModel>(configuration), recorder);
    }

    /// <summary>Identity helper so an expression literal can be written inline in a collection.</summary>
    private static Expression<Func<ItemModel, object>> Expr(Expression<Func<ItemModel, object>> expression)
        => expression;

    private sealed class RecordingValidator : IFieldValidator<ItemModel, object>
    {
        public List<object?> Seen { get; } = [];

        public string? ErrorMessage { get; set; }

        public Task<ValidationResult> ValidateAsync(ItemModel model, object value, IServiceProvider services)
        {
            Seen.Add(value);
            return Task.FromResult(ValidationResult.Success());
        }
    }

    public class OrderModel
    {
        public List<ItemModel> Items { get; set; } = [];
    }

    public class ItemModel
    {
        public string ProductName { get; set; } = "";
        public string First { get; set; } = "";
        public string Second { get; set; } = "";
        public string Third { get; set; } = "";
        public string Fourth { get; set; } = "";
    }
}
