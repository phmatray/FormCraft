using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;
using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields honour the text-input attributes the component render path
/// forwards (#189). Written when item fields rendered through CollectionFieldComponent's imperative
/// RenderTreeBuilder path, which resolved these in its own <c>AddTextInputAttributes</c> rather than
/// through <c>MudBlazorTextFieldComponent</c> — so it needed coverage of its own. #203 deleted that
/// path; these now exercise the same component a standalone field uses, and pass unmodified.
/// <para>
/// Before #189 the text path never emitted <c>InputType</c>, so a <c>.AsPassword()</c> item field
/// rendered its characters in clear text on screen while the identical field outside a collection
/// masked them. That is the case these tests exist to pin.
/// </para>
/// <para>
/// The unset-value assertions below deliberately assert what the COMPONENT path renders rather than
/// MudBlazor's own component default. The two differ for <c>MaxLength</c> (524288 vs the component
/// path's <c>int.MaxValue</c>), and parity between FormCraft's two paths is the property this issue
/// is about.
/// </para>
/// </summary>
public class CollectionInputTypeTests : MudBlazorTestBase
{
    private readonly CapturingLoggerProvider _logs = new();

    public CollectionInputTypeTests()
    {
        Services.AddLogging(builder => builder.AddProvider(_logs));
    }

    [Fact]
    public void ItemField_With_AsPassword_Should_Render_A_Password_Input()
    {
        // Arrange & Act - the bug this issue was filed for: the characters were displayed.
        var component = RenderOrderForm(TextItemForm(field => field.AsPassword()));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType
            .ShouldBe(InputType.Password);
    }

