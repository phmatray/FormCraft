using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Diagnostics;

/// <summary>
/// Pins the two ends of <c>MudBlazorFieldComponentBase.ShouldReport</c>: a field with no scope at all
/// always reports (#284), and a field inside a form reports once however often it is re-mounted
/// (#304).
/// </summary>
/// <remarks>
/// <para>
/// <c>ShouldReport</c> tries three things in order — the <c>CollectionItemFieldScope</c>, then the
/// <c>FormDiagnosticScope</c>, then <c>true</c>. This suite covers the <b>first</b> and <b>last</b> of
/// those, which are the two that go wrong silently. A collection renders one component instance per
/// row, so the latch exists to stop a 50-item collection emitting 50 identical warnings about one
/// field's configuration. With neither scope there is nothing to de-duplicate against, and the field
/// must report — which is the overwhelmingly common case.
/// </para>
/// <para>
/// ⛔ <b>Invert that default and the failure is silent and inverted-looking.</b> The diagnostics go
/// quiet for ordinary fields while continuing to work inside collections, so the bug hides in the
/// case nobody writes a test for and survives in the case everybody does. #284 unified four
/// hand-written copies of the idiom — one of them spelled <c>== false</c>, the opposite way round —
/// into <c>MudBlazorFieldComponentBase.ShouldReport</c>. That made the default correct in exactly
/// one place, and therefore wrong in exactly one place too: every diagnostic in the package now
/// fails or survives together.
/// </para>
/// <para>
/// <b>Why a suite of its own, when each diagnostic already has one.</b> Each of the four suites does
/// happen to render its diagnostic outside a collection, so the polarity is covered today — but
/// incidentally, as a side effect of testing the <i>rule</i>, and no comment there says the
/// placement is load-bearing. Nothing stops a future edit from moving those cases inside a
/// collection for convenience and taking the coverage with them. These tests exist to say the quiet
/// part: <b>outside a collection is the case under test</b>.
/// </para>
/// <para>
/// <b>The polarity tests render bare components, not through
/// <see cref="FormCraftComponent{TModel}"/>.</b> That is required rather than incidental for the
/// ShrinkLabel diagnostic: inside a form there is a <c>ShrinkLabelDiagnosticCollector</c>, and the
/// component reports to it and returns <i>before</i> reaching the latch at all. Its latch branch is
/// only observable with no collector and no scope, which is exactly what rendering the component
/// directly gives. The other three take the same route for consistency, and because it is the
/// smaller surface. <b>A bare component also has no form</b>, so those four tests exercise the
/// <c>true</c> arm specifically — which is what makes them the guard against inverting it.
/// </para>
/// <para>
/// <b>The re-mount tests are the opposite, and must render through
/// <see cref="FormCraftComponent{TModel}"/> (#304).</b> The scope they are about is cascaded by the
/// form, so a bare component would not have one and the test would pass for the wrong reason. They
/// gate a field with <c>.VisibleWhen(...)</c> and toggle it: that destroys and re-creates the
/// component, which is precisely what a per-instance flag cannot survive. Both placements belong in
/// this file because they are the same question — <i>where does the latch live?</i> — asked at its two
/// boundaries.
/// </para>
/// </remarks>
public class DiagnosticLatchTests : MudBlazorTestBase
{
    private readonly CapturingLoggerProvider _logs = new();

    public DiagnosticLatchTests()
    {
        Services.AddLogging(builder => builder.AddProvider(_logs));
    }

    [Fact]
    public void MaskedLines_Should_Report_For_A_Field_Outside_A_Collection()
    {
        // Arrange - masked and multi-line: MudBlazor renders a <textarea> past one line and a
        // textarea cannot mask, so the line count is dropped to keep the value hidden (#207).
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Password")
                .AsPassword()
                .AsTextArea(lines: 4))
            .Build();

