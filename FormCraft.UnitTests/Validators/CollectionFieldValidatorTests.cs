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

    [Fact]
    public async Task ValidateItemsAsync_Should_Not_Throw_And_Should_Still_Validate_Other_Items_When_One_Items_Nested_Binding_Is_Unreadable()
    {
        // Arrange - an item form field bound to a nested expression. Before #397 this call site
        // (CollectionFieldValidator's hoisted-getters loop) had no guard: one item with a null
        // intermediate threw an unhandled NullReferenceException out of the whole collection pass,
        // instead of just failing that item's field against null and letting the rest validate.
        var itemForm = FormBuilder<OrderItemModel>.Create()
            .AddField(x => x.Nested!.Value, field => field.Required("Nested value is required"))
            .AddField(x => x.ProductName, field => field.Required("Product name is required"))
            .Build();
        var config = new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.Items)
        {
            ItemFormConfiguration = itemForm
        };
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel
        {
            Items = new List<OrderItemModel>
            {
                new() { Nested = null, ProductName = "Widget" }, // unreadable nested binding
                new() { Nested = new NestedOrderDetail(), ProductName = "" } // invalid ProductName
            }
        };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateItemsAsync(model, services);

        // Assert - item 0's failed read is validated as null, not silently skipped: its Required()
        // nested field still reports invalid (review finding, #397). Item 1's ProductName error still
        // surfaces even though item 0's nested read failed.
        errors.ShouldContain(e => e.ItemIndex == 0 && e.FieldName == "Value");
        errors.ShouldContain(e => e.ItemIndex == 1 && e.FieldName == "ProductName");
    }

    [Fact]
    public async Task ValidateItemFieldAsync_Should_Not_Throw_For_A_Nested_Binding_With_A_Null_Intermediate()
    {
        // Arrange - the single-cell path (a field-changed notification's own validation) hits the
        // same unguarded read #397 fixes in the full-collection traversal above.
        var itemForm = FormBuilder<OrderItemModel>.Create()
            .AddField(x => x.Nested!.Value, field => field.Required("Nested value is required"))
            .Build();
        var config = new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.Items)
        {
            ItemFormConfiguration = itemForm
        };
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel
        {
            Items = new List<OrderItemModel> { new() { Nested = null } }
        };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateItemFieldAsync(model, 0, "Value", services);

        // Assert - the failed read is validated as null, not silently skipped: the Required() field
        // still reports invalid (review finding, #397). An empty-errors assertion alone would also
        // pass a regression that skips validating a field whose read failed.
        errors.ShouldContain(e => e.FieldName == "Value" && e.Message == "Nested value is required");
    }

    [Fact]
    public async Task ValidateAllAsync_Should_Not_Throw_And_Should_Report_MinItems_When_Collection_Binding_Is_Unreadable()
    {
        // Arrange - the collection itself is bound through a nested path (`x => x.Details!.Items`)
        // whose intermediate (`Details`) is null. Before #408 this call site's direct
        // `_configuration.CollectionAccessor(model)` had no guard at all, unlike the per-item field
        // reads #397 already guards, and threw an unhandled NullReferenceException.
        var config = new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.Details!.Items)
        {
            MinItems = 1
        };
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel { Details = null };
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await validator.ValidateAllAsync(model, services);

        // Assert - an unreadable collection validates as zero items: MinItems(1) fires exactly as it
        // would for a genuinely empty (but readable) collection, and there are no item errors.
        result.Messages.ShouldContain(m => m.Contains("at least 1"));
        result.ItemErrors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateItemFieldAsync_Should_Not_Throw_And_Return_No_Errors_When_Collection_Binding_Is_Unreadable()
    {
        // Arrange - same unreadable collection binding, hitting the single-cell path instead.
        var itemForm = FormBuilder<OrderItemModel>.Create()
            .AddField(x => x.ProductName, field => field.Required("Product name is required"))
            .Build();
        var config = new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.Details!.Items)
        {
            ItemFormConfiguration = itemForm
        };
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel { Details = null };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateItemFieldAsync(model, 0, "ProductName", services);

        // Assert - matches the existing out-of-range-index behaviour: no errors, not a throw.
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateItemsAsync_TModel_Overload_Should_Not_Throw_When_Collection_Binding_Is_Unreadable()
    {
        // Arrange - same unreadable collection binding, hitting the public ValidateItemsAsync(TModel,
        // IServiceProvider) overload, which re-resolves the accessor itself rather than reusing a
        // snapshot ValidateAllAsync already read.
        var itemForm = FormBuilder<OrderItemModel>.Create()
            .AddField(x => x.ProductName, field => field.Required("Product name is required"))
            .Build();
        var config = new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.Details!.Items)
        {
            ItemFormConfiguration = itemForm
        };
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);
        var model = new OrderModel { Details = null };
        var services = A.Fake<IServiceProvider>();

        // Act
        var errors = await validator.ValidateItemsAsync(model, services);

        // Assert
        errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAllAsync_Should_Propagate_An_Exception_From_A_Genuinely_Faulting_Collection_Accessor()
    {
        // Arrange - a broken collection getter, not a null intermediate. Before #425 it was read as
        // "zero items", which with the default MinItems=0 validated as passing.
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(
            new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.FaultingItems));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            validator.ValidateAllAsync(new OrderModel(), A.Fake<IServiceProvider>()));
    }

    [Fact]
    public async Task ValidateItemFieldAsync_Should_Propagate_An_Exception_From_A_Genuinely_Faulting_Collection_Accessor()
    {
        // Arrange
        var config = new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.FaultingItems)
        {
            ItemFormConfiguration = FormBuilder<OrderItemModel>.Create()
                .AddField(x => x.ProductName, field => field.Required("Product name is required"))
                .Build()
        };
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            validator.ValidateItemFieldAsync(new OrderModel(), 0, "ProductName", A.Fake<IServiceProvider>()));
    }

    [Fact]
    public async Task ValidateItemsAsync_TModel_Overload_Should_Propagate_An_Exception_From_A_Genuinely_Faulting_Collection_Accessor()
    {
        // Arrange
        var config = new CollectionFieldConfiguration<OrderModel, OrderItemModel>(x => x.FaultingItems)
        {
            ItemFormConfiguration = FormBuilder<OrderItemModel>.Create()
                .AddField(x => x.ProductName, field => field.Required("Product name is required"))
                .Build()
        };
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(config);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            validator.ValidateItemsAsync(new OrderModel(), A.Fake<IServiceProvider>()));
    }

    [Fact]
    public async Task ValidateAllAsync_Should_Propagate_An_Exception_From_A_Genuinely_Faulting_Item_Field_Accessor()
    {
        // Arrange - the collection reads fine; one item field's getter is broken. This goes through
        // the per-item traversal's hoisted-getter TryInvoke rather than TryReadCollection (#425).
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(CreateFaultingItemFieldConfig());
        var model = new OrderModel { Items = new List<OrderItemModel> { new() } };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            validator.ValidateAllAsync(model, A.Fake<IServiceProvider>()));
    }

    [Fact]
    public async Task ValidateItemFieldAsync_Should_Propagate_An_Exception_From_A_Genuinely_Faulting_Item_Field_Accessor()
    {
        // Arrange - same broken item field, through the single-cell path's TryGetValue (#425).
        var validator = new CollectionFieldValidator<OrderModel, OrderItemModel>(CreateFaultingItemFieldConfig());
        var model = new OrderModel { Items = new List<OrderItemModel> { new() } };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            validator.ValidateItemFieldAsync(model, 0, "Faulting", A.Fake<IServiceProvider>()));
    }

    private static CollectionFieldConfiguration<OrderModel, OrderItemModel> CreateFaultingItemFieldConfig()
        => new(x => x.Items)
        {
            ItemFormConfiguration = FormBuilder<OrderItemModel>.Create()
                .AddField(x => x.Faulting, field => field.Required("Faulting is required"))
                .Build()
        };

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

        // Unrelated to Items above - exists so a collection field can be bound through a nested path
        // (`x => x.Details!.Items`) whose intermediate is null (#408). OrderModel keeps its own
        // top-level Items property so CollectionFieldConfiguration's setter-building step (which
        // resolves the setter by the expression's last member name against TModel directly) still
        // finds a same-named, same-typed property to bind - the tests using this nested path never
        // call CollectionSetter, so which property it actually targets is irrelevant to them.
        public OrderDetailModel? Details { get; set; }

        // A genuinely broken collection getter (#425). The no-op setter exists only because
        // CollectionFieldConfiguration's constructor derives CollectionSetter from the same member.
        public List<OrderItemModel> FaultingItems
        {
            get => throw new InvalidOperationException("boom");
            set { }
        }
    }

    public class OrderDetailModel
    {
        public List<OrderItemModel> Items { get; set; } = new();
    }

    public class OrderItemModel
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 0m;
        public decimal TotalPrice => Quantity * UnitPrice;
        public NestedOrderDetail? Nested { get; set; }

        // A genuinely broken item field getter (#425) - only read by tests that bind it.
        public string Faulting => throw new InvalidOperationException("boom");
    }

    public class NestedOrderDetail
    {
        public string Value { get; set; } = "";
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
