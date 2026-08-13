using Microsoft.JSInterop;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests for the shared swallow-safe focus call (#318), extracted from the upload base #281 built.
/// </summary>
/// <remarks>
/// <para>
/// The behaviour under test is deliberately dull: focus the target if there is one, and never throw.
/// It earns a home of its own because <b>every</b> control that unmounts or disables itself needs it,
/// and copying the catch list per component is the failure class this library keeps re-filing
/// (#146, #177, #184, #189) — the last time it was copied, one copy silently lost
/// <c>catch (JSException)</c> and escaped into the click handler.
/// </para>
/// <para>
/// The assertion technique is #281's, documented at length on <see cref="FileUploadClearFocusTests"/>:
/// bUnit models no real DOM focus, so tests assert the recorded
/// <c>Blazor._internal.domWrapper.focus</c> interop invocation. ⛔ Never reflect into MudBlazor's
/// private <c>MudBaseButton._elementReference</c> to identify a button — it breaks on any patch
/// release.
/// </para>
/// </remarks>
public class FocusRestoreTests : MudBlazorTestBase
{
    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

    [Fact]
    public async Task FocusSafelyAsync_Should_Focus_The_Target()
    {
        // Arrange
        var button = Render<MudButton>(parameters => parameters.AddChildContent("Browse"));

        // Act
        await button.InvokeAsync(() => FocusRestore.FocusSafelyAsync(button.Instance));

        // Assert
        FocusCount().ShouldBe(1);
    }

    [Fact]
    public async Task FocusSafelyAsync_With_No_Target_Should_Do_Nothing()
    {
        // Arrange - the null case is not hypothetical: @ref is unassigned until a render completes,
        // and a control gated behind an @if may never have rendered at all.

        // Act
        await Should.NotThrowAsync(() => FocusRestore.FocusSafelyAsync(null));

        // Assert - and it must not manufacture an interop call either
        FocusCount().ShouldBe(0);
    }

    [Fact]
    public async Task FocusSafelyAsync_Should_Swallow_A_Failing_Focus_Call()
    {
        // Arrange - JSException with this wording is what Blazor's domWrapper.focus raises for an
        // element that has left the DOM. Letting it escape tears down a Blazor Server circuit,
        // which is strictly worse than the focus bug this helper exists to fix.
        JSInterop
            .SetupVoid(FocusIdentifier, _ => true)
            .SetException(new JSException("Unable to focus an invalid element."));

        var button = Render<MudButton>(parameters => parameters.AddChildContent("Browse"));

        // Act & Assert
        await Should.NotThrowAsync(() =>
            button.InvokeAsync(() => FocusRestore.FocusSafelyAsync(button.Instance)));
    }

    [Fact]
    public async Task FocusSafelyAsync_Should_Focus_An_Icon_Button_Too()
    {
        // Arrange - the collection field's reorder and delete controls are MudIconButton, not
        // MudButton. Both derive from MudBaseButton, which is why the helper takes that type; this
        // pins it, because a signature narrowed to MudButton would compile everywhere except there.
        var button = Render<MudIconButton>(parameters => parameters
            .Add(p => p.Icon, Icons.Material.Filled.Delete));

        // Act
        await button.InvokeAsync(() => FocusRestore.FocusSafelyAsync(button.Instance));

        // Assert
        FocusCount().ShouldBe(1);
    }

    private int FocusCount() => JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier);
}
