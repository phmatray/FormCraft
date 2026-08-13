using FormCraft.ForFluentUI.Extensions;
using FormCraft.ForFluentUI.UnitTests.Components;

namespace FormCraft.ForFluentUI.UnitTests.Extensions;

/// <summary>
/// The typed <c>.WithNativeRequired(...)</c> replaces the raw
/// <c>.WithAttribute("Required", ...)</c> form for Fluent consumers (#278).
/// </summary>
/// <remarks>
/// Called as a static method throughout, not as an extension. This project references both adapters,
/// and the MudBlazor package publishes a <c>WithNativeRequired</c> of the same name into namespace
/// <c>FormCraft</c>; the extension-method form would therefore be <c>CS0121</c>-ambiguous here. A
/// Fluent-only application has one in scope and writes <c>.WithNativeRequired()</c> normally - which
/// is the whole reason the Fluent one lives in its own namespace.
/// </remarks>
public class NativeRequiredBuilderTests : FluentUITestBase
{
    [Fact]
    public void WithNativeRequired_Should_Write_The_Required_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => FluentUIFieldBuilderExtensions.WithNativeRequired(f.WithLabel("Name")))
            .Build();

        // Assert
        config.Fields[0].AdditionalAttributes["Required"].ShouldBe(true);
    }

    [Fact]
    public void WithNativeRequired_False_Should_Write_The_Opt_Out()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => FluentUIFieldBuilderExtensions.WithNativeRequired(f.WithLabel("Name"), false))
            .Build();

        // Assert
        config.Fields[0].AdditionalAttributes["Required"].ShouldBe(false);
    }

    [Fact]
    public void WithNativeRequired_Alone_Should_Announce_The_Field_As_Required()
    {
        // Arrange - the decoration without the validator, which is the point of the opt-in
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => FluentUIFieldBuilderExtensions.WithNativeRequired(f.WithLabel("Name")))
            .Build();

        // Act
        var component = Render(config);

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void WithNativeRequired_False_Should_Suppress_The_Announcement_On_A_Required_Field()
    {
        // Arrange - the explicit opt-out must beat the validator's own answer (#199, #204)
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => FluentUIFieldBuilderExtensions.WithNativeRequired(
                f.WithLabel("Name").Required("Name is required"), false))
            .Build();

        // Act
        var component = Render(config);

        // Assert
        component.FindAll("[aria-required='true']").ShouldBeEmpty();
    }

    [Fact]
    public void WithNativeRequired_Should_Agree_With_The_Raw_Attribute_Form()
    {
        // Arrange - the typed method is sugar over the documented magic string, not a second
        // mechanism; if these ever diverge the sugar is lying.
        var typed = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => FluentUIFieldBuilderExtensions.WithNativeRequired(f.WithLabel("Name")))
            .Build();
        var raw = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").WithAttribute("Required", true))
            .Build();

        // Assert
        typed.Fields[0].AdditionalAttributes["Required"]
            .ShouldBe(raw.Fields[0].AdditionalAttributes["Required"]);
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> Render(IFormConfiguration<TestModel> config)
        => Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));
}
