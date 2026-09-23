using static FormCraft.TestSupport.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// A hidden collection field must not block submission with invisible errors (#342) - the MudBlazor
/// counterpart to
/// <c>CollectionValidationPassTests.ValidateModelAsync_Should_Not_Validate_A_Hidden_Collection</c> in
/// core, exercised through the real rendered form rather than the validator component directly.
/// </summary>
public class CollectionFieldVisibilitySubmitTests : MudBlazorTestBase
{
    [Fact]
    public async Task Submitting_Should_Succeed_When_The_Only_Invalid_Field_Is_A_Hidden_Collection()
    {
        // Arrange - a required, empty item field inside a collection the form does not render.
        var model = NewOrder();
        var config = TextItemForm(field => field.Required("Product name is required"));
        ((ICollectionFormConfiguration<OrderModel>)config).CollectionFields[0].IsVisible = false;

        var submitted = false;
        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.OnValidSubmit, (OrderModel _) => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert - nothing on screen names the hidden collection, and submit is not blocked by it.
        submitted.ShouldBeTrue();
        component.Markup.ShouldNotContain("Product name is required");
    }
}