        // Act
        RenderTextField(config, new TestModel());

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Password");
        warnings[0].ShouldContain("masked");
    }

    [Fact]
    public void PasswordAdornment_Should_Report_For_A_Field_Outside_A_Collection()
    {
        // Arrange - a field has one adornment slot and the visibility toggle takes it, so the
        // configured adornment and its click handler are discarded (#219).
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Password")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.End, onClick: _ => { })
                .AsPassword())
            .Build();

        // Act
        RenderTextField(config, new TestModel());

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Password");
        warnings[0].ShouldContain("visibility toggle");
    }

    [Fact]
    public void MaskedValue_Should_Report_For_A_Field_Outside_A_Collection()
    {
        // Arrange - legacy data the mask rejects outright: the field renders blank while the model
        // keeps "N/A", and a user who submits without touching it leaves it there (#266).
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Phone")
                .WithAttribute("Mask", "(000) 000-0000"))
            .Build();

        // Act
        RenderTextField(config, new TestModel { Secret = "N/A" });

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Phone");
        warnings[0].ShouldContain("(000) 000-0000");
    }

    [Fact]
    public void ShrinkLabel_Should_Report_For_A_Field_Outside_A_Collection_And_Outside_A_Form()
    {
        // Arrange - ShrinkLabel=false with a placeholder, which MudBlazor lets win (#181). Rendered
        // bare, so there is no collector to report to and the latch branch is the one taken; inside
        // a FormCraftComponent this returns at the collector and never consults the latch at all.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Email")
                .WithPlaceholder("user@example.com")
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderTextField(config, new TestModel());

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Email");
        warnings[0].ShouldContain("Placeholder");
    }

    [Fact]
    public void A_Field_Re_Mounted_Inside_A_Form_Should_Report_Once()
    {
        // Arrange - the #304 case. `.VisibleWhen(...)` gates the field's whole markup, so toggling
        // the condition destroys the component and toggling back builds a NEW one. Every latch the
        // package has is either per-instance (gone with the instance) or hangs off a
        // CollectionItemFieldScope, which an ordinary field does not have — so each re-mount runs
        // OnInitialized again and re-reports a configuration fact that has not changed.
        //
        // Inside a form there is somewhere for a durable latch to live, and this asserts it is used.
        // The label is deliberately NOT "Password": MaskedLinesDiagnostic's own message ends with
        // "drop .AsPassword() if the value is not a secret", so asserting on "Password" would match
        // the template rather than the field name and could never fail.
        var model = new TestModel { Secret = string.Empty, ShowSecret = true };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Recovery phrase")
                .AsPassword()
                .AsTextArea(lines: 4)
                .VisibleWhen(m => m.ShowSecret))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act - three full hide/show cycles. Today each one emits another copy.
        for (var i = 0; i < 3; i++)
        {
            model.ShowSecret = false;
            component.Render();
            model.ShowSecret = true;
            component.Render();
        }

        // Assert - the configuration was reported once and has not changed since, and the one
        // surviving warning is the one about this field rather than an unrelated survivor.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Recovery phrase");
    }

    [Fact]
    public void Two_Different_Fields_In_One_Form_Should_Each_Still_Report()
    {
        // Arrange - the guard against over-latching. A latch keyed too coarsely (per form, per
        // category, anything that forgets the FIELD) would silence the second field entirely, which
        // is a worse failure than the duplication being fixed: it loses a real diagnostic rather
        // than repeating one.
        // Neither label appears in MaskedLinesDiagnostic's message template, so each assertion below
        // can only be satisfied by the field name it names. "Password" would not qualify — the
        // template itself says "drop .AsPassword()".
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Secret, f => f
                .WithLabel("Primary code")
                .AsPassword()
                .AsTextArea(lines: 4))
            .AddField(x => x.Backup, f => f
                .WithLabel("Backup code")
                .AsPassword()
                .AsTextArea(lines: 3))
            .Build();

        // Act
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - one per field, each naming its own.
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(2);
        warnings.ShouldContain(w => w.Contains("Primary code"));
        warnings.ShouldContain(w => w.Contains("Backup code"));
    }

    /// <summary>
    /// Renders the field as a bare component: no <see cref="FormCraftComponent{TModel}"/>, so no
    /// diagnostic collector cascades in, and no collection, so no
    /// <c>CollectionItemFieldScope</c> does either. Both absences are the point.
    /// </summary>
    private void RenderTextField(IFormConfiguration<TestModel> config, TestModel model)
    {
        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(string),
            CurrentValue = model.Secret,
        };

        Render<MudBlazorTextFieldComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));
    }

    private class TestModel
    {
        public string Secret { get; set; } = string.Empty;

        public string Backup { get; set; } = string.Empty;

        public bool ShowSecret { get; set; }
    }
}
