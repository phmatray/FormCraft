namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// A surviving collection row keeps the component instance that was rendering it, so
/// component-local state stays attached to the data it belongs to (#334).
/// </summary>
/// <remarks>
/// <para>
/// #298 already fixed the two triggers that repair themselves — displayed values (
/// <c>FieldComponentBase.ShouldReloadValue()</c> reloads from the model) and field configuration
/// (one <c>IFieldConfiguration</c> object serves every row, so a row shift never hands a component a
/// different field). What did not repair itself was component <b>identity</b>: the item loop was a
/// plain <c>@for</c> with no <c>@key</c>, so Blazor matched item components by POSITION, and a
/// revealed password (or MudBlazor's own input state, or #283's masked-value diagnostic latch) could
/// end up attached to a different row after a removal or reorder.
/// </para>
/// <para>
/// The fix keys the row on a per-item weak token rather than the item itself — see
/// <c>CollectionFieldComponent.RowKey</c> — precisely because keying on the item (#308) crashes for
/// any <c>record</c>, <c>struct</c>, or <c>Equals</c>-overriding class whose rows compare equal. The
/// existing regression test for that crash,
/// <see cref="FieldConfigurationRefreshTests.Rows_Whose_Items_Compare_Equal_Should_Render_Without_A_Duplicate_Key_Error"/>,
/// stays the guard for the record shape; this file adds the struct shape and the identity assertion
/// itself.
/// </para>
/// </remarks>
public class CollectionRowIdentityTests : MudBlazorTestBase
{
    /// <summary>
    /// Removing a row leaves each surviving row rendered by the same item-field component instance
    /// as before — the identity half of #334, complementing
    /// <see cref="FieldConfigurationRefreshTests.Removing_A_Collection_Row_Should_Leave_The_Others_Showing_Their_Own_Values"/>,
    /// which already pins that the surviving VALUES are correct.
    /// </summary>
    [Fact]
    public void Removing_A_Row_Should_Leave_Surviving_Rows_On_Their_Own_Component_Instance()
    {
        // Arrange - three rows, each a distinct OrderItem instance.
        var model = new OrderModel
        {
            Items =
            [
                new OrderItem { ProductName = "first" },
                new OrderItem { ProductName = "second" },
                new OrderItem { ProductName = "third" },
            ],
        };
        var secondItem = model.Items[1];

        var component = this.RenderItemForm(model, CollectionItemFixture.TextItemForm());

        var before = component.FindComponents<MudBlazorTextFieldComponent<OrderItem>>();
        var secondInstanceBefore = before.Single(c => ReferenceEquals(c.Instance.Context.Model, secondItem)).Instance;

        // Act - drop the FIRST row, on the model rather than through the delete button, exactly as
        // the companion value test does: `Items` IS the model's own list.
        model.Items.RemoveAt(0);
        component.Render();

        // Assert - "second" is still rendered by the SAME component instance, not one that inherited
        // the slot "first" used to occupy.
        var after = component.FindComponents<MudBlazorTextFieldComponent<OrderItem>>();
        var secondInstanceAfter = after.Single(c => ReferenceEquals(c.Instance.Context.Model, secondItem)).Instance;
        secondInstanceAfter.ShouldBeSameAs(secondInstanceBefore);
    }

    /// <summary>
    /// A component follows its item when rows are reordered, not just when one is removed.
    /// </summary>
    [Fact]
    public void Moving_A_Row_Should_Carry_Its_Component_Instance_To_The_New_Position()
    {
        // Arrange
        var model = new OrderModel
        {
            Items =
            [
                new OrderItem { ProductName = "first" },
                new OrderItem { ProductName = "second" },
            ],
        };
        var firstItem = model.Items[0];

        var component = this.RenderItemForm(model, CollectionItemFixture.TextItemForm());

        var before = component.FindComponents<MudBlazorTextFieldComponent<OrderItem>>();
        var firstInstanceBefore = before.Single(c => ReferenceEquals(c.Instance.Context.Model, firstItem)).Instance;

        // Act - swap the two rows, the same mutation MoveItemDown(0) performs.
        (model.Items[0], model.Items[1]) = (model.Items[1], model.Items[0]);
        component.Render();

        // Assert - "first" is now the SECOND row, still on its own component instance.
        var after = component.FindComponents<MudBlazorTextFieldComponent<OrderItem>>();
        after.Select(c => c.Instance.Context.Model.ProductName).ShouldBe(["second", "first"]);
        var firstInstanceAfter = after.Single(c => ReferenceEquals(c.Instance.Context.Model, firstItem)).Instance;
        firstInstanceAfter.ShouldBeSameAs(firstInstanceBefore);
    }

