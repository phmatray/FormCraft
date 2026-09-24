using FormCraft.ForFluentUI.UnitTests.Components;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// A <c>.Required(...)</c> field must be identified to assistive technology - WCAG 2.1
/// <b>3.3.2 Labels or Instructions</b> (Level A). This is the Fluent-side counterpart of #199.
/// </summary>
/// <remarks>
/// <para>
/// The attribute is written by <c>FluentUIFieldComponentBase.AriaRequired</c> rather than left to
/// Fluent's own <c>Required</c> parameter. Fluent UI v5's published XML documentation covers
/// <c>aria-label</c>, <c>aria-live</c> and <c>aria-level</c> and never mentions
/// <c>aria-required</c>, so the attribute this library's accessibility guarantee rests on is not one
/// the component library promises. These tests pin our own guarantee, which is the point: they fail
/// if a future Fluent release stops emitting it, instead of the guarantee quietly evaporating.
/// </para>
/// <para>
/// Unlike the MudBlazor adapter, an optional field renders <b>no</b> <c>aria-required</c> attribute
/// at all rather than <c>aria-required="false"</c>. Both are correct - absence and <c>"false"</c>
/// mean the same thing to a screen reader - and this adapter omits it because nothing here forces
/// the attribute to be present the way <c>MudInput</c> does.
/// </para>
/// </remarks>
public class AriaRequiredTests : FluentUITestBase
{
    private IRenderedComponent<FormCraftComponent<TestModel>> RenderField(
        Action<FieldBuilder<TestModel, string>> configure)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, configure)
            .Build();

        return Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));
    }

    [Fact]
    public void Required_Field_Should_Announce_Itself_To_Assistive_Technology()
    {
        // Arrange & Act - the plain .Required(...) call
        var component = RenderField(f => f.WithLabel("Name").Required("Name is required"));

        // Assert - the attribute a screen reader actually reads
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void Optional_Field_Should_Not_Announce_Itself_As_Required()
    {
        // Arrange & Act
        var component = RenderField(f => f.WithLabel("Name"));

        // Assert
        component.FindAll("[aria-required='true']").ShouldBeEmpty();
    }

    [Fact]
    public void Explicit_Required_Attribute_False_Should_Suppress_The_Announcement()
    {
        // Arrange & Act - the raw form of the MudBlazor adapter's .WithNativeRequired(false)
        var component = RenderField(f => f
            .WithLabel("Name")
            .Required("Name is required")
            .WithAttribute("Required", false));

        // Assert - the explicit opt-out wins over the validator's answer
        component.FindAll("[aria-required='true']").ShouldBeEmpty();
    }

    [Fact]
    public void Explicit_Required_Attribute_True_Should_Announce_Without_A_Validator()
    {
        // Arrange & Act - the opt-in wins in the other direction too: decoration without .Required()
        var component = RenderField(f => f
            .WithLabel("Name")
            .WithAttribute("Required", true));

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void FieldHelpText_IdFor_Should_Sanitize_A_Nested_Fields_Dotted_Path()
    {
        // Arrange & Act - a nested binding's FieldName is a dotted path ("Billing.Amount",
        // #437/#448). A '.' is a valid HTML id character but the CSS class-selector delimiter, so an
        // unescaped one breaks a consumer's own #id.Class-shaped stylesheet selector (#449).
        var id = FieldHelpText.IdFor("Billing.Amount");

        // Assert
        id.ShouldBe("formcraft-help-Billing-Amount");
        id.ShouldNotContain(".");
    }

    [Fact]
    public void FieldHelpText_IdFor_Should_Leave_A_Single_Member_Field_Unchanged()
    {
        // Arrange & Act - the common case has no '.' to sanitize, so nothing should change (#449)
        var id = FieldHelpText.IdFor("ProductName");

        // Assert
        id.ShouldBe("formcraft-help-ProductName");
    }
}
