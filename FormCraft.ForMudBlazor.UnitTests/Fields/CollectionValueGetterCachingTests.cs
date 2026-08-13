using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Guards the collection case of the value-getter cache, on both paths that use it (#269, #312).
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
/// collection — rows × fields compiles per character on the render path before this change.
/// </para>
/// <para>
/// The three tests below cover different things on purpose: the DOM one guards the cache on the
/// <b>render</b> path, the validation one guards it on the <b>validation</b> path (both share one
/// cache since #312), and the model one covers the #203 write path, which does not go through the
/// cached getter at all.
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

        // Assert - the edit lands on row 1 and its siblings are untouched. This is the #203 write
        // path (CollectionFieldComponent resolves the row by index and the member by reflection),
        // which the value-getter cache is not on — it is asserted here so the read-side guard below
        // is read against a known-good write, not so it guards the cache itself.
        component.WaitForAssertion(() =>
        {
            model.Items[0].ProductName.ShouldBe("first");
            model.Items[1].ProductName.ShouldBe("edited");
            model.Items[2].ProductName.ShouldBe("third");
        });
    }

    [Fact]
    public void Every_Row_Should_Render_Its_Own_Value_After_An_Edit_Rerenders_The_Collection()
    {
        // Arrange
        var model = NewOrderWithItems("first", "second", "third");
        var component = this.RenderItemForm(model, TextItemForm());

        // Act - editing one row re-renders every row through the shared cached getter
        component.FindAll("input")[1].Input("edited");

        // Assert - each row still reads its own item rather than a value cached from another. This
        // is the guard that fails if the cache ever holds a value instead of a getter, or is keyed
        // by anything the rows share: every row would then show row 0's content.
        component.WaitForAssertion(() =>
        {
            var inputs = component.FindAll("input");
            inputs[0].GetAttribute("value").ShouldBe("first");
            inputs[1].GetAttribute("value").ShouldBe("edited");
            inputs[2].GetAttribute("value").ShouldBe("third");
        });
    }

    [Fact]
    public async Task Validating_A_Multi_Row_Item_Form_Should_Attribute_Each_Message_To_Its_Own_Row()
    {
        // Arrange - rows 0 and 2 are empty (invalid), row 1 is filled (valid). Since #312 the
        // validators read every row's value through one cached getter shared by the whole
        // collection, so this is where a getter that had been cached per *value* rather than per
        // *field* would show: every row would be judged against row 0's content, marking the filled
        // row invalid (or the empty rows valid).
        var model = NewOrderWithItems("", "Widget", "");
        EditContext? editContext = null;

        var component = this.RenderItemForm(
            model,
            TextItemForm(field => field.Required("Product name is required")),
            parameters => parameters.Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        // Act
        var isValid = true;
        await component.InvokeAsync(async () => isValid = await component.Instance.ValidateAsync());

        // Assert - the messages land on the rows that are actually empty, and only those (#91).
        isValid.ShouldBeFalse();
        editContext.ShouldNotBeNull();

        editContext!.GetValidationMessages(new FieldIdentifier(model, "Items[0].ProductName"))
            .ShouldContain("Product name is required");
        editContext.GetValidationMessages(new FieldIdentifier(model, "Items[1].ProductName"))
            .ShouldBeEmpty();
        editContext.GetValidationMessages(new FieldIdentifier(model, "Items[2].ProductName"))
            .ShouldContain("Product name is required");
    }
}
