using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Diagnostics;

/// <summary>
/// Tests the diagnostic that reports an adornment displaced by the password visibility toggle (#219).
/// </summary>
/// <remarks>
/// A field has exactly one adornment slot. <c>.AsPassword(enableVisibilityToggle: true)</c> claims it
/// for the show/hide eye, so an adornment configured alongside — and any <c>onClick</c> handler with
/// it — is discarded. #192 made that handler live on both render paths, which leaves this the one
/// combination where it is still dropped, and nothing said so.
/// <para>
/// The combination is a genuine either/or, so warning is the right outcome: nothing here changes what
/// renders.
/// </para>
/// </remarks>
public class PasswordAdornmentDiagnosticTests : MudBlazorTestBase
{
    private readonly CapturingLoggerProvider _logs = new();

    public PasswordAdornmentDiagnosticTests()
    {
        Services.AddLogging(builder => builder.AddProvider(_logs));
    }

    [Fact]
    public void Should_Warn_When_The_Toggle_Displaces_A_Configured_Adornment()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Password")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.End, onClick: _ => { })
                .AsPassword())
            .Build();

        // Act
        RenderForm(config);

        // Assert - names the field, and says which setting lost.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Password");
        warnings[0].ShouldContain("visibility toggle");
    }

    [Fact]
    public void Should_Not_Warn_When_The_Toggle_Is_Off()
    {
        // Arrange - with the toggle disabled the adornment and its handler are honoured, so there is
        // nothing to report. This is the case that makes the warning actionable rather than noise:
        // it names a real way out.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Password")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.End, onClick: _ => { })
                .AsPassword(enableVisibilityToggle: false))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Warn_For_A_Password_Field_With_No_Adornment()
    {
        // Arrange - the overwhelmingly common password field must stay silent.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f.WithLabel("Password").AsPassword())
            .Build();

        // Act
        RenderForm(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Warn_For_An_Adornment_On_A_NonPassword_Field()
    {
        // Arrange - nothing is displaced when there is no toggle to displace it.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Search")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.End, onClick: _ => { }))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Warn_Only_Once_Even_After_A_Rerender()
    {
        // Arrange - a configuration fact, not an event.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Password")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.End, onClick: _ => { })
                .AsPassword())
            .Build();

        // Act
        var component = RenderForm(config);
        component.Render();
        component.Render();

        // Assert
        _logs.Warnings.Count.ShouldBe(1);
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderForm(
        IFormConfiguration<TestModel> config) =>
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, config));

    private class TestModel
    {
        public string Secret { get; set; } = string.Empty;
    }
}
