using Microsoft.AspNetCore.Components;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Moves keyboard focus to a Fluent control or a plain wrapping element, and never lets the attempt
/// throw (#337, #383).
/// </summary>
/// <remarks>
/// <para>
/// A thin, Fluent-typed wrapper over <see cref="global::FormCraft.FocusRestore"/> — the swallow-safe
/// catch list lives there, once, shared with <c>FormCraft.ForMudBlazor</c>'s own wrapper (#318).
/// </para>
/// <para>
/// <b>Two overloads for two Fluent focus mechanisms.</b> Fluent UI Blazor's <c>FluentButton</c> (the
/// pinned v5 RC, <c>Microsoft.FluentUI.AspNetCore.Components</c> 5.0.0-rc.5-26219.1) has no
/// <c>FocusAsync()</c> and no public <see cref="ElementReference"/> of its own — confirmed by
/// decompiling the installed assembly: <c>FluentButton</c> does not implement
/// <c>IFluentComponentElementBase</c> (the interface several other Fluent inputs use for exactly
/// this), and its render tree never calls <c>AddElementReferenceCapture</c>. A throwaway bUnit probe
/// (#383 Task 1 Step 1) additionally found it renders as a native custom element
/// (<c>&lt;fluent-button&gt;</c>) with no static CSS class list — ruling out reproducing it with a
/// bare <c>&lt;button&gt;</c> (the issue's Approach B) — but confirming its <c>Id</c> parameter
/// renders straight through to the element's <c>id</c> attribute, which <c>document.getElementById</c>
/// can reach directly (the issue's Approach A). The <see cref="ElementReference"/> overload below
/// still serves the collection field's plain <c>&lt;div&gt;</c> fallbacks (the header and per-row
/// header, which stay non-interactive landing spots); the <see cref="Func{ValueTask}"/> overload
/// serves its four <c>FluentButton</c> controls (Add, Remove, Move up, Move down) through the small
/// colocated JS module <c>collectionFocus.js</c>.
/// </para>
/// </remarks>
internal static class FocusRestore
{
    /// <summary>
    /// Focuses <paramref name="target"/>, swallowing every failure that means "there is no live
    /// element to focus". See the class remarks for when this one is used instead of the
    /// <see cref="Func{ValueTask}"/> overload.
    /// </summary>
    internal static Task FocusSafelyAsync(ElementReference target) =>
        global::FormCraft.FocusRestore.SafelyAsync(target);

    /// <summary>
    /// Focuses whatever <paramref name="focus"/> targets, swallowing every failure that means "there
    /// is no live element to focus" — the route to a <c>FluentButton</c> itself, via its <c>Id</c>
    /// and a small JS module. See the class remarks.
    /// </summary>
    internal static Task FocusSafelyAsync(Func<ValueTask> focus) =>
        global::FormCraft.FocusRestore.SafelyAsync(focus);
}
