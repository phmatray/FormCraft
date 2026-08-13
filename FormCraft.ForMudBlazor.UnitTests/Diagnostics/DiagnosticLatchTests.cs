using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Diagnostics;

/// <summary>
/// Pins the polarity of the once-per-field diagnostic latch: a field with no
/// <c>CollectionItemFieldScope</c> always reports (#284).
/// </summary>
/// <remarks>
/// <para>
/// The latch reads <c>ItemFieldScope?.ShouldWarnOnce(category, DiagnosticFieldKey) ?? true</c>, and
/// the <c>?? true</c> is the whole of this suite. A collection renders one component instance per
/// row, so the latch exists to stop a 50-item collection emitting 50 identical warnings about one
/// field's configuration. Outside a collection there is no scope, nothing to de-duplicate, and the
/// field must report — which is the overwhelmingly common case.
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
/// <b>Rendered as bare components, not through <see cref="FormCraftComponent{TModel}"/>.</b> That is
/// required rather than incidental for the ShrinkLabel diagnostic: inside a form there is a
/// <c>ShrinkLabelDiagnosticCollector</c>, and the component reports to it and returns <i>before</i>
/// reaching the latch at all. Its latch branch is only observable with no collector and no scope,
/// which is exactly what rendering the component directly gives. The other three take the same route
/// for consistency, and because it is the smaller surface.
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
    }
}
