using FormCraft.ForFluentUI.UnitTests.TestSupport;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// Tests for the Fluent-typed focus wrapper (#337), mirroring
/// <c>FormCraft.ForMudBlazor.UnitTests.Fields.FocusRestoreTests</c> for this adapter's
/// <see cref="ElementReference"/>-only mechanism (see <see cref="FocusAssertingTestBase"/> remarks
/// for why Fluent has no button-typed overload to test).
/// </summary>
public class FocusRestoreTests : FocusAssertingTestBase
{
    private sealed class Host : Microsoft.AspNetCore.Components.ComponentBase
    {
        public ElementReference Target;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "tabindex", "-1");
            builder.AddElementReferenceCapture(2, r => Target = r);
            builder.CloseElement();
        }
    }

    [Fact]
    public async Task FocusSafelyAsync_Should_Focus_The_Target()
    {
        // Arrange
        var component = Render<Host>();

        // Act
        await component.InvokeAsync(() => FocusRestore.FocusSafelyAsync(component.Instance.Target));

        // Assert
        FocusCount().ShouldBe(1);
    }

    [Fact]
    public async Task FocusSafelyAsync_Should_Swallow_A_Failing_Focus_Call()
    {
        // Arrange - JSException with this wording is what Blazor's domWrapper.focus raises for an
        // element that has left the DOM. Letting it escape tears down a Blazor Server circuit, which
        // is strictly worse than the focus bug this helper exists to fix. FluentUITestBase runs
        // JSInterop in Loose mode, where focus always succeeds - without this, the catch block below
        // has zero coverage.
        FailTheFocusInterop();
        var component = Render<Host>();

        // Act & Assert
        await Should.NotThrowAsync(() =>
            component.InvokeAsync(() => FocusRestore.FocusSafelyAsync(component.Instance.Target)));
    }
}
