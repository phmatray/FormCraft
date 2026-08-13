using AngleSharp.Dom;
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
    private const string MoveUpSelector = "button[aria-label='Move up']";
    private const string MoveDownSelector = "button[aria-label='Move down']";

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

    [Fact]
    public async Task Moving_An_Item_To_The_Top_Should_Focus_Its_Move_Down_Button()
    {
        // Arrange - the disable-self variant: the item lands at index 0, so the Move-up button the
        // user just pressed becomes Disabled under their finger. Browsers drop focus from a
        // newly-disabled element, so this is the same 2.4.3 failure as an unmount.
        var component = RenderCollection(3, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act
        await component.InvokeAsync(() => component.FindAll(MoveUpSelector)[1].Click());

        // Assert - focus moved to the counterpart that is still enabled on that row
        FocusCount().ShouldBe(focusesBefore + 1);
        var focusedId = LastFocusedElementId();

        var expectedId = await LearnElementIdAsync(component, MoveButtons(component, "Move down")[0]);
        focusedId.ShouldBe(expectedId);
    }

    [Fact]
    public async Task Moving_An_Item_To_The_Bottom_Should_Focus_Its_Move_Up_Button()
    {
        // Arrange - the mirror case at the other end of the list
        var component = RenderCollection(3, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act - move the middle item down, landing it last
        await component.InvokeAsync(() => component.FindAll(MoveDownSelector)[1].Click());

        // Assert
        FocusCount().ShouldBe(focusesBefore + 1);
        var focusedId = LastFocusedElementId();

        var expectedId = await LearnElementIdAsync(component, MoveButtons(component, "Move up")[2]);
        focusedId.ShouldBe(expectedId);
    }

    [Fact]
    public async Task Moving_An_Item_Within_The_Middle_Should_Follow_It_To_Its_New_Row()
    {
        // Arrange - four rows, so the moved item lands somewhere both controls stay enabled. Focus
        // should follow the ITEM to its new row rather than sit on the index the user started at,
        // which would silently now control a different item.
        var component = RenderCollection(4, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act - move the third item up; it lands at index 1, still mid-list
        await component.InvokeAsync(() => component.FindAll(MoveUpSelector)[2].Click());

        // Assert
        FocusCount().ShouldBe(focusesBefore + 1);
        var focusedId = LastFocusedElementId();

        var expectedId = await LearnElementIdAsync(component, MoveButtons(component, "Move up")[1]);
        focusedId.ShouldBe(expectedId);
    }

    [Fact]
    public async Task A_Single_Item_Cannot_Be_Moved_So_Nothing_Is_Focused()
    {
        // Arrange - both move buttons are Disabled with one row, and the handlers early-return, so
        // there is no state change and nothing to move focus to. Pinned so the "no enabled
        // counterpart" fallback is not mistaken for a reachable path through a move.
        var component = RenderCollection(1, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act & Assert - the controls are disabled, and no focus request is issued
        component.FindAll(MoveUpSelector)[0].HasAttribute("disabled").ShouldBeTrue();
        component.FindAll(MoveDownSelector)[0].HasAttribute("disabled").ShouldBeTrue();
        FocusCount().ShouldBe(focusesBefore);
    }

    [Fact]
    public async Task Adding_The_Last_Allowed_Row_Should_Move_Focus_Into_That_Row()
    {
        // Arrange - MaxItems 2 with one row: the Add click reaches the max, so Add unmounts itself.
        var component = RenderCollection(1, collection => collection
            .AllowAdd()
            .AllowRemove()
            .WithMaxItems(2));

        var buttonIdsBefore = await LearnEveryButtonIdAsync(component);
        var focusesBefore = FocusCount();

        // Act
        await component.InvokeAsync(() => AddButtonElement(component).Click());

        // Assert - Add is gone, and focus was moved deliberately
        component.FindAll("button")
            .Any(b => b.TextContent.Contains("Add Item"))
            .ShouldBeFalse();
        FocusCount().ShouldBe(focusesBefore + 1);
        var focusedId = LastFocusedElementId();

        // ...into the new row itself rather than onto any control. The row container is the target
        // because the row's *fields* render through IFieldRendererService and expose no reference to
        // aim at; landing on the row puts the user's next Tab into those fields, and deliberately
        // not on the row's Delete button, where Enter would undo the add.
        var buttonIdsAfter = await LearnEveryButtonIdAsync(component);
        focusedId.ShouldNotBeOneOf([.. buttonIdsBefore.Concat(buttonIdsAfter)]);
    }

    [Fact]
    public async Task Removing_A_Row_In_The_Second_Collection_Should_Not_Move_Focus_Into_The_First()
    {
        // Arrange - two collection fields on one form. Every reference here is per-component and
        // per-index; a static or form-level one would pass every other test in this file and land
        // focus in the wrong field here.
        var model = new TwoCollectionModel();
        for (var i = 0; i < 3; i++)
        {
            model.First.Add(new MixedItem());
            model.Second.Add(new MixedItem());
        }

        var component = Render<FormCraftComponent<TwoCollectionModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, TwoCollectionForm()));

        var fields = component.FindComponents<CollectionFieldComponent<TwoCollectionModel, MixedItem>>();
        fields.Count.ShouldBe(2);

        var firstFieldIds = new List<string>();
        foreach (var button in DeleteButtonsIn(fields[0]))
        {
            firstFieldIds.Add(await LearnElementIdAsync(component, button));
        }

        var focusesBefore = FocusCount();

        // Act - remove the middle row of the SECOND collection
        await component.InvokeAsync(() => fields[1].FindAll(DeleteSelector)[1].Click());

        // Assert - one focus request, and it landed in the second field, not the first
        FocusCount().ShouldBe(focusesBefore + 1);
        var focusedId = LastFocusedElementId();
        focusedId.ShouldNotBeOneOf([.. firstFieldIds]);

        var expectedId = await LearnElementIdAsync(component, DeleteButtonsIn(fields[1])[1]);
        focusedId.ShouldBe(expectedId);

        // ...and the first collection was left entirely alone
        fields[0].FindAll(DeleteSelector).Count.ShouldBe(3);
    }

    private static List<MudIconButton> DeleteButtonsIn(
        IRenderedComponent<CollectionFieldComponent<TwoCollectionModel, MixedItem>> field) =>
        field.FindComponents<MudIconButton>()
            .Where(b => b.Instance.UserAttributes.TryGetValue("aria-label", out var label)
                        && (label as string) == "Remove item")
            .Select(b => b.Instance)
            .ToList();

    /// <summary>
    /// Two collections over one model. Not in <c>CollectionItemFixture</c> because no other suite
    /// needs a second collection — the fixture's models each hold exactly one.
    /// </summary>
    private static IFormConfiguration<TwoCollectionModel> TwoCollectionForm() =>
        FormBuilder<TwoCollectionModel>
            .Create()
            .AddCollectionField(x => x.First, collection => collection
                .WithLabel("First")
                .AllowRemove()
                .WithItemForm(item => item.AddField(x => x.Name, field => field.WithLabel("Name"))))
            .AddCollectionField(x => x.Second, collection => collection
                .WithLabel("Second")
                .AllowRemove()
                .WithItemForm(item => item.AddField(x => x.Name, field => field.WithLabel("Name"))))
            .Build();

    private sealed class TwoCollectionModel
    {
        public List<MixedItem> First { get; set; } = new();

        public List<MixedItem> Second { get; set; } = new();
    }

    private async Task<List<string>> LearnEveryButtonIdAsync(
        IRenderedComponent<FormCraftComponent<MixedItemModel>> component)
    {
        var ids = new List<string>();
        foreach (var button in component.FindComponents<MudIconButton>().Select(b => b.Instance))
        {
            ids.Add(await LearnElementIdAsync(component, button));
        }

        foreach (var button in component.FindComponents<MudButton>().Select(b => b.Instance))
        {
            ids.Add(await LearnElementIdAsync(component, button));
        }

        return ids;
    }

    /// <summary>
    /// The collection's <b>Add</b> button element. Note it is <i>not</i> inside a
    /// <c>.mud-toolbar</c> — that is the upload component's layout; the collection renders Add in
    /// its own header.
    /// </summary>
    private static IElement AddButtonElement(
        IRenderedComponent<FormCraftComponent<MixedItemModel>> component) =>
        component.FindAll("button").First(b => b.TextContent.Contains("Add Item"));

    private static List<MudIconButton> MoveButtons(
        IRenderedComponent<FormCraftComponent<MixedItemModel>> component,
        string ariaLabel) =>
        component.FindComponents<MudIconButton>()
            .Where(b => b.Instance.UserAttributes.TryGetValue("aria-label", out var label)
                        && (label as string) == ariaLabel)
            .Select(b => b.Instance)
            .ToList();

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