    /// <summary>
    /// The same item object appearing twice in the collection — legal for a reference type — renders
    /// without a duplicate-key crash. A weak token is minted per item INSTANCE, so two rows sharing
    /// the SAME instance would otherwise collide into one token; <c>RowKey</c> detects that and falls
    /// back to the unkeyed (index) path for both occurrences.
    /// </summary>
    [Fact]
    public void The_Same_Item_Instance_Appearing_Twice_Should_Render_Without_A_Duplicate_Key_Error()
    {
        // Arrange - one OrderItem, added to the list twice.
        var sharedItem = new OrderItem { ProductName = "shared" };
        var model = new OrderModel { Items = [sharedItem, sharedItem] };

        // Act & Assert - rendering, and then growing further, must not throw.
        var component = this.RenderItemForm(model, CollectionItemFixture.TextItemForm());
        component.FindAll("input").Count.ShouldBe(2);

        model.Items.Add(new OrderItem { ProductName = "third" });
        Should.NotThrow(() => component.Render());
        component.FindAll("input").Count.ShouldBe(3);
    }

    /// <summary>
    /// A struct-typed item renders and grows without throwing — the other shape #308's reverted
    /// <c>@key="Items[index]"</c> could not survive. <b>The <c>struct</c> is the point</b>: it
    /// boxes fresh on every access, so it has no reference-stable identity to hand a weak table, and
    /// <c>RowKey</c> falls back to the boxed index for it (today's positional behaviour, unchanged).
    /// </summary>
    /// <remarks>
    /// ⛔ Do not "share this with the fixture". Every <c>CollectionItemFixture</c> model is a class;
    /// swapping one in would stop this test from exercising a value-typed <c>TItem</c> at all.
    /// Allowlisted in <c>CollectionItemShapeGuardTests</c> for the same reason
    /// <c>FieldConfigurationRefreshTests.EqualityItemModel</c> is: the shape collides with
    /// <c>OrderItem</c>'s, and the collision is exactly what a shared model would erase.
    /// </remarks>
    [Fact]
    public void A_Struct_Typed_Item_Should_Render_And_Grow_Without_Throwing()
    {
        // Arrange - two rows, the shape a keyed loop must not reject for a value type.
        var model = new StructItemModel
        {
            Items = [new StructItem { ProductName = "a" }, new StructItem { ProductName = "b" }],
        };

        var config = FormBuilder<StructItemModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();

        // Act & Assert
        var component = this.RenderItemForm(model, config);
        component.FindAll("input").Count.ShouldBe(2);

        model.Items.Add(new StructItem { ProductName = "c" });
        Should.NotThrow(() => component.Render());
        component.FindAll("input").Count.ShouldBe(3);
    }

    /// <summary>
    /// A value-typed item. <b>The <c>struct</c> is the point</b> — see
    /// <see cref="A_Struct_Typed_Item_Should_Render_And_Grow_Without_Throwing"/>.
    /// </summary>
    internal struct StructItem
    {
        public string ProductName { get; set; }
    }

    /// <summary>
    /// The collection root for <see cref="StructItem"/>. Allowlisted in
    /// <c>CollectionItemShapeGuardTests</c> — see the reason recorded there.
    /// </summary>
    internal sealed class StructItemModel
    {
        public List<StructItem> Items { get; set; } = [];
    }
}
