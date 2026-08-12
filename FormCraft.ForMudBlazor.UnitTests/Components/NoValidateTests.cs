using FormCraft.ForMudBlazor.UnitTests.Fields;
using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Pins the guarantee that a FormCraft form renders <c>novalidate</c> (#206).
/// </summary>
/// <remarks>
/// <para>
/// "Forms render <c>novalidate</c>" is stated as fact in the README, in <c>CLAUDE.md</c> and in code
/// comments, and real decisions rest on it — #190 removed the HTML5 <c>Required</c> attribute from
/// collection item fields on the strength of it, and #193's <c>.WithAttribute("Required", true)</c>
/// opt-in puts a genuine <c>required</c> attribute on an input. If the form is not marked, that
/// attribute is enforced by the browser, and the library produces exactly the native validation
/// bubbles it documents itself as never producing.
/// </para>
/// <para>
/// The guarantee used to be applied after the fact by
/// <c>JSRuntime.InvokeVoidAsync("eval", "document.querySelector('form')?…")</c>, which missed in
/// three ways: it marked the <b>first</b> form in the document rather than this component's, it
/// never ran during prerender (<c>OnAfterRenderAsync</c> does not execute on the server pass), and
/// it failed silently. These tests assert the attribute is in the rendered markup, which is the only
/// form of the claim that holds in all three cases — and which bUnit can see at all, since it runs
/// no JavaScript.
/// </para>
/// </remarks>
public class NoValidateTests : MudBlazorTestBase
{
    [Fact]
    public void FormCraft_Form_Should_Render_NoValidate()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, config));

        // Assert
        component.Find("form").HasAttribute("novalidate").ShouldBeTrue();
    }

    [Fact]
    public void FormCraft_Form_Should_Be_The_One_Marked_When_Another_Form_Precedes_It()
    {
        // Arrange - the headline defect. `document.querySelector('form')` returns the FIRST form in
        // the document, so a page with a search or login form above the FormCraft one marked *that*
        // one and left the FormCraft form validating. Rendering the attribute in the markup makes
        // the form it lands on correct by construction rather than by document order.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act - BeforeForm puts an unrelated <form> ahead of FormCraft's in the same render tree.
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, config)
            .Add(p => p.BeforeForm, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "form");
                builder.AddAttribute(1, "id", "unrelated-search-form");
                builder.CloseElement();
            })));

        // Assert - the unrelated form is untouched, and FormCraft's own is marked.
        var forms = component.FindAll("form");
        forms.Count.ShouldBe(2);

        forms[0].GetAttribute("id").ShouldBe("unrelated-search-form");
        forms[0].HasAttribute("novalidate").ShouldBeFalse();
        forms[1].HasAttribute("novalidate").ShouldBeTrue();
    }

    [Fact]
    public void Two_FormCraft_Forms_Should_Both_Render_NoValidate()
    {
        // Arrange - the second half of the same defect: with the script, two FormCraft forms on one
        // page meant the first was marked twice and the second never.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act - a second FormCraftComponent rendered after the first, in one tree.
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, config)
            .Add(p => p.AfterForm, (RenderFragment)(builder =>
            {
                builder.OpenComponent<FormCraftComponent<TestModel>>(0);
                builder.AddAttribute(1, "Model", new TestModel());
                builder.AddAttribute(2, "Configuration", config);
                builder.CloseComponent();
            })));

        // Assert
        var forms = component.FindAll("form");
        forms.Count.ShouldBe(2);
        forms.ShouldAllBe(f => f.HasAttribute("novalidate"));
    }

    [Fact]
    public void An_Opt_In_Required_Item_Field_Should_Still_Render_Required_Inside_A_NoValidate_Form()
    {
        // Arrange & Act - #193's documented escape hatch must keep working: the attribute is still
        // emitted, it is the *form* that neutralises browser enforcement. Asserting both halves
        // together is the point — this is the exact combination that made #206 worth fixing rather
        // than merely tidying, because on a page where the script missed, this input really was
        // browser-enforced. The blank seed matches the model this test used to declare locally.
        var component = this.RenderItemForm(NewOrder(), TextItemForm(field => field
            .WithAttribute("Required", true)));

        // Assert
        component.Find("form").HasAttribute("novalidate").ShouldBeTrue();
        component.Find("input").HasAttribute("required").ShouldBeTrue();
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
