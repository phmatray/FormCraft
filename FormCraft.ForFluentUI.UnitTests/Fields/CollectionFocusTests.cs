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
    private const string MoveUpSelector = "[data-testid=formcraft-collection-move-up]";
    private const string MoveDownSelector = "[data-testid=formcraft-collection-move-down]";

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

    [Fact]
    public async Task Adding_A_Row_That_Leaves_Add_Standing_Should_Not_Steal_Focus_From_It()
    {
        // Arrange - MaxItems defaults to 0, so HasReachedMax is never true and Add does NOT unmount
        // itself. There is no 2.4.3 failure to fix, and moving focus anyway would push a user
        // building a list into the new row's header - which carries tabindex="-1" and so is OUTSIDE
        // the tab order, costing them a Shift+Tab back to Add for every single row.
        var (component, _) = RenderCollection(1, collection => collection.AllowAdd().AllowRemove());
        var focusesBefore = FocusCount();

        // Act
        await component.InvokeAsync(() => component.Find(AddSelector).Click());

        // Assert - the row was added, Add survived, and focus was left where the user put it
        component.FindAll(DeleteSelector).Count.ShouldBe(2);
        component.FindAll(AddSelector).Count.ShouldBe(1);
        FocusCount().ShouldBe(focusesBefore);
    }

    [Fact]
    public async Task Adding_The_Last_Allowed_Row_Should_Move_Focus_Into_That_Row()
    {
        // Arrange - MaxItems 2 with one row: the Add click reaches the max, so Add unmounts itself.
        var (component, field) = RenderCollection(1, collection => collection
            .AllowAdd()
            .AllowRemove()
            .WithMaxItems(2));

        // Act
        await component.InvokeAsync(() => component.Find(AddSelector).Click());

        // Assert - Add is gone, and focus moved deliberately into the new row's header - not onto
        // any control, since the row's own fields render through IFieldRendererService and expose no
        // reference to aim at, and landing on Delete would put Enter on "undo the add".
        component.FindAll(AddSelector).ShouldBeEmpty();
        FocusCount().ShouldBe(1);
        LastFocusedElementId().ShouldBe(field.Instance.RowHeaderTargetAt(1).Id);
    }

    [Fact]
    public async Task Moving_An_Item_To_The_Top_Should_Focus_Its_Move_Down_Control()
    {
        // Arrange - the disable-self variant: the item lands at index 0, so the Move-up control the
        // user just pressed becomes Disabled under their finger. Browsers drop focus from a
        // newly-disabled element, so this is the same 2.4.3 failure as an unmount.
        var (component, field) = RenderCollection(3, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act
        await component.InvokeAsync(() => component.FindAll(MoveUpSelector)[1].Click());

        // Assert - focus moved to the counterpart that is still enabled on that row
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(field.Instance.MoveDownTargetAt(0).Id);
    }

    [Fact]
    public async Task Moving_An_Item_To_The_Bottom_Should_Focus_Its_Move_Up_Control()
    {
        // Arrange - the mirror case at the other end of the list
        var (component, field) = RenderCollection(3, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act - move the middle item down, landing it last
        await component.InvokeAsync(() => component.FindAll(MoveDownSelector)[1].Click());

        // Assert
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(field.Instance.MoveUpTargetAt(2).Id);
    }

    [Fact]
    public async Task Moving_An_Item_Down_Within_The_Middle_Should_Focus_Its_Move_Down_Control()
    {
        // Arrange - the direction matters. Landing mid-list leaves BOTH controls enabled, so the
        // choice is free - and it has to be the direction the user was already going, or a repeat
        // Enter undoes the move instead of continuing it. Always preferring "up" passes the move-up
        // test above and fails exactly here.
        var (component, field) = RenderCollection(4, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act - move the second item down; it lands at index 2, still mid-list
        await component.InvokeAsync(() => component.FindAll(MoveDownSelector)[1].Click());

        // Assert
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(field.Instance.MoveDownTargetAt(2).Id);
    }

    [Fact]
    public async Task Moving_An_Item_Within_The_Middle_Should_Follow_It_To_Its_New_Row()
    {
        // Arrange - four rows, so the moved item lands somewhere both controls stay enabled. Focus
        // should follow the ITEM to its new row rather than sit on the index the user started at,
        // which would silently now control a different item.
        var (component, field) = RenderCollection(4, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act - move the third item up; it lands at index 1, still mid-list
        await component.InvokeAsync(() => component.FindAll(MoveUpSelector)[2].Click());

        // Assert
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(field.Instance.MoveUpTargetAt(1).Id);
    }

    [Fact]
    public async Task A_Single_Item_Cannot_Be_Moved_So_Nothing_Is_Focused()
    {
        // Arrange - both move controls are Disabled with one row, and the handlers early-return, so
        // there is no state change and nothing to move focus to. Pinned so the "no enabled
        // counterpart" fallback is not mistaken for a reachable path through a move.
        var (component, _) = RenderCollection(1, collection => collection.AllowReorder());
        var focusesBefore = FocusCount();

        // Act & Assert - the controls are disabled, and no focus request is issued
        component.FindAll(MoveUpSelector)[0].HasAttribute("disabled").ShouldBeTrue();
        component.FindAll(MoveDownSelector)[0].HasAttribute("disabled").ShouldBeTrue();
        FocusCount().ShouldBe(focusesBefore);
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

        var fields = component.FindComponents<FluentUICollectionFieldComponent<TwoCollectionModel, MixedItem>>();
        fields.Count.ShouldBe(2);

        var firstFieldIds = new List<string>
        {
            fields[0].Instance.DeleteTargetAt(0)!.Value.Id,
            fields[0].Instance.DeleteTargetAt(1)!.Value.Id,
            fields[0].Instance.DeleteTargetAt(2)!.Value.Id,
        };

        var focusesBefore = FocusCount();

        // Act - remove the middle row of the SECOND collection
        await component.InvokeAsync(() => fields[1].FindAll(DeleteSelector)[1].Click());

        // Assert - one focus request, and it landed in the second field, not the first
        FocusCount().ShouldBe(focusesBefore + 1);
        var focusedId = LastFocusedElementId();
        focusedId.ShouldNotBeOneOf([.. firstFieldIds]);
        focusedId.ShouldBe(fields[1].Instance.DeleteTargetAt(1)!.Value.Id);

        // ...and the first collection was left entirely alone
        fields[0].FindAll(DeleteSelector).Count.ShouldBe(3);
    }

    /// <summary>
    /// Two collections over one model, local to this suite - mirrors
    /// <c>FormCraft.ForMudBlazor.UnitTests.Fields.CollectionFocusTests</c>'s own local shape rather
    /// than <see cref="CollectionItemFixture.TwoCollectionModel"/>, which has no
    /// <c>configureCollection</c> hook to enable <c>AllowRemove()</c>.
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
