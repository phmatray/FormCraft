using FormCraft.ForFluentUI.UnitTests.TestSupport;
using static FormCraft.TestSupport.CollectionItemFixture;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// Tests that the Fluent collection field's row controls move keyboard focus deliberately when
/// activating them removes or disables the control the user is standing on (#337), mirroring
/// <c>FormCraft.ForMudBlazor.UnitTests.Fields.CollectionFocusTests</c> (#318) for this adapter's
/// wrapper-element focus mechanism (see <see cref="FocusAssertingTestBase"/> remarks).
/// </summary>
public class CollectionFocusTests : FocusAssertingTestBase
{
    private const string DeleteSelector = "[data-testid=formcraft-collection-remove]";
    private const string AddSelector = "[data-testid=formcraft-collection-add]";

    [Fact]
    public async Task Removing_A_Middle_Row_Should_Focus_The_Delete_Control_That_Takes_Its_Place()
    {
        // Arrange - three rows; the user is standing on row 1's delete control
        var (component, field) = RenderCollection(3, collection => collection.AllowAdd().AllowRemove());
        component.FindAll(DeleteSelector).Count.ShouldBe(3);
        var focusesBefore = FocusCount();

        // Act - remove the middle row, which unmounts the control that was activated
        await component.InvokeAsync(() => component.FindAll(DeleteSelector)[1].Click());

        // Assert - two rows left, and exactly one focus request was issued
        component.FindAll(DeleteSelector).Count.ShouldBe(2);
        FocusCount().ShouldBe(focusesBefore + 1);

        // ...and it went to the delete control now occupying the vacated slot
        LastFocusedElementId().ShouldBe(field.Instance.DeleteTargetAt(1)!.Value.Id);
    }

    [Fact]
    public async Task Removing_The_Last_Row_Should_Fall_Back_To_The_Previous_Rows_Delete_Control()
    {
        // Arrange
        var (component, field) = RenderCollection(3, collection => collection.AllowAdd().AllowRemove());
        var focusesBefore = FocusCount();

        // Act - remove the last row
        await component.InvokeAsync(() => component.FindAll(DeleteSelector)[2].Click());

        // Assert - one focus request, stepping backwards onto the new last row rather than off the
        // end of the list
        component.FindAll(DeleteSelector).Count.ShouldBe(2);
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(field.Instance.DeleteTargetAt(1)!.Value.Id);
    }

    [Fact]
    public async Task Removing_Down_To_MinItems_Should_Focus_Add_Because_Every_Delete_Unmounts()
    {
        // Arrange - MinItems 2 with 3 rows: one removal makes HasReachedMin true, which unmounts
        // EVERY row's delete control at once, not just the one that was clicked
        var (component, field) = RenderCollection(3, collection => collection
            .AllowAdd()
            .AllowRemove()
            .WithMinItems(2));

        var addId = field.Instance.AddTarget!.Value.Id;
        var focusesBefore = FocusCount();

        // Act
        await component.InvokeAsync(() => component.FindAll(DeleteSelector)[0].Click());

        // Assert - no delete control survives, so Add is the affordance that remains
        component.FindAll(DeleteSelector).ShouldBeEmpty();
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(addId);
    }

    [Fact]
    public async Task With_Neither_Delete_Nor_Add_Surviving_Focus_Should_Land_On_The_Collection_Header()
    {
        // Arrange - MinItems 2 and no Add: after the removal there is no control left in the field at
        // all. Focus still has to go somewhere deliberate, so the collection's own header takes it.
        var (component, field) = RenderCollection(3, collection => collection
            .AllowRemove()
            .WithMinItems(2));

        component.FindAll(AddSelector).ShouldBeEmpty();
        var headerId = field.Instance.HeaderTarget.Id;
        var focusesBefore = FocusCount();

        // Act
        await component.InvokeAsync(() => component.FindAll(DeleteSelector)[0].Click());

        // Assert - nothing focusable is left in the field, yet focus was still moved deliberately,
        // onto the header rather than a stale reference to a control that has unmounted
        component.FindAll(DeleteSelector).ShouldBeEmpty();
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(headerId);
    }

    private (IRenderedComponent<FormCraftComponent<MixedItemModel>> Component,
        IRenderedComponent<FluentUICollectionFieldComponent<MixedItemModel, MixedItem>> Field) RenderCollection(
        int rows,
        Action<CollectionFieldBuilder<MixedItemModel, MixedItem>> configureCollection)
    {
        var model = NewMixedItems(Enumerable.Range(0, rows).Select(_ => new MixedItem()).ToArray());
        var config = MultiFieldItemForm(configureCollection: configureCollection);

        var component = Render<FormCraftComponent<MixedItemModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var field = component.FindComponent<FluentUICollectionFieldComponent<MixedItemModel, MixedItem>>();
        return (component, field);
    }
}
