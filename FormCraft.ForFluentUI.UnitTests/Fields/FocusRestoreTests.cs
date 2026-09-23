using FormCraft.ForFluentUI.UnitTests.TestSupport;
using Microsoft.JSInterop;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// Tests for the Fluent-typed focus wrapper (#337, #383), mirroring
/// <c>FormCraft.ForMudBlazor.UnitTests.Fields.FocusRestoreTests</c> for this adapter's two
/// mechanisms (see <see cref="FocusAssertingTestBase"/> remarks): the <see cref="ElementReference"/>
/// overload for the collection field's plain <c>&lt;div&gt;</c> fallbacks, and the
/// <see cref="Func{ValueTask}"/> overload the four <c>FluentButton</c> controls focus through since
/// no button-typed overload exists for Fluent to test directly.
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

    [Fact]
    public async Task FocusSafelyAsync_Func_Should_Invoke_The_Delegate()
    {
        // Arrange - the route the collection field's four FluentButton controls use (#383): there is
        // no ElementReference to give them, so this overload forwards an arbitrary async delegate
        // (in production, a JS module call keyed off the control's id) instead.
        var invoked = false;
        Func<ValueTask> focus = () =>
        {
            invoked = true;
            return ValueTask.CompletedTask;
        };

        // Act
        await FocusRestore.FocusSafelyAsync(focus);

        // Assert
        invoked.ShouldBeTrue();
    }

    [Fact]
    public async Task FocusSafelyAsync_Func_Should_Swallow_A_Failing_Focus_Call()
    {
        // Arrange - the same JSException wording Blazor's own domWrapper.focus raises for an element
        // that has left the DOM; the delegate overload must swallow it exactly like the
        // ElementReference one does.
        Func<ValueTask> throwing = () => throw new JSException("Unable to focus an invalid element.");

        // Act & Assert
        await Should.NotThrowAsync(() => FocusRestore.FocusSafelyAsync(throwing));
    }
}
