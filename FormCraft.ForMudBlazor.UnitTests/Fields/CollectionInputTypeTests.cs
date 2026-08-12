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
    [Fact]
    public void ItemField_With_AsPassword_Should_Render_A_Password_Input()
    {
        // Arrange & Act - the bug this issue was filed for: the characters were displayed.
        var component = RenderOrderForm(BuildConfiguration(field => field.AsPassword()));

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
        var component = RenderOrderForm(BuildConfiguration(field =>
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
        var component = RenderOrderForm(BuildConfiguration(field => field.AsPassword()));

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
        var component = RenderOrderForm(BuildConfiguration(_ => { }));

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
        var component = RenderOrderForm(BuildConfiguration(field => field.WithInputType(configured)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType.ShouldBe(expected);
    }

    [Fact]
    public void ItemField_Should_Map_An_Unrecognised_Input_Type_To_Text()
    {
        // Arrange & Act - the component path's fallback arm, mirrored.
        var component = RenderOrderForm(BuildConfiguration(field =>
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
        var component = RenderOrderForm(BuildConfiguration(field =>
            field.WithAttribute("InputType", "password")));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.InputType
            .ShouldBe(InputType.Password);
    }

    [Fact]
    public void ItemField_Should_Render_Its_Configured_Lines()
    {
        // Arrange & Act
        var component = RenderOrderForm(BuildConfiguration(field => field.AsTextArea(lines: 4)));

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
        var component = RenderOrderForm(BuildConfiguration(_ => { }));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(1);
    }

    [Fact]
    public void ItemField_Should_Render_Its_Configured_MaxLength()
    {
        // Arrange & Act
        var component = RenderOrderForm(BuildConfiguration(field =>
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
        var component = RenderOrderForm(BuildConfiguration(_ => { }));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.MaxLength.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void ItemField_With_A_Non_Positive_MaxLength_Should_Render_Unbounded()
    {
        // Arrange & Act - the component path treats a zero/negative MaxLength as "no limit"; the
        // item path must not turn it into a field that accepts nothing.
        var component = RenderOrderForm(BuildConfiguration(field =>
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
        var component = RenderOrderForm(BuildConfiguration(field =>
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
        var component = RenderOrderForm(BuildConfiguration(field =>
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
        var component = RenderOrderForm(BuildConfiguration(field =>
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
        var component = RenderOrderForm(BuildConfiguration(field => field.WithLabel("Product")));

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
        var component = RenderOrderForm(BuildConfiguration(field => field.WithInputType(configured)));

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
        var component = RenderOrderForm(BuildConfiguration(field => field.WithInputType(configured)));

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
        var component = RenderOrderForm(BuildConfiguration(field =>
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
        var component = RenderOrderForm(BuildConfiguration(field =>
            field.AsTextArea(lines: 4).AsPassword()));

        // Assert
        component.FindAll("textarea").ShouldBeEmpty();
        component.Find("input").GetAttribute("type").ShouldBe("password");
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(1);
    }

    private IRenderedComponent<FormCraftComponent<CredentialsModel>> RenderOrderForm(
        IFormConfiguration<CredentialsModel> config)
    {
        var model = new CredentialsModel { Items = { new Credential { Secret = "hunter2" } } };

        return Render<FormCraftComponent<CredentialsModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));
    }

    private static IFormConfiguration<CredentialsModel> BuildConfiguration(
        Action<FieldBuilder<Credential, string>> configureItemField)
    {
        return FormBuilder<CredentialsModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Credentials")
                .WithItemForm(item => item
                    .AddField(x => x.Secret, field =>
                    {
                        field.WithLabel("Secret");
                        configureItemField(field);
                    })))
            .Build();
    }

    private class CredentialsModel
    {
        public List<Credential> Items { get; set; } = new();
    }

    private class Credential
    {
        public string Secret { get; set; } = string.Empty;
    }
}
