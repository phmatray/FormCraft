using FormCraft.ForFluentUI.UnitTests.Components;

namespace FormCraft.ForFluentUI.UnitTests.Extensions;

/// <summary>
/// The Fluent adapter honours the typed <c>.WithNativeRequired(...)</c> (#278).
/// </summary>
/// <remarks>
/// The method itself is <b>core's</b> since #279, not this package's. This issue's plan allowed for
/// exactly that: "check whether the shared-machinery issue has moved <c>WithNativeRequired</c> into
/// core - if it has, this task is only a test that Fluent honours it". So these assert the
/// adapter's end of the contract - that the attribute the builder writes reaches
/// <c>aria-required</c> through <c>NativeRequired.Resolve</c> - rather than re-testing the builder.
/// <para>
/// A Fluent copy of the method would be worse than redundant: core's lives in namespace
/// <c>FormCraft</c>, which every consumer imports for <c>FormBuilder</c>, so a second identical
/// signature would make every call <c>CS0121</c>-ambiguous.
/// </para>
/// </remarks>
public class NativeRequiredBuilderTests : FluentUITestBase
{
    [Fact]
    public void WithNativeRequired_Should_Write_The_Required_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").WithNativeRequired())
            .Build();

        // Assert
        config.Fields[0].AdditionalAttributes[NativeRequired.AttributeName].ShouldBe(true);
    }

    [Fact]
    public void WithNativeRequired_False_Should_Write_The_Opt_Out()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").WithNativeRequired(false))
            .Build();

        // Assert
        config.Fields[0].AdditionalAttributes[NativeRequired.AttributeName].ShouldBe(false);
    }

    [Fact]
    public void WithNativeRequired_Alone_Should_Announce_The_Field_As_Required()
    {
        // Arrange - the decoration without the validator, which is the point of the opt-in
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").WithNativeRequired())
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
            .AddField(x => x.Name, f => f
                .WithLabel("Name")
                .Required("Name is required")
                .WithNativeRequired(false))
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
            .AddField(x => x.Name, f => f.WithLabel("Name").WithNativeRequired())
            .Build();
        var raw = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").WithAttribute("Required", true))
            .Build();

        // Assert
        typed.Fields[0].AdditionalAttributes[NativeRequired.AttributeName]
            .ShouldBe(raw.Fields[0].AdditionalAttributes["Required"]);
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> Render(IFormConfiguration<TestModel> config)
        => Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));
}
