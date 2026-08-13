using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class Home : IDisposable
{
    private const string InstallCommand = "dotnet add package FormCraft.ForMudBlazor";

    private bool _copiedInstall;

    /// <summary>
    /// Cancels the pending "copied" reset when the page is torn down.
    /// </summary>
    /// <remarks>
    /// Unlike the hero's flash this is real state rather than a one-shot animation — the button's label
    /// stays "Copied" until it is cleared — so it needs a token rather than a CSS rule. Without one, a
    /// visitor who navigates inside the two-second window resumes the continuation on a component the
    /// renderer has already disposed, and <c>StateHasChanged</c> throws: a console error on WebAssembly,
    /// the error UI on a circuit.
    /// </remarks>
    private CancellationTokenSource? _resetCts;

    /// <summary>
    /// Set once the page has been torn down, so a continuation can tell it has nobody to render to.
    /// </summary>
    /// <remarks>
    /// The token covers the two-second wait, but the interop call before it is an await of its own —
    /// short, yet still long enough to navigate across — and it completes before any token exists.
    /// </remarks>
    private bool _disposed;

    private async Task CopyInstall()
    {
        try
        {
            _copiedInstall = await JS.InvokeAsync<bool>("formcraftCopy", InstallCommand);
        }
        // "Fallback if the clipboard API is not available" is wider than JSException on its own: none
        // of the three below derive from it, so narrowing to JSException let them escape as unhandled
        // component exceptions. Copying a command to the clipboard is a convenience — no failure of it
        // is worth surfacing an error to the visitor, who can still select the text.
        catch (Exception ex) when (ex
            is JSException                  // the clipboard API is missing, or the call itself threw
            or JSDisconnectedException      // the circuit is gone; does not derive from JSException
            or InvalidOperationException    // "JavaScript interop calls cannot be issued at this time"
            or TaskCanceledException)       // the interop call hit its timeout
        {
            _copiedInstall = false;
        }

        if (_disposed || !_copiedInstall)
        {
            return;
        }

        // A second click restarts the window instead of being cut short by the first click's reset.
        _resetCts?.Cancel();
        _resetCts?.Dispose();
        _resetCts = new CancellationTokenSource();
        var token = _resetCts.Token;

        StateHasChanged();

        try
        {
            await Task.Delay(2000, token);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a later click, or the page went away. Either way there is nothing to
            // reset and nobody to re-render.
            return;
        }

        _copiedInstall = false;
        StateHasChanged();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _resetCts?.Cancel();
        _resetCts?.Dispose();
        _resetCts = null;
    }
}
