using Microsoft.AspNetCore.Components;

namespace FormCraft.DemoBlazorApp.Components.Shared;

/// <summary>
/// Base for demo components that pause before re-rendering — a simulated API call, a "copied" badge
/// that clears itself, anything that awaits and then calls <see cref="ComponentBase.StateHasChanged"/>.
/// </summary>
/// <remarks>
/// <para>
/// The defect this exists to prevent: <c>await Task.Delay(2000)</c> followed by
/// <c>StateHasChanged()</c> resumes on a component the renderer may already have disposed, because the
/// visitor navigated during the wait. On this app — standalone WebAssembly — that surfaces as a console
/// error. #285 fixed two instances of it by hand; #315 found twenty-four more, which is the argument
/// for putting the guard somewhere it cannot be forgotten rather than writing it out again per page.
/// </para>
/// <para>
/// The shape matters: <see cref="DelayAsync"/> returns a <see cref="bool"/> rather than
/// <see cref="Task"/>, so the only way to carry on after the wait is to inspect the answer. A caller
/// that ignores it gets a compiler warning, and this project builds with
/// <c>TreatWarningsAsErrors</c>.
/// </para>
/// </remarks>
public abstract class DemoComponentBase : ComponentBase, IDisposable
{
    private CancellationTokenSource? _delayCts;
    private bool _disposed;

    /// <summary>
    /// Whether the component has been torn down. Check this after <em>any</em> other await — a JS
    /// interop call, an HTTP request — before calling <see cref="ComponentBase.StateHasChanged"/>.
    /// </summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Waits <paramref name="milliseconds"/> and reports whether the component is still around.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the wait completed and the component is still alive — safe to mutate state and
    /// re-render. <c>false</c> if the component was disposed, or a later call superseded this one.
    /// </returns>
    /// <remarks>
    /// Two separate hazards, both handled here because both were live findings on #285:
    /// the token covers the wait itself, and the <see cref="_disposed"/> re-check afterwards covers the
    /// gap where the timer has already fired and the continuation is queued on the dispatcher —
    /// cancellation cannot help once that has happened. A second call cancels the first, so
    /// re-clicking a button restarts its window instead of being cut short by the earlier reset.
    /// </remarks>
    protected async Task<bool> DelayAsync(int milliseconds)
    {
        if (_disposed)
        {
            return false;
        }

        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _delayCts = new CancellationTokenSource();
        var token = _delayCts.Token;

        try
        {
            await Task.Delay(milliseconds, token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return !_disposed;
    }

    /// <summary>
    /// Cancels any pending wait and marks the component disposed.
    /// </summary>
    /// <remarks>
    /// Virtual, and safe to call twice. A derived component with teardown of its own overrides this and
    /// calls <c>base.Dispose()</c> — <c>FormSlots</c> does exactly that for its countdown timer.
    /// </remarks>
    public virtual void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _delayCts?.Cancel();
        _delayCts?.Dispose();
        _delayCts = null;
        GC.SuppressFinalize(this);
    }
}
