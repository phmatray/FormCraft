using Microsoft.JSInterop;

namespace FormCraft.UnitTests.Rendering;

/// <summary>
/// Tests the framework-neutral swallow-safe focus call now that it lives in core (#337, extracted
/// from <c>FormCraft.ForMudBlazor</c>'s <c>FocusRestore</c>, #318).
/// </summary>
/// <remarks>
/// The catch list is the whole substance of this helper and the one thing that must not drift between
/// copies — #281 shipped without <c>catch (JSException)</c> once already, and a failed focus call
/// escaped a click handler into production. These tests pin it directly against a plain
/// <c>Func&lt;ValueTask&gt;</c>, with no UI framework or bUnit host involved, so the same tests would
/// catch a regression whichever adapter calls through it.
/// </remarks>
public class FocusRestoreTests
{
    [Fact]
    public async Task SafelyAsync_Should_Invoke_A_Succeeding_Delegate_Exactly_Once()
    {
        // Arrange
        var callCount = 0;
        ValueTask Focus()
        {
            callCount++;
            return ValueTask.CompletedTask;
        }

        // Act
        await FocusRestore.SafelyAsync(Focus);

        // Assert
        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task SafelyAsync_Should_Swallow_A_JSException()
    {
        // Arrange - what Blazor's domWrapper.focus raises for an element that has left the DOM.
        ValueTask Focus() => throw new JSException("Unable to focus an invalid element.");

        // Act & Assert
        await Should.NotThrowAsync(() => FocusRestore.SafelyAsync(Focus));
    }

    [Fact]
    public async Task SafelyAsync_Should_Swallow_A_JSDisconnectedException()
    {
        // Arrange - the circuit is gone; there is nothing left to focus.
        ValueTask Focus() => throw new JSDisconnectedException("Circuit disconnected.");

        // Act & Assert
        await Should.NotThrowAsync(() => FocusRestore.SafelyAsync(Focus));
    }

    [Fact]
    public async Task SafelyAsync_Should_Swallow_An_OperationCanceledException()
    {
        // Arrange - the interop call timed out or was cancelled.
        ValueTask Focus() => throw new OperationCanceledException();

        // Act & Assert
        await Should.NotThrowAsync(() => FocusRestore.SafelyAsync(Focus));
    }

    [Fact]
    public async Task SafelyAsync_Should_Swallow_An_ObjectDisposedException()
    {
        // Arrange - the component was torn down mid-action.
        ValueTask Focus() => throw new ObjectDisposedException("target");

        // Act & Assert
        await Should.NotThrowAsync(() => FocusRestore.SafelyAsync(Focus));
    }

    [Fact]
    public async Task SafelyAsync_Should_Swallow_An_InvalidOperationException()
    {
        // Arrange - no usable JS runtime behind the reference yet (prerender/SSR).
        ValueTask Focus() => throw new InvalidOperationException("No JS runtime available.");

        // Act & Assert
        await Should.NotThrowAsync(() => FocusRestore.SafelyAsync(Focus));
    }

    [Fact]
    public async Task SafelyAsync_With_An_ElementReference_Should_Swallow_The_No_Runtime_Case()
    {
        // Arrange - a default ElementReference carries no JSRuntime context, so its FocusAsync()
        // throws InvalidOperationException the same way a prerender/SSR pass does before the client
        // connects. This exercises the ElementReference overload with no bUnit host at all.

        // Act & Assert
        await Should.NotThrowAsync(() => FocusRestore.SafelyAsync(default(ElementReference)));
    }
}
