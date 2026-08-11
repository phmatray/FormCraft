namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields honour the text-input attributes the component render path
/// forwards (#189). These render through CollectionFieldComponent's imperative RenderTreeBuilder
/// path, which resolves presentation attributes in AddCommonFieldAttributes / RenderTextField
/// rather than through MudBlazorFieldComponentBase — so it needs its own coverage.
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

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Lines.ShouldBe(4);
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
    public void ItemField_With_A_Mask_Should_Render_No_Mask_Exactly_Like_A_Standalone_Field()
    {
        // Arrange & Act - deliberately NOT forwarded. FormCraft stores "Mask" as a string, while
        // MudBlazor's Mask parameter takes an IMask; the component path reads the string into a
        // property and then drops it (its GetMask() is an unimplemented stub that always returns
        // null and is never called). So neither path supports masks today. Forwarding the string
        // here would make the item path differ from the standalone one — the opposite of this
        // issue's goal. Pinned so that whoever implements masks does it on both paths at once.
        var component = RenderOrderForm(BuildConfiguration(field =>
            field.WithAttribute("Mask", "0000-0000")));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask.ShouldBeNull();
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
