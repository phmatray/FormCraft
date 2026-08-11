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