    [Fact]
    public void ItemField_With_AsPassword_Should_Mask_Regardless_Of_The_Visibility_Toggle()
    {
        // Arrange & Act - AsPassword() also writes EnablePasswordToggle, which the collection path
        // does not implement. Masking must not depend on that: the toggle is a convenience, the
        // masking is the security-relevant half.
        var component = RenderOrderForm(TextItemForm(field =>
            field.AsPassword(enableVisibilityToggle: false)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType
            .ShouldBe(InputType.Password);
    }

    [Fact]
    public void ItemField_With_AsPassword_Should_Emit_A_Password_Input_In_The_Markup()
    {
        // Arrange & Act - the parameter assertions above prove the value was forwarded; this proves
        // it reaches the DOM the user actually looks at. That is the whole claim of this issue, and
        // a component parameter that never made it onto the <input> would satisfy the others.
        var component = RenderOrderForm(TextItemForm(field => field.AsPassword()));

        // Assert - `type="password"` is the whole of the fix: it is what makes the browser mask the
        // characters. Deliberately NOT asserting the value is absent from the markup — a bound
        // input renders its `value` attribute whatever its type, and a standalone password field
        // does exactly the same. That would be a claim about the DOM that is false of correctly
        // masked password fields everywhere, not evidence of this bug.
        component.Find("input").GetAttribute("type").ShouldBe("password");
    }

    [Fact]
    public void ItemField_Without_An_Input_Type_Should_Render_Text()
    {
        // Arrange & Act - unchanged from before #189; guards the forward against regressing the
        // ordinary case into some other input type.
        var component = RenderOrderForm(TextItemForm());

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType
            .ShouldBe(InputType.Text);
    }

    [Theory]
    [InlineData("email", InputType.Email)]
    [InlineData("tel", InputType.Telephone)]
    [InlineData("url", InputType.Url)]
    [InlineData("search", InputType.Search)]
    [InlineData("password", InputType.Password)]
    public void ItemField_Should_Map_Its_Input_Type_The_Same_Way_The_Component_Path_Does(
        string configured,
        InputType expected)
    {
        // Arrange & Act - MudBlazorTextFieldComponent.GetInputType() owns this mapping; the two
        // paths must agree on all of it, not just on "password".
        var component = RenderOrderForm(TextItemForm(field => field.WithInputType(configured)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType.ShouldBe(expected);
    }

    [Fact]
    public void ItemField_Should_Map_An_Unrecognised_Input_Type_To_Text()
    {
        // Arrange & Act - the component path's fallback arm, mirrored.
        var component = RenderOrderForm(TextItemForm(field =>
            field.WithInputType("definitely-not-an-input-type")));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType
            .ShouldBe(InputType.Text);
    }

    [Fact]
    public void ItemField_Should_Read_An_Input_Type_Set_Through_A_Raw_Attribute()
    {
        // Arrange & Act - WithInputType writes the first-class Field.InputType property, but the
        // component path also accepts a raw "InputType" attribute as a fallback. Resolve both, or
        // a field configured the second way keeps rendering in clear text.
        var component = RenderOrderForm(TextItemForm(field =>
            field.WithAttribute("InputType", "password")));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType
            .ShouldBe(InputType.Password);
    }

    [Fact]
    public void ItemField_Should_Render_Its_Configured_Lines()
    {
        // Arrange & Act
        var component = RenderOrderForm(TextItemForm(field => field.AsTextArea(lines: 4)));

        // Assert - the parameter, and the consequence of it. MudTextField switches element on
        // Lines > 1, so this is the one forwarded attribute whose effect is visible in the markup
        // as a different tag rather than a different attribute value.
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(4);
        component.FindAll("textarea").Count.ShouldBe(1);
    }

    [Fact]
    public void ItemField_Without_Lines_Should_Render_A_Single_Line()
    {
        // Arrange & Act - measured, not assumed: MudTextField's own default is 1, and the component
        // path's fallback is also 1, so the two agree and forwarding changes nothing when unset.
        var component = RenderOrderForm(TextItemForm());

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(1);
    }

    [Fact]
    public void ItemField_Should_Render_Its_Configured_MaxLength()
    {
        // Arrange & Act
        var component = RenderOrderForm(TextItemForm(field =>
            field.AsTextArea(lines: 3, maxLength: 500)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.MaxLength.ShouldBe(500);
    }

    [Fact]
    public void ItemField_Without_MaxLength_Should_Render_Unbounded_Like_A_Standalone_Field()
    {
        // Arrange & Act - this is the one place the "use MudBlazor's own default" rule does NOT
        // apply. MudTextField defaults MaxLength to 524288, but the component path deliberately
        // renders int.MaxValue when nothing is configured. Parity is with the component path, so
        // that is the value to match — copying MudBlazor's bare default here would make the two
        // paths disagree by exactly the amount this issue exists to remove.
        var component = RenderOrderForm(TextItemForm());

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.MaxLength.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void ItemField_With_A_Non_Positive_MaxLength_Should_Render_Unbounded()
    {
        // Arrange & Act - the component path treats a zero/negative MaxLength as "no limit"; the
        // item path must not turn it into a field that accepts nothing.
        var component = RenderOrderForm(TextItemForm(field =>
            field.WithAttribute("MaxLength", 0)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.MaxLength.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void ItemField_Should_Render_Its_Configured_Autocomplete()
    {
        // Arrange & Act - MudTextField has no Autocomplete parameter at all (verified by
        // reflection), so the component path emits a raw lowercase "autocomplete" HTML attribute
        // that lands in the unmatched-attribute bag. The item path has to do the same.
        var component = RenderOrderForm(TextItemForm(field =>
            field.WithAutocomplete("current-password")));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance
            .UserAttributes["autocomplete"].ShouldBe("current-password");
    }

    [Fact]
    public void ItemField_Autocomplete_Should_Reach_The_Rendered_Input()
    {
        // Arrange & Act - guard the guard: UserAttributes is only a staging dictionary. Assert the
        // attribute actually survives onto the <input>, which is what a password manager reads.
        var component = RenderOrderForm(TextItemForm(field =>
            field.WithAutocomplete("current-password")));

        // Assert
        component.Find("input").GetAttribute("autocomplete").ShouldBe("current-password");
    }

    [Fact]
    public void ItemField_With_A_Mask_Should_Bind_The_Configured_Pattern_Mask()
    {
        // Arrange & Act - #211. This test used to pin the opposite: masks were forwarded by neither
        // path, so it asserted null and said "pinned so that whoever implements masks does it on both
        // paths at once". That is now done — FormCraft's mask string is resolved to a MudBlazor IMask
        // through the shared TextMaskMap, so the item path and the standalone path agree.
        var component = RenderOrderForm(TextItemForm(field =>
            field.WithAttribute("Mask", "0000-0000")));

        // Assert - the pattern rather than the instance: each path builds its own IMask, so the
        // objects are never equal and only the configured pattern is comparable.
        var mask = component.FindComponent<MudTextField<string>>().Instance.Mask;
        mask.ShouldBeOfType<PatternMask>();
        mask.Mask.ShouldBe("0000-0000");
    }

    [Fact]
    public void ItemField_Without_A_Mask_Should_Bind_No_Mask()
    {
        // Arrange & Act - the guard on the guard, and the reason TextMaskMap.Resolve returns null for
        // an empty pattern rather than PatternMask(""). MudTextField swaps its input implementation
        // for a MudMask as soon as Mask is non-null, so a non-null empty mask would reroute every
        // unmasked item field through a different component and quietly drop MaxLines with it.
        var component = RenderOrderForm(TextItemForm());

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask.ShouldBeNull();
    }

    [Theory]
    [InlineData("number", InputType.Number)]
    [InlineData("date", InputType.Date)]
    [InlineData("time", InputType.Time)]
    public void ItemField_Should_Map_The_Numeric_And_Temporal_Input_Types(
        string configured,
        InputType expected)
    {
        // Arrange & Act - #210. These three fell through to InputType.Text, silently costing the
        // mobile keypad and the native picker. #189 carried the recognised set over unchanged on
        // purpose, so widening it is a behaviour change rather than a parity fix — but because both
        // paths resolve through TextInputTypeMap, it lands on both at once.
        var component = RenderOrderForm(TextItemForm(field => field.WithInputType(configured)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType.ShouldBe(expected);
    }

    [Theory]
    [InlineData("number")]
    [InlineData("date")]
    [InlineData("time")]
    public void ItemField_Should_Emit_The_Numeric_And_Temporal_Types_In_The_Markup(string configured)
    {
        // Arrange & Act - the parameter assertion above proves the value was forwarded; this proves
        // it reaches the element the browser reads, which is what actually selects the keypad or the
        // picker. A forwarded parameter that never lands on the <input> is the failure mode both
        // #189 and #207 hit.
        var component = RenderOrderForm(TextItemForm(field => field.WithInputType(configured)));

        // Assert
        component.Find("input").GetAttribute("type").ShouldBe(configured);
    }

    [Fact]
    public void ItemField_With_AsPassword_And_Lines_Should_Stay_Masked()
    {
        // Arrange & Act - #207. Past Lines > 1 MudBlazor emits a <textarea>, and a textarea has no
        // `type` attribute at all, so the masking was silently dropped and the credential was
        // displayed. There is no such thing as a masked textarea, so the security-relevant half of
        // the configuration wins and the field renders as a single-line password input.
        var component = RenderOrderForm(TextItemForm(field =>
            field.AsPassword().AsTextArea(lines: 4)));

        // Assert - the markup, not just the parameter: asserting only MudTextField.InputType would
        // pass while MudBlazor still rendered a <textarea>, which is exactly how this bug hid.
        component.FindAll("textarea").ShouldBeEmpty();
        component.Find("input").GetAttribute("type").ShouldBe("password");
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(1);
    }

    [Fact]
    public void ItemField_With_Lines_Then_AsPassword_Should_Stay_Masked()
    {
        // Arrange & Act - order-independence is not a nicety here: AsTextArea lives in the core
        // FormCraft project and AsPassword in FormCraft.ForMudBlazor, so neither builder method
        // ever sees the other's setting. Only the render path can reconcile them.
        var component = RenderOrderForm(TextItemForm(field =>
            field.AsTextArea(lines: 4).AsPassword()));

        // Assert
        component.FindAll("textarea").ShouldBeEmpty();
        component.Find("input").GetAttribute("type").ShouldBe("password");
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(1);
    }

    [Fact]
    public void ItemField_With_A_Mask_That_Blanks_Values_Should_Warn_Once_For_The_Field()
    {
        // Arrange - a collection renders one component instance PER ROW, and the diagnostic fires
        // from OnInitialized, so an unlatched warning reports a single field's CONFIGURATION once
        // per row: fifty rows, fifty identical lines, and the signal is buried in its own noise.
        // The latch is what makes this a report about a field rather than about a list.
        var config = TextItemForm(field => field
            .WithLabel("Secret")
            .WithAttribute("Mask", "(000) 000-0000"));

        // Act
        RenderRows(config, rows: 5);

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Secret");
        warnings[0].ShouldContain("(000) 000-0000");
    }

    [Fact]
    public void ItemField_Tripping_Two_Diagnostics_Should_Report_Both()
    {
        // Arrange - the reason the latch key carries the diagnostic CATEGORY and is not just the
        // field name. One field can legitimately trip several diagnostics at once, and a latch
        // shared between them would report whichever fired first and hide the rest for good. The
        // code this replaced kept two separate HashSets for exactly this; the category-qualified key
        // is how that survives now that the latch is shared infrastructure.
        var config = TextItemForm(field => field
            .WithAttribute("Mask", "(000) 000-0000")
            .AsPassword()
            .AsTextArea(lines: 4));

        // Act
        RenderRows(config, rows: 5);

        // Assert - one masked-lines warning and one masked-value warning, each still latched to one
        // per field rather than one per row.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(2);
        warnings.ShouldContain(w => w.Contains("masked"));
        warnings.ShouldContain(w => w.Contains("(000) 000-0000"));
    }

    [Fact]
    public void ItemField_With_A_Mask_Should_Not_Warn_When_Every_Row_Conforms()
    {
        // Arrange - the negative that keeps the latch honest. Nothing is rejected here, so the
        // absence of a warning must come from the rule, not from a latch that swallowed it.
        var config = TextItemForm(field => field.WithAttribute("Mask", "(000) 000-0000"));

        // Act
        RenderRows(config, rows: 5, value: "5551234567");

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ItemFields_Of_The_Same_Name_In_Two_Collections_Should_Be_Named_Apart()
    {
        // Arrange - the latch already counts these as the two separate fields they are, so both are
        // reported. If both messages then say "Secret", the developer is told twice that something
        // is wrong and never which collection to audit -- which is the ambiguity
        // CollectionItemFieldScope.DiagnosticKey exists to remove, and it has to reach the MESSAGE
        // and not just the latch key to be worth anything.
        var config = TwoCollectionItemForm(
            contact => contact
                .WithLabel("Secret")
                .WithAttribute("Mask", "(000) 000-0000"),
            supplier => supplier
                .WithLabel("Secret")
                .WithAttribute("Mask", "(000) 000-0000"));

        // Act
        this.RenderItemForm(NewTwoCollections("N/A", "N/A"), config);

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(2);
        warnings.ShouldContain(w => w.Contains("Contacts[].Secret"));
        warnings.ShouldContain(w => w.Contains("Suppliers[].Secret"));
    }

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderRows(
        IFormConfiguration<OrderModel> config,
        int rows,
        string value = "N/A")
    {
        return this.RenderItemForm(
            NewOrderWithItems(Enumerable.Repeat(value, rows).ToArray()),
            config);
    }

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderOrderForm(
        IFormConfiguration<OrderModel> config)
        => RenderRows(config, rows: 1, value: "hunter2");
}
