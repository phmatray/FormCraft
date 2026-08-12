using FormCraft.ForFluentUI.UnitTests.Components;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// Covers what a <see cref="string"/> field renders as, and that edits reach the model.
/// </summary>
public class FluentUITextFieldComponentTests : FluentUITestBase
{
    private IRenderedComponent<FormCraftComponent<TestModel>> RenderField(
        TestModel model,
        Action<FieldBuilder<TestModel, string>> configure)
    {
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, configure)
            .Build();

        return Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));
    }

    [Fact]
    public void String_Field_Should_Render_A_Text_Input()
    {
        // Arrange & Act
        var component = RenderField(new TestModel(), f => f.WithLabel("Name"));

        // Assert
        component.FindComponents<FluentTextInput>().ShouldNotBeEmpty();
    }

    [Fact]
    public void Text_Field_Should_Render_Its_Label_And_Placeholder()
    {
        // Arrange & Act
        var component = RenderField(new TestModel(), f => f
            .WithLabel("Full name")
            .WithPlaceholder("Jane Doe"));

        // Assert
        var input = component.FindComponent<FluentTextInput>();
        input.Instance.Label.ShouldBe("Full name");
        input.Instance.Placeholder.ShouldBe("Jane Doe");
    }

    [Fact]
    public void Text_Field_Should_Load_The_Existing_Model_Value()
    {
        // Arrange & Act
        var component = RenderField(new TestModel { Name = "Ada" }, f => f.WithLabel("Name"));

        // Assert
        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("Ada");
    }

    [Fact]
    public async Task Editing_The_Field_Should_Write_Back_To_The_Model()
    {
        // Arrange
        var model = new TestModel();
        var component = RenderField(model, f => f.WithLabel("Name"));
        var input = component.FindComponent<FluentTextInput>();

        // Act - drive the component's own ValueChanged, which is what the DOM event ends up calling
        await component.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync("Grace"));

        // Assert
        model.Name.ShouldBe("Grace");
    }

    [Fact]
    public void Multiline_Field_Should_Render_A_Text_Area()
    {
        // Arrange & Act
        var component = RenderField(new TestModel(), f => f
            .WithLabel("Notes")
            .WithAttribute("Lines", 4));

        // Assert
        component.FindComponents<FluentTextArea>().ShouldNotBeEmpty();
        component.FindComponents<FluentTextInput>().ShouldBeEmpty();
    }

    [Fact]
    public void Password_Field_Should_Stay_Single_Line_So_It_Can_Mask()
    {
        // Arrange & Act - a text area has no `type`, so honouring Lines here would render the
        // credential in clear text. Masking wins, matching the MudBlazor adapter (#207).
        var component = RenderField(new TestModel(), f => f
            .WithLabel("Password")
            .WithAttribute("InputType", "password")
            .WithAttribute("Lines", 4));

        // Assert
        component.FindComponents<FluentTextArea>().ShouldBeEmpty();
        component.FindComponent<FluentTextInput>().Instance.TextInputType
            .ShouldBe(TextInputType.Password);
    }

    [Theory]
    [InlineData("email", TextInputType.Email)]
    [InlineData("tel", TextInputType.Telephone)]
    [InlineData("url", TextInputType.Url)]
    [InlineData("search", TextInputType.Search)]
    [InlineData("number", TextInputType.Number)]
    [InlineData("nonsense", TextInputType.Text)]
    public void Configured_Input_Type_Should_Map_Onto_The_Fluent_Enum(string configured, TextInputType expected)
    {
        // Arrange & Act
        var component = RenderField(new TestModel(), f => f
            .WithLabel("Field")
            .WithAttribute("InputType", configured));

        // Assert - an unrecognised value falls back to Text rather than throwing
        component.FindComponent<FluentTextInput>().Instance.TextInputType.ShouldBe(expected);
    }
}
