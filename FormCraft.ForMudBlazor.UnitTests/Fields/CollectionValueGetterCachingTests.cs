using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Guards the collection case of the value-getter cache (#269).
/// <para>
/// Since #203 every row of an item form renders through <c>IFieldRendererService</c>, and every row
/// of one collection shares a single field configuration instance — the cache is keyed by that
/// instance, so all rows share one cache entry. That is safe only because what is cached is the
/// <b>getter</b>, which takes the row's item as its parameter, and never the value it returned for
/// some other row. Cache the value instead, or key the cache by anything coarser than the
/// configuration, and every row in the collection would display row 0's content.
/// </para>
/// <para>
/// This is also where the optimization pays off: <c>UpdateItemFieldValue</c> notifies the parent
/// while text fields render <c>Immediate="true"</c>, so a keystroke in any row re-renders the whole
/// collection — rows × fields compiles per character before this change.
/// </para>
/// </summary>
public class CollectionValueGetterCachingTests : MudBlazorTestBase
{
    [Fact]
    public void Typing_Into_One_Row_Should_Land_On_That_Row_Only()
    {
        // Arrange - three rows sharing one item field configuration
        var model = NewOrderWithItems("first", "second", "third");
        var component = this.RenderItemForm(model, TextItemForm());

        // Act - type into the middle row
        component.FindAll("input")[1].Input("edited");

        // Assert - the edit lands on row 1, and its siblings are untouched
        model.Items[0].ProductName.ShouldBe("first");
        model.Items[1].ProductName.ShouldBe("edited");
        model.Items[2].ProductName.ShouldBe("third");
    }

    [Fact]
    public void Every_Row_Should_Render_Its_Own_Value_After_An_Edit_Rerenders_The_Collection()
    {
        // Arrange
        var model = NewOrderWithItems("first", "second", "third");
        var component = this.RenderItemForm(model, TextItemForm());

        // Act - editing one row re-renders every row through the shared cached getter
        component.FindAll("input")[1].Input("edited");

        // Assert - each row still reads its own item rather than a value cached from another
        component.WaitForAssertion(() =>
        {
            var inputs = component.FindAll("input");
            inputs[0].GetAttribute("value").ShouldBe("first");
            inputs[1].GetAttribute("value").ShouldBe("edited");
            inputs[2].GetAttribute("value").ShouldBe("third");
        });
    }
}
