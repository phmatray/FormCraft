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

    [Fact]
    public async Task ValidateItemsAsync_Should_Return_Structured_Errors_With_Index_And_FieldName()
    {
        // Arrange
        var config = CreateCollectionConfigWithItemForm();
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel
        {
            Items = new List<OrderItemModel>
            {
                new() { ProductName = "Widget", Quantity = 5 }, // valid
                new() { ProductName = "", Quantity = 5 }        // invalid ProductName
            }
        };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateItemsAsync(model, services);

        // Assert - structured error identifies the failing item index and field name
        errors.Count.ShouldBe(1);
        errors[0].ItemIndex.ShouldBe(1);
        errors[0].FieldName.ShouldBe("ProductName");
        errors[0].Message.ShouldBe("Product name is required");
    }

    [Fact]
    public async Task ValidateItemsAsync_Should_Return_Empty_When_Items_Are_Valid()
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
        var errors = await validator.ValidateItemsAsync(model, services);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateItemsAsync_Should_Return_Empty_When_No_ItemFormConfiguration()
    {
        // Arrange
        var config = CreateCollectionConfig();
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel { Items = new List<OrderItemModel> { new() } };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateItemsAsync(model, services);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAllAsync_Should_Use_One_Snapshot_For_Count_And_Item_Messages()
    {
        // Arrange — a collection property that returns a DIFFERENT snapshot on every access: one
        // invalid item on the first read, three valid items on every read after. Resolving the
        // accessor twice per pass (#344) would validate the first snapshot but count the second,
        // so MinItems(2) would never fire even though the snapshot actually validated has 1 item.
        var itemForm = FormBuilder<OrderItemModel>.Create()
            .AddField(x => x.ProductName, field => field.Required("Product name is required"))
            .Build();
        var config = new CollectionFieldConfiguration<VolatileOrderModel, OrderItemModel>(x => x.Items)
        {
            MinItems = 2,
            ItemFormConfiguration = itemForm
        };
        var validator = new CollectionFieldValidator<VolatileOrderModel, OrderItemModel>(config);
        var model = new VolatileOrderModel();
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await validator.ValidateAllAsync(model, services);

        // Assert — the count rule and the item traversal must describe the SAME (first) snapshot:
        // 1 item, invalid, below MinItems(2). Before the fix, the traversal saw that 1-item
        // snapshot (1 item error) while the count rule saw the second, 3-item snapshot and never
        // reported MinItems at all.
        result.ItemErrors.Count.ShouldBe(1);
        result.ItemErrors[0].ItemIndex.ShouldBe(0);
        result.Messages.ShouldContain(m => m.Contains("at least 2"));
        result.Messages.Count.ShouldBe(2);
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

    /// <summary>
    /// A model whose collection property returns a NEW, DIFFERENT list on every access — first
    /// call: one invalid item; every call after: three valid items. Used to pin that one
    /// validation pass reads the collection only once (#344): a second read during the same pass
    /// would see a different snapshot than the one that was actually validated.
    /// </summary>
    public class VolatileOrderModel
    {
        private int _accessCount;

        // A setter is required — CollectionFieldConfiguration's constructor derives CollectionSetter
        // from the same expression and needs it to be writeable — but this test never calls it; the
        // getter is what matters.
        public List<OrderItemModel> Items
        {
            get
            {
                _accessCount++;
                return _accessCount == 1
                    ? new List<OrderItemModel> { new() { ProductName = "" } }
                    : new List<OrderItemModel>
                    {
                        new() { ProductName = "A" },
                        new() { ProductName = "B" },
                        new() { ProductName = "C" }
                    };
            }
            set { }
        }
    }
}
