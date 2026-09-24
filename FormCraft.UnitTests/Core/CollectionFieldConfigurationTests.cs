namespace FormCraft.UnitTests.Core;

public class CollectionFieldConfigurationTests
{
    [Fact]
    public void Constructor_Should_Not_Throw_For_A_Nested_Path_When_TModel_Has_No_Matching_Top_Level_Property()
    {
        // Arrange & Act - before the fix, building the setter from only the expression's last member
        // name ("Items") against TModel directly threw ArgumentException here, because
        // NoTopLevelItemsModel declares no top-level Items property (#420).
        var config = new CollectionFieldConfiguration<NoTopLevelItemsModel, Widget>(x => x.Details.Items);

        // Assert
        config.FieldName.ShouldBe("Items");
    }

    [Fact]
    public void CollectionSetter_Should_Write_Through_The_Full_Nested_Path()
    {
        // Arrange
        var config = new CollectionFieldConfiguration<NoTopLevelItemsModel, Widget>(x => x.Details.Items);
        var model = new NoTopLevelItemsModel();
        var widgets = new List<Widget> { new() { Name = "Gadget" } };

        // Act
        config.CollectionSetter(model, widgets);

        // Assert
        model.Details.Items.ShouldBeSameAs(widgets);
    }

    [Fact]
    public void CollectionSetter_Should_Write_The_Nested_Property_Not_A_Same_Named_Decoy_On_TModel()
    {
        // Arrange - TModel also happens to declare a top-level "Items" property of a compatible type.
        // Before the fix this coincidence was the only thing that let a nested `x => x.Details.Items`
        // binding construct at all, and CollectionSetter silently targeted the decoy instead of the
        // real nested list while CollectionAccessor kept reading the real one (#420).
        var config = new CollectionFieldConfiguration<DecoyItemsModel, Widget>(x => x.Details.Items);
        var model = new DecoyItemsModel();
        var decoyReference = model.Items;
        var widgets = new List<Widget> { new() { Name = "Gadget" } };

        // Act
        config.CollectionSetter(model, widgets);

        // Assert
        model.Details.Items.ShouldBeSameAs(widgets);
        model.Items.ShouldBeSameAs(decoyReference);
    }

    [Fact]
    public void CollectionSetter_Should_Write_A_Direct_Top_Level_Property_Unchanged()
    {
        // Arrange - regression: a non-nested expression behaves exactly as before the fix.
        var config = new CollectionFieldConfiguration<DecoyItemsModel, Widget>(x => x.Items);
        var model = new DecoyItemsModel();
        var widgets = new List<Widget> { new() { Name = "Gadget" } };

        // Act
        config.CollectionSetter(model, widgets);

        // Assert
        model.Items.ShouldBeSameAs(widgets);
    }

    private class NoTopLevelItemsModel
    {
        public NestedDetails Details { get; set; } = new();
    }

    private class DecoyItemsModel
    {
        public List<Widget> Items { get; set; } = new();

        public NestedDetails Details { get; set; } = new();
    }

    private class NestedDetails
    {
        public List<Widget> Items { get; set; } = new();
    }

    private class Widget
    {
        public string Name { get; set; } = "";
    }
}
