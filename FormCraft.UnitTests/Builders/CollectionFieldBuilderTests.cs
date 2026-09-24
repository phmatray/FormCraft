namespace FormCraft.UnitTests.Builders;

public class CollectionFieldBuilderTests
{
    [Fact]
    public void AddCollectionField_Should_Add_Collection_To_Configuration()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items)
            .Build();

        // Assert
        var collectionConfig = config as ICollectionFormConfiguration<OrderModel>;
        collectionConfig.ShouldNotBeNull();
        collectionConfig.CollectionFields.Count.ShouldBe(1);
        collectionConfig.CollectionFields[0].FieldName.ShouldBe("Items");
    }

    [Fact]
    public void AddCollectionField_Should_Return_FormBuilder_For_Chaining()
    {
        // Arrange
        var builder = FormBuilder<OrderModel>.Create();

        // Act
        var result = builder.AddCollectionField(x => x.Items);

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void AddCollectionField_Should_Support_AllowAdd()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .AllowAdd("Add Order Item"))
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        var field = collectionConfig.CollectionFields[0];
        field.CanAdd.ShouldBeTrue();
        field.AddButtonText.ShouldBe("Add Order Item");
    }

    [Fact]
    public void AddCollectionField_Should_Support_AllowRemove()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .AllowRemove())
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        collectionConfig.CollectionFields[0].CanRemove.ShouldBeTrue();
    }

    [Fact]
    public void AddCollectionField_Should_Support_AllowReorder()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .AllowReorder())
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        collectionConfig.CollectionFields[0].CanReorder.ShouldBeTrue();
    }

    [Fact]
    public void AddCollectionField_Should_Support_MinMaxItems()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithMinItems(1)
                .WithMaxItems(10))
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        var field = collectionConfig.CollectionFields[0];
        field.MinItems.ShouldBe(1);
        field.MaxItems.ShouldBe(10);
    }

    [Fact]
    public void AddCollectionField_Should_Support_WithLabel()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Order Items"))
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        collectionConfig.CollectionFields[0].Label.ShouldBe("Order Items");
    }

    [Fact]
    public void AddCollectionField_Should_Support_EmptyText()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithEmptyText("No items yet"))
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        collectionConfig.CollectionFields[0].EmptyText.ShouldBe("No items yet");
    }

    [Fact]
    public void AddCollectionField_Should_Support_WithItemForm()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.Required())
                    .AddField(x => x.Quantity, field => field.WithRange(1, 100))
                    .AddField(x => x.UnitPrice)))
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        var collectionField = collectionConfig.CollectionFields[0] as CollectionFieldConfiguration<OrderModel, OrderItemModel>;
        collectionField.ShouldNotBeNull();
        collectionField.ItemFormConfiguration.ShouldNotBeNull();
        collectionField.ItemFormConfiguration!.Fields.Count.ShouldBe(3);
    }

    [Fact]
    public void AddCollectionField_Should_Assign_Correct_Order()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddField(x => x.OrderNumber)
            .AddCollectionField(x => x.Items)
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        config.Fields[0].Order.ShouldBe(0);
        collectionConfig.CollectionFields[0].Order.ShouldBe(1);
    }

    [Fact]
    public void AddCollectionField_Should_Have_Default_Values()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items)
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        var field = collectionConfig.CollectionFields[0];
        field.CanAdd.ShouldBeFalse();
        field.CanRemove.ShouldBeFalse();
        field.CanReorder.ShouldBeFalse();
        field.MinItems.ShouldBe(0);
        field.MaxItems.ShouldBe(0);
        field.IsVisible.ShouldBeTrue();
        field.AddButtonText.ShouldBe("Add Item");
    }

    [Fact]
    public void CollectionField_Accessor_Should_Return_Collection()
    {
        // Arrange
        var model = new OrderModel
        {
            Items = new List<OrderItemModel>
            {
                new() { ProductName = "Widget", Quantity = 2 }
            }
        };

        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items)
            .Build();

        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;
        var collectionField = (CollectionFieldConfiguration<OrderModel, OrderItemModel>)collectionConfig.CollectionFields[0];

        // Act
        var items = collectionField.CollectionAccessor(model);

        // Assert
        items.Count.ShouldBe(1);
        items[0].ProductName.ShouldBe("Widget");
    }

    [Fact]
    public void CollectionField_ItemType_Should_Return_Correct_Type()
    {
        // Arrange & Act
        var config = FormBuilder<OrderModel>.Create()
            .AddCollectionField(x => x.Items)
            .Build();

        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)config;

        // Assert
        collectionConfig.CollectionFields[0].ItemType.ShouldBe(typeof(OrderItemModel));
    }

    [Fact]
    public void Full_Api_Example_Should_Match_Proposed_Pattern()
    {
        // This test verifies the full API matches the owner's proposed pattern from the issue.
        // Arrange & Act
        var formConfig = FormBuilder<OrderModel>.Create()
            .AddField(x => x.OrderNumber)
            .AddCollectionField(x => x.Items, collection => collection
                .AllowAdd()
                .AllowRemove()
                .WithMinItems(1)
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.Required())
                    .AddField(x => x.Quantity, field => field.WithRange(1, 100))
                    .AddField(x => x.UnitPrice, field => field.WithRange(0.01m, 10000m))))
            .Build();

        // Assert
        formConfig.Fields.Count.ShouldBe(1);
        formConfig.Fields[0].FieldName.ShouldBe("OrderNumber");

        var collectionConfig = (ICollectionFormConfiguration<OrderModel>)formConfig;
        collectionConfig.CollectionFields.Count.ShouldBe(1);

        var collectionField = (CollectionFieldConfiguration<OrderModel, OrderItemModel>)collectionConfig.CollectionFields[0];
        collectionField.FieldName.ShouldBe("Items");
        collectionField.CanAdd.ShouldBeTrue();
        collectionField.CanRemove.ShouldBeTrue();
        collectionField.MinItems.ShouldBe(1);
        collectionField.ItemFormConfiguration.ShouldNotBeNull();
        collectionField.ItemFormConfiguration!.Fields.Count.ShouldBe(3);
    }

    [Fact]
    public void AddCollectionField_Should_Not_Throw_For_A_Nested_Path_On_A_Model_With_No_Top_Level_Items()
    {
        // Arrange & Act - NestedOnlyModel declares no top-level Items property, so this only
        // constructs at all once CollectionSetter is built from the full member chain (#420).
        var config = FormBuilder<NestedOnlyModel>.Create()
            .AddCollectionField(x => x.Details.Items)
            .Build();

        // Assert
        var collectionConfig = (ICollectionFormConfiguration<NestedOnlyModel>)config;
        var field = (CollectionFieldConfiguration<NestedOnlyModel, OrderItemModel>)collectionConfig.CollectionFields[0];
        var model = new NestedOnlyModel();
        var items = new List<OrderItemModel> { new() { ProductName = "Widget" } };

        field.CollectionSetter(model, items);

        model.Details.Items.ShouldBeSameAs(items);
    }

    [Fact]
    public void AddCollectionField_Should_Write_The_Nested_Property_Not_An_Unrelated_Top_Level_Items()
    {
        // Arrange & Act - DecoyModel also declares a top-level Items of a compatible type; the
        // setter must target the nested one bound by the expression, not the decoy (#420).
        var config = FormBuilder<DecoyModel>.Create()
            .AddCollectionField(x => x.Details.Items)
            .Build();

        var collectionConfig = (ICollectionFormConfiguration<DecoyModel>)config;
        var field = (CollectionFieldConfiguration<DecoyModel, OrderItemModel>)collectionConfig.CollectionFields[0];
        var model = new DecoyModel();
        var decoyReference = model.Items;
        var items = new List<OrderItemModel> { new() { ProductName = "Widget" } };

        // Act
        field.CollectionSetter(model, items);

        // Assert
        model.Details.Items.ShouldBeSameAs(items);
        model.Items.ShouldBeSameAs(decoyReference);
    }

    // Test models matching the owner's proposed API
    public class OrderModel
    {
        public string OrderNumber { get; set; } = "";
        public List<OrderItemModel> Items { get; set; } = new();
        public decimal TotalAmount => Items.Sum(x => x.TotalPrice);
    }

    public class OrderItemModel
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; } = 0m;
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    // Models for #420: a nested collection binding (x => x.Details.Items) on a TModel with no
    // top-level Items of its own, and a sibling with a same-named, same-typed decoy.
    public class NestedOnlyModel
    {
        public OrderDetailModel Details { get; set; } = new();
    }

    public class DecoyModel
    {
        public List<OrderItemModel> Items { get; set; } = new();

        public OrderDetailModel Details { get; set; } = new();
    }

    public class OrderDetailModel
    {
        public List<OrderItemModel> Items { get; set; } = new();
    }
}
