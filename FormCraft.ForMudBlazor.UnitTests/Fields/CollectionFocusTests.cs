using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that the collection field's row controls move keyboard focus deliberately when activating
/// them removes or disables the control the user is standing on (#318).
/// </summary>
/// <remarks>
/// <para>
/// Four controls here have the shape #281 fixed on the upload Clear button: <b>delete</b> is gated on
/// <c>@if (CanRemove &amp;&amp; !HasReachedMin)</c> and its handler can falsify that; <b>Add</b> is
/// gated on <c>!HasReachedMax</c> and its handler can reach the max; and the two <b>move</b> buttons
/// become <c>Disabled</c> when the item lands at an end. All four leave focus on <c>&lt;body&gt;</c>
/// — WCAG 2.1 <b>2.4.3 Focus Order</b> (Level A).
/// </para>
/// <para>
/// <b>Why the expected id is learned *after* the removal.</b> The keyless <c>@for</c> loop does
/// reuse the element already at each position — measured: the delete buttons at indices 0 and 1 keep
/// their element ids across a removal — so learning the id beforehand would also work. These read
/// the component's focus id first and only then learn who is standing in the slot, because that
/// states the guarantee directly ("focus is on whichever delete button now occupies the slot")
/// rather than resting it on a diffing detail that is true but incidental.
/// </para>
/// </remarks>
public class CollectionFocusTests : FocusAssertingTestBase
{
    private const string DeleteSelector = "button[aria-label='Remove item']";

    [Fact]
    public async Task Removing_A_Middle_Row_Should_Focus_The_Delete_Button_That_Takes_Its_Place()
    {
        // Arrange - three rows; the user is standing on row 1's delete button
        var component = RenderCollection(3, collection => collection.AllowAdd().AllowRemove());
        DeleteButtons(component).Count.ShouldBe(3);
        var focusesBefore = FocusCount();

        // Act - remove the middle row, which unmounts the button that was activated
        await component.InvokeAsync(() => component.FindAll(DeleteSelector)[1].Click());

        // Assert - two rows left, and exactly one focus request was issued
        component.FindAll(DeleteSelector).Count.ShouldBe(2);
        FocusCount().ShouldBe(focusesBefore + 1);
        var focusedId = LastFocusedElementId();

        // ...and it went to the delete button now occupying the vacated slot
        var expectedId = await LearnElementIdAsync(component, DeleteButtons(component)[1]);
        focusedId.ShouldBe(expectedId);
    }

    [Fact]
    public async Task Removing_The_Last_Row_Should_Fall_Back_To_The_Previous_Rows_Delete_Button()
    {
        // Arrange - removing the final row leaves no button at that index, so the chain has to step
        // backwards rather than off the end
        var component = RenderCollection(3, collection => collection.AllowAdd().AllowRemove());
        var focusesBefore = FocusCount();

        // Act - remove the last row
        await component.InvokeAsync(() => component.FindAll(DeleteSelector)[2].Click());

        // Assert - one focus request, and it stepped backwards onto the new last row rather than
        // off the end of the list
        component.FindAll(DeleteSelector).Count.ShouldBe(2);
        FocusCount().ShouldBe(focusesBefore + 1);
        var focusedId = LastFocusedElementId();

        var expectedId = await LearnElementIdAsync(component, DeleteButtons(component)[1]);
        focusedId.ShouldBe(expectedId);
    }

    [Fact]
    public async Task Removing_Down_To_MinItems_Should_Focus_Add_Because_Every_Delete_Unmounts()
    {
        // Arrange - MinItems 2 with 3 rows: one removal makes HasReachedMin true, which unmounts
        // EVERY row's delete button at once, not just the one that was clicked
        var component = RenderCollection(3, collection => collection
            .AllowAdd()
            .AllowRemove()
            .WithMinItems(2));

        var addId = await LearnElementIdAsync(component, AddButton(component));
        var focusesBefore = FocusCount();

        // Act
        await component.InvokeAsync(() => component.FindAll(DeleteSelector)[0].Click());

        // Assert - no delete button survives, so Add is the affordance that remains
        component.FindAll(DeleteSelector).ShouldBeEmpty();
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(addId);
    }

    [Fact]
    public async Task With_Neither_Delete_Nor_Add_Surviving_Focus_Should_Land_On_The_Collection_Header()
    {
        // Arrange - MinItems 2 and no Add: after the removal there is no button left in the field at
        // all. Focus still has to go somewhere deliberate, so the collection's own header takes it —
        // it names the collection, so a screen reader says where the user now is.
        var component = RenderCollection(3, collection => collection
            .AllowRemove()
            .WithMinItems(2));

        component.FindAll(".mud-toolbar button").ShouldBeEmpty();
        var focusesBefore = FocusCount();

        // Act
        await component.InvokeAsync(() => component.FindAll(DeleteSelector)[0].Click());

        // Assert - nothing focusable is left in the field, yet focus was still moved deliberately
        component.FindAll(DeleteSelector).ShouldBeEmpty();
        FocusCount().ShouldBe(focusesBefore + 1);
    }

    private IRenderedComponent<FormCraftComponent<MixedItemModel>> RenderCollection(
        int rows,
        Action<CollectionFieldBuilder<MixedItemModel, MixedItem>> configureCollection)
    {
        var model = NewMixedItems(Enumerable.Range(0, rows).Select(_ => new MixedItem()).ToArray());
        var config = MultiFieldItemForm(configureCollection: configureCollection);

        return Render<FormCraftComponent<MixedItemModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));
    }

    private static List<MudIconButton> DeleteButtons(
        IRenderedComponent<FormCraftComponent<MixedItemModel>> component) =>
        component.FindComponents<MudIconButton>()
            .Where(b => b.Instance.UserAttributes.TryGetValue("aria-label", out var label)
                        && (label as string) == "Remove item")
            .Select(b => b.Instance)
            .ToList();

    private static MudButton AddButton(IRenderedComponent<FormCraftComponent<MixedItemModel>> component) =>
        component.FindComponents<MudButton>()
            .Select(b => b.Instance)
            .First(b => b.StartIcon == Icons.Material.Filled.Add);
}
