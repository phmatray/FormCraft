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

    [Fact]
    public void Should_Warn_Once_Per_Field_Not_Once_Per_Collection_Row()
    {
        // Arrange - #203. This diagnostic became reachable from inside a collection when item fields
        // started rendering through the same component as everything else: the hand-rolled path had
        // no password toggle at all, so it could never displace an adornment.
        //
        // That is one component instance PER ROW, and the warning fires from OnInitialized, so
        // without a latch a three-row collection reports the same field's configuration three times.
        // A 50-row one would report it fifty times. The configuration is a property of the FIELD, not
        // of a row, so the correct count is one however many rows exist.
        var config = FormBuilder<VaultModel>
            .Create()
            .AddCollectionField(x => x.Entries, collection => collection
                .WithLabel("Entries")
                .WithItemForm(item => item
                    .AddField(x => x.Secret, f => f
                        .WithLabel("Password")
                        .WithAdornment(Icons.Material.Filled.Search, Adornment.End, onClick: _ => { })
                        .AsPassword())))
            .Build();

        var model = new VaultModel { Entries = { new VaultEntry(), new VaultEntry(), new VaultEntry() } };

        // Act
        var component = Render<FormCraftComponent<VaultModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - three rows really did render, and produced one warning between them.
        component.FindComponents<MudTextField<string>>().Count.ShouldBe(3);
        _logs.Warnings.Count.ShouldBe(1);
        _logs.Warnings[0].ShouldContain("Password");
    }

    [Fact]
    public void Two_Diagnostics_On_One_Item_Field_Should_Both_Be_Reported()
    {
        // Arrange - the latch is keyed by (diagnostic, field), and this is why. A single field can
        // trip more than one: masked + multi-line reports the dropped line count (#207), and the
        // visibility toggle displacing the adornment reports that (#219). Both are true here.
        //
        // The code #203 replaced kept two separate HashSets for exactly this reason, with the note
        // that "a shared latch would let whichever fired first silence the other on the same field".
        // Latching on the field alone would report one of these and hide the other for good — a
        // silent loss of a security-adjacent warning, which is the shape of bug this whole issue is
        // about.
        var config = FormBuilder<VaultModel>
            .Create()
            .AddCollectionField(x => x.Entries, collection => collection
                .WithLabel("Entries")
                .WithItemForm(item => item
                    .AddField(x => x.Secret, f => f
                        .WithLabel("Password")
                        .WithAdornment(Icons.Material.Filled.Search, Adornment.End, onClick: _ => { })
                        .AsPassword()
                        .AsTextArea(lines: 4))))
            .Build();

        // Act
        Render<FormCraftComponent<VaultModel>>(parameters => parameters
            .Add(p => p.Model, new VaultModel { Entries = { new VaultEntry() } })
            .Add(p => p.Configuration, config));

        // Assert - one of each, not one in total.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(2);
        warnings.ShouldContain(w => w.Contains("visibility toggle"));
        warnings.ShouldContain(w => w.Contains("lines"));
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

    private class VaultModel
    {
        public List<VaultEntry> Entries { get; set; } = new();
    }

    private class VaultEntry
    {
        public string Secret { get; set; } = string.Empty;
    }
}
