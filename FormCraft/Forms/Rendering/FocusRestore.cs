using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FormCraft;

/// <summary>
/// Moves keyboard focus to a control, and never lets the attempt throw (#318, #337).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> A control whose render condition depends on the value its own handler
/// mutates destroys its own reachability when activated: the element the keyboard user is standing
/// on unmounts (or becomes disabled), focus falls to <c>&lt;body&gt;</c>, and the next
/// <kbd>Tab</kbd> restarts from the top of the document — WCAG 2.1 <b>2.4.3 Focus Order</b>
/// (Level A). Every such control has to move focus deliberately; this is the one call they all make.
/// </para>
/// <para>
/// It is shared rather than reimplemented per component because the catch list below is the whole
/// substance, and it was got wrong the first time it was written: #281 shipped without
/// <see cref="JSException"/> and a failed focus escaped the click handler. Per-component copies of a
/// shared behaviour are the failure class this library keeps re-filing (#146, #177, #184, #189).
/// </para>
/// <para>
/// ⛔ <b>Do not narrow the catch list.</b> Focus is the courtesy on top of an action that has
/// <i>already succeeded</i> — the file is removed, the row is deleted, the item has moved. Letting a
/// focus failure surface would turn a completed action into an unhandled exception, and on Blazor
/// Server that tears down the circuit and every other field's state with it. Failing to move focus
/// is a small accessibility regression; throwing here is a data-loss bug.
/// </para>
/// <para>
/// Lived in <c>FormCraft.ForMudBlazor</c> as <c>internal</c> until #337, at which point a second
/// adapter (<c>FormCraft.ForFluentUI</c>) needed the identical catch list — the drift #279 already
/// fixed once for <see cref="NativeRequired"/> happening for the same reason. Public and in core, so
/// there is one implementation of the swallow-safe call again. Unlike <see cref="NativeRequired"/>,
/// the UI-specific half (which control types can even be focused) still lives per adapter: MudBlazor
/// forwards a <c>MudBaseButton</c>'s own <c>FocusAsync()</c> through the delegate overload below;
/// Fluent UI Blazor's <c>FluentButton</c> (pinned v5 RC) exposes no focus API of its own at all — no
/// <c>FocusAsync()</c>, no public <c>ElementReference</c> — so its adapter targets a plain wrapping
/// element instead, through the <see cref="ElementReference"/> overload only.
/// </para>
/// </remarks>
public static class FocusRestore
{
    /// <summary>
    /// Focuses a plain element, for the case where no framework-specific control survives the action
    /// at all — or, for an adapter whose control type exposes no focus API of its own, for every
    /// target in the chain.
    /// </summary>
    /// <remarks>
    /// The MudBlazor collection field needs this for its last-resort target: removing a row down to
    /// <c>MinItems</c> in a field that also forbids adding leaves the field with no focusable
    /// control, so the fallback target is the collection's own header — which carries the
    /// collection's label, so a screen reader says where the user has landed rather than going
    /// silent. The Fluent UI adapter needs it for every target, MudBlazor's included, because
    /// <c>FluentButton</c> has nothing else to offer (see the class remarks).
    /// </remarks>
    public static Task SafelyAsync(ElementReference target) =>
        SafelyAsync(() => target.FocusAsync());

    /// <summary>
    /// Focuses whatever <paramref name="focus"/> targets, swallowing every failure that means
    /// "there is no live element to focus".
    /// </summary>
    /// <param name="focus">
    /// The framework-specific focus call — typically a control's own <c>FocusAsync()</c>, or an
    /// <see cref="ElementReference"/>'s, bound in a closure. Public so an adapter whose control type
    /// exposes its own focus method (MudBlazor's <c>MudBaseButton</c>) can forward straight to it
    /// without a second copy of the catch list.
    /// </param>
    public static async Task SafelyAsync(Func<ValueTask> focus)
    {
        try
        {
            await focus();
        }
        catch (JSException)
        {
            // The element is no longer focusable — typically gone from the DOM. This is the likely
            // one: assigning the value raises OnValueChanged, so a parent that hides the field or
            // drops the row can unmount the target before the awaited interop call reaches it.
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone; there is nothing left to focus.
        }
        catch (OperationCanceledException)
        {
            // The interop call timed out or was cancelled.
        }
        catch (ObjectDisposedException)
        {
            // The component was torn down mid-action.
        }
        catch (InvalidOperationException)
        {
            // No usable JS runtime behind the reference yet — the prerender/SSR pass, where
            // RemoteJSRuntime rejects interop issued before the client connects. Broader than that
            // one cause and knowingly so; see the class remarks on why swallowing wins here.
        }
    }
}
