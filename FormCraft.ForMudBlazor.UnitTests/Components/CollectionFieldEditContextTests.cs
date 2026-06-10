namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Tests for nested FieldIdentifier integration of collection fields (#91): item field edits
/// must notify the parent EditContext using identifiers like "Items[0].ProductName" (rooted at
/// the parent model, per Blazor convention), validation messages must attach to those
/// identifiers, and modification tracking must reflect nested edits.
/// </summary>
public class CollectionFieldEditContextTests : MudBlazorTestBase
{
    [Fact]
    public void Editing_Item_Field_Should_Mark_Nested_FieldIdentifier_As_Modified()
    {
        // Arrange
        var model = new OrderModel { Items = { new OrderItem() } };
        EditContext? editContext = null;

        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BuildConfiguration())
            .Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        // Act - edit the first item's ProductName input
        component.FindAll("input")[0].Input("Widget");

        // Assert - the nested identifier is rooted at the parent model and tracked as modified
        editContext.ShouldNotBeNull();
        var nestedField = new FieldIdentifier(model, "Items[0].ProductName");
        editContext!.IsModified(nestedField).ShouldBeTrue();
        model.Items[0].ProductName.ShouldBe("Widget");
    }

    [Fact]
    public async Task ValidateAsync_Should_Attach_Messages_To_Nested_FieldIdentifiers()
    {
        // Arrange - one item with an empty required ProductName
        var model = new OrderModel { Items = { new OrderItem() } };
        EditContext? editContext = null;

        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BuildConfiguration())
            .Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        // Act
        var isValid = true;
        await component.InvokeAsync(async () => isValid = await component.Instance.ValidateAsync());

        // Assert - the message is attached to the nested identifier...
        isValid.ShouldBeFalse();
        var nestedField = new FieldIdentifier(model, "Items[0].ProductName");
        editContext!.GetValidationMessages(nestedField).ShouldContain("Product name is required");

        // ...and surfaces in the rendered markup next to the item field
        component.WaitForAssertion(() => component.Markup.ShouldContain("Product name is required"));
    }

    [Fact]
    public async Task Correcting_Item_Field_Should_Clear_Nested_Validation_Message()
    {
        // Arrange - start invalid and validate to populate nested messages
        var model = new OrderModel { Items = { new OrderItem() } };
        EditContext? editContext = null;

        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BuildConfiguration())
            .Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        await component.InvokeAsync(() => component.Instance.ValidateAsync());
        var nestedField = new FieldIdentifier(model, "Items[0].ProductName");
        editContext!.GetValidationMessages(nestedField).ShouldNotBeEmpty();

        // Act - correct the value through the UI
        component.FindAll("input")[0].Input("Widget");

        // Assert - the stale nested message clears as soon as the field is corrected
        component.WaitForAssertion(() =>
            editContext.GetValidationMessages(nestedField).ShouldBeEmpty());
    }

    [Fact]
    public async Task Flat_Collection_Level_Messages_Should_Still_Be_Attached()
    {
        // Arrange - nested identifiers are ADDITIVE: the existing flat messages on the
        // collection field name must keep working (no regression)
        var model = new OrderModel { Items = { new OrderItem() } };
        EditContext? editContext = null;

        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BuildConfiguration())
            .Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        // Act
        await component.InvokeAsync(() => component.Instance.ValidateAsync());

        // Assert
        var flatMessages = editContext!.GetValidationMessages(editContext.Field("Items")).ToList();
        flatMessages.ShouldNotBeEmpty();
        flatMessages.ShouldContain(m => m.Contains("Product name is required"));
    }

    [Fact]
    public void Collection_Field_Should_Render_Items_Without_Regression()
    {
        // Arrange / Act - the component must keep rendering item sub-forms as before
        var model = new OrderModel { Items = { new OrderItem { ProductName = "Widget" } } };

        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BuildConfiguration()));

        // Assert
        component.Markup.ShouldContain("Item 1");
        component.FindAll("input")[0].GetAttribute("value").ShouldBe("Widget");
    }

    private static IFormConfiguration<OrderModel> BuildConfiguration()
    {
        return FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field
                        .WithLabel("Product")
                        .Required("Product name is required"))))
            .Build();
    }

    private class OrderModel
    {
        public List<OrderItem> Items { get; set; } = new();
    }

    private class OrderItem
    {
        public string ProductName { get; set; } = string.Empty;
    }
}
