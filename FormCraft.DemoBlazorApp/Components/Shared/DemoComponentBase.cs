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
/// ⚠️ <b>The token is per component, never per call, and nothing cancels it but disposal.</b> An
/// earlier draft gave each call a fresh source and cancelled the previous one, so that re-clicking a
/// button restarted its window. That was wrong on any page with two independent delays: a page with
/// three submit handlers (<c>PasswordFieldDemo</c>) or a dependent-dropdown chain
/// (<c>AsyncValueProviderDemo</c>) had one operation silently abort another, which left
/// <c>_isSubmitting</c> / <c>_loadingCities</c> stuck true and the spinner running forever. Independent
/// waits must stay independent — which is also exactly what the raw <c>Task.Delay</c> calls this
/// replaced did.
/// </para>
/// </remarks>
public abstract class DemoComponentBase : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _lifetimeCts = new();
    private bool _disposed;

    /// <summary>
    /// Whether the component has been torn down. Check this before calling
    /// <see cref="ComponentBase.StateHasChanged"/> from anything that can outlive the component — a
    /// continuation after <em>any</em> await, or a timer callback.
    /// </summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Waits <paramref name="milliseconds"/> and reports whether the component is still around.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the wait completed and the component is still alive — safe to mutate state and
    /// re-render. <c>false</c> only if the component was disposed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Two hazards, both live findings on #285: the token covers the wait itself, and the
    /// <see cref="_disposed"/> re-check afterwards covers the gap where the timer has already fired and
    /// the continuation is queued on the dispatcher — cancellation cannot help once that has happened.
    /// </para>
    /// <para>
    /// Concurrent calls do not interfere: they share the component's lifetime token but none of them
    /// cancels another, so <c>false</c> means "the page is gone" and nothing else.
    /// </para>
    /// <para>
    /// Nothing forces a caller to inspect the result — C# raises no diagnostic for a discarded return
    /// value, so this is a convention the reviewer has to hold up, not something the compiler enforces.
    /// <c>SecurityDemo</c> ignores it deliberately and guards its <c>finally</c> with
    /// <see cref="IsDisposed"/> instead, because a <c>finally</c> runs even when the <c>try</c>
    /// returned early.
    /// </para>
    /// </remarks>
    protected async Task<bool> DelayAsync(int milliseconds)
    {
        if (_disposed)
        {
            return false;
        }

        try
        {
            await Task.Delay(milliseconds, _lifetimeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return !_disposed;
    }

    /// <summary>
    /// Cancels every pending wait and marks the component disposed.
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
        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
        GC.SuppressFinalize(this);
    }
}
