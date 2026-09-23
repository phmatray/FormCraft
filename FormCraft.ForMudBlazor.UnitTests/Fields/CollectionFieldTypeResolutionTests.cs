using static FormCraft.TestSupport.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Guards the collection case of the per-configuration field-type cache (#314).
/// <para>
/// Since #203 every row of a collection's item form renders through <c>IFieldRendererService</c>, and
/// every row shares the SAME field configuration instances — <see cref="CollectionValueGetterCachingTests"/>
/// pins this sharing for the value-getter cache (#269, #312), and the type cache this issue adds is
/// keyed by those same shared instances. Unlike a value, a field's resolved TYPE never differs across
/// rows of one collection, so sharing one cache entry across all rows is exactly correct here — the
/// hazard a per-configuration cache could introduce is a stale or misrouted entry, not a cross-row
/// leak the way it would be for a value. This suite proves every row still resolves its own field to
/// the right MudBlazor control, not merely whichever row rendered first.
/// </para>
/// </summary>
public class CollectionFieldTypeResolutionTests : MudBlazorTestBase
{
    [Fact]
    public void Every_Row_Of_A_Multi_Field_Item_Form_Should_Render_The_Right_Control_Per_Field()
    {
        // Arrange - three rows sharing the same four field configuration instances (#203), covering
        // all four of MultiFieldItemForm's field kinds: string, int, bool, DateTime.
        var model = NewMixedItems(new MixedItem(), new MixedItem(), new MixedItem());

        var component = this.RenderItemForm(model, MultiFieldItemForm());

        // Assert - one of each control per row, three rows. The resolved field type (and therefore
        // the renderer chosen for it) is the same for every row: no row's type leaks onto another via
        // the shared cache entry, and no row is left unresolved by a stale one.
        component.FindComponents<MudTextField<string>>().Count(t => t.Instance.Label == "Name").ShouldBe(3);
        component.FindComponents<MudNumericField<int>>().Count.ShouldBe(3);
        component.FindComponents<MudCheckBox<bool>>().Count.ShouldBe(3);
        component.FindComponents<MudDatePicker>().Count.ShouldBe(3);
    }
}
