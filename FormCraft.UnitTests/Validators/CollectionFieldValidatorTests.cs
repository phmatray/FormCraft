namespace FormCraft.UnitTests.Validators;

public class CollectionFieldValidatorTests
{
    [Fact]
    public async Task Validate_Should_Pass_When_No_Constraints()
    {
        // Arrange
        var config = CreateCollectionConfig();
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel();
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateAsync(model, services);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Validate_Should_Fail_When_Below_MinItems()
    {
        // Arrange
        var config = CreateCollectionConfig(minItems: 1);
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel(); // Empty items list
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateAsync(model, services);

        // Assert
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("at least 1");
    }

    [Fact]
    public async Task Validate_Should_Pass_When_At_MinItems()
    {
        // Arrange
        var config = CreateCollectionConfig(minItems: 1);
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel
        {
            Items = new List<OrderItemModel> { new() { ProductName = "Widget" } }
        };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateAsync(model, services);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Validate_Should_Fail_When_Above_MaxItems()
    {
        // Arrange
        var config = CreateCollectionConfig(maxItems: 2);
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel
        {
            Items = new List<OrderItemModel>
            {
                new(), new(), new() // 3 items, max is 2
            }
        };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateAsync(model, services);

        // Assert
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("at most 2");
    }

    [Fact]
    public async Task Validate_Should_Validate_Individual_Items_With_ItemForm()
    {
        // Arrange
        var config = CreateCollectionConfigWithItemForm();
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel
        {
            Items = new List<OrderItemModel>
            {
                new() { ProductName = "" } // Empty product name should fail required validation
            }
        };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateAsync(model, services);

        // Assert
        errors.Count.ShouldBeGreaterThan(0);
        errors.ShouldContain(e => e.Contains("[1]") && e.Contains("ProductName"));
    }

    [Fact]
    public async Task Validate_Should_Pass_When_Items_Are_Valid()
    {
        // Arrange
        var config = CreateCollectionConfigWithItemForm();
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel
        {
            Items = new List<OrderItemModel>
            {
                new() { ProductName = "Widget", Quantity = 5, UnitPrice = 10.00m }
            }
        };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateAsync(model, services);

        // Assert
        errors.ShouldBeEmpty();
    }

    private CollectionFieldConfiguration<OrderModel, OrderItemModel> CreateCollectionConfig(
        int minItems = 0, int maxItems = 0)
    {
        return new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.Items)
        {
            MinItems = minItems,
            MaxItems = maxItems
        };
    }

    private CollectionFieldConfiguration<OrderModel, OrderItemModel> CreateCollectionConfigWithItemForm()
    {
        var itemForm = FormBuilder<OrderItemModel>.Create()
            .AddField(x => x.ProductName, field => field.Required("Product name is required"))
            .AddField(x => x.Quantity, field => field.WithRange(1, 100))
            .Build();

        return new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.Items)
        {
            ItemFormConfiguration = itemForm
        };
    }

    public class OrderModel
    {
        public string OrderNumber { get; set; } = "";
        public List<OrderItemModel> Items { get; set; } = new();
    }

    public class OrderItemModel
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 0m;
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
