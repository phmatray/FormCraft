using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Diagnostics;

/// <summary>
/// Tests the diagnostic that reports a multi-line setting dropped to keep a field masked (#207).
/// <para>
/// The render fix alone is silent: the developer wrote <c>.AsTextArea(...)</c> and gets a one-line
/// field, with nothing saying why. These tests assert FormCraft says so on both render paths, and —
/// just as important — that it stays quiet for an ordinary multi-line field and for a password
/// field that asked for no extra lines.
/// </para>
/// </summary>
public class MaskedLinesDiagnosticTests : MudBlazorTestBase
{
    private readonly CapturingLoggerProvider _logs = new();

    public MaskedLinesDiagnosticTests()
    {
        Services.AddLogging(builder => builder.AddProvider(_logs));
    }

    [Fact]
    public void Should_Warn_When_A_Password_Field_Also_Asks_For_Multiple_Lines()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Password")
                .AsPassword()
                .AsTextArea(lines: 4))
            .Build();

        // Act
        RenderStandalone(config);

        // Assert - names the field and both settings, so a form of many fields points at the right
        // one and the developer can see which of the two lost.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Password");
        warnings[0].ShouldContain("masked");
    }

    [Fact]
    public void Should_Warn_For_An_Item_Field_Too()
    {
        // Arrange - the collection path builds its tree with RenderTreeBuilder rather than through
        // MudBlazorFieldComponentBase, so it needs its own wiring and its own coverage. The clear
        // text bug was identical on both paths; the diagnostic must be too.
        var config = BuildItemFormConfiguration(f => f.AsPassword().AsTextArea(lines: 4));

        // Act
        RenderItemForm(config);

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Secret");
        warnings[0].ShouldContain("masked");
    }

    [Fact]
    public void Should_Not_Warn_For_A_Password_Field_With_No_Lines()
    {
        // Arrange - nothing was dropped, so there is nothing to report. The overwhelmingly common
        // password field must not start emitting a warning.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f.WithLabel("Password").AsPassword())
            .Build();

        // Act
        RenderStandalone(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Warn_For_An_Ordinary_Multiline_Field()
    {
        // Arrange - a textarea that is not masked is exactly what the developer asked for.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f.WithLabel("Notes").AsTextArea(lines: 4))
            .Build();

        // Act
        RenderStandalone(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Warn_Only_Once_Even_After_A_Rerender()
    {
        // Arrange - the conflict is a configuration fact, not an event. Re-reporting it on every
        // parameter change would flood the console as the user types, which is how a useful
        // diagnostic gets muted by the people it is meant to help.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Password")
                .AsPassword()
                .AsTextArea(lines: 4))
            .Build();

        // Act
        var component = RenderStandalone(config);
        component.Render();
        component.Render();

        // Assert
        _logs.Warnings.Count.ShouldBe(1);
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderStandalone(
        IFormConfiguration<TestModel> config)
    {
        var model = new TestModel();

        return Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<CredentialsModel>> RenderItemForm(
        IFormConfiguration<CredentialsModel> config)
    {
        var model = new CredentialsModel { Items = { new Credential { Secret = "hunter2" } } };

        return Render<FormCraftComponent<CredentialsModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));
    }

    private static IFormConfiguration<CredentialsModel> BuildItemFormConfiguration(
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

    private class TestModel
    {
        public string Secret { get; set; } = string.Empty;
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
