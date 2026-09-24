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

        // Assert - FieldName is now qualified by the full member chain, not just the last segment
        // (#428), so this reads "Details.Items" rather than the pre-#428 "Items".
        config.FieldName.ShouldBe("Details.Items");
    }

    [Fact]
    public void FieldName_Should_Not_Collide_Between_Two_Differently_Nested_Collections_Sharing_A_Last_Segment()
    {
        // Arrange & Act - Billing.Items and Shipping.Items both end in "Items"; before #428 both
        // reported FieldName == "Items", so DynamicFormValidator's by-name lookup could not tell them
        // apart and would silently validate the wrong collection.
        var billing = new CollectionFieldConfiguration<TwoSectionModel, Widget>(x => x.Billing.Items);
        var shipping = new CollectionFieldConfiguration<TwoSectionModel, Widget>(x => x.Shipping.Items);

        // Assert
        billing.FieldName.ShouldBe("Billing.Items");
        shipping.FieldName.ShouldBe("Shipping.Items");
        billing.FieldName.ShouldNotBe(shipping.FieldName);
    }

    [Fact]
    public void FieldName_Should_Qualify_By_The_Whole_Chain_For_Three_Or_More_Levels()
    {
        // Arrange & Act - a three-level path must qualify against the WHOLE chain, or x.A.B.Items and
        // x.C.B.Items would collide the same way a two-level path does (Spec edge case).
        var config = new CollectionFieldConfiguration<ThreeLevelModel, Widget>(x => x.A.B.Items);

        // Assert
        config.FieldName.ShouldBe("A.B.Items");
    }

    [Fact]
    public void FieldName_Should_Be_Unchanged_For_A_Direct_Top_Level_Collection()
    {
        // Arrange & Act - regression: a single, non-nested segment produces exactly the string it
        // always has (Acceptance Criterion 3).
        var config = new CollectionFieldConfiguration<DirectItemsModel, Widget>(x => x.Items);

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

    private class TwoSectionModel
    {
        public Section Billing { get; set; } = new();
        public Section Shipping { get; set; } = new();
    }

    private class Section
    {
        public List<Widget> Items { get; set; } = new();
    }

    private class ThreeLevelModel
    {
        public LevelA A { get; set; } = new();
    }

    private class LevelA
    {
        public LevelB B { get; set; } = new();
    }

    private class LevelB
    {
        public List<Widget> Items { get; set; } = new();
    }

    private class DirectItemsModel
    {
        public List<Widget> Items { get; set; } = new();
    }

    private class Widget
    {
        public string Name { get; set; } = "";
    }
}
