using Microsoft.AspNetCore.Components;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Moves keyboard focus to a plain wrapping element, and never lets the attempt throw (#337).
/// </summary>
/// <remarks>
/// <para>
/// A thin, Fluent-typed wrapper over <see cref="global::FormCraft.FocusRestore"/> — the swallow-safe
/// catch list lives there, once, shared with <c>FormCraft.ForMudBlazor</c>'s own wrapper (#318).
/// </para>
/// <para>
/// <b>Why this only ever takes an <see cref="ElementReference"/>.</b> MudBlazor's equivalent wrapper
/// forwards a control's own <c>FocusAsync()</c>. Fluent UI Blazor's <c>FluentButton</c> (the pinned
/// v5 RC, <c>Microsoft.FluentUI.AspNetCore.Components</c> 5.0.0-rc.5-26219.1) has no such method and
/// no public <see cref="ElementReference"/> of its own — confirmed by decompiling the installed
/// assembly: <c>FluentButton</c> does not implement <c>IFluentComponentElementBase</c> (the interface
/// several other Fluent inputs use for exactly this), and its render tree never calls
/// <c>AddElementReferenceCapture</c>. So every focus target in the Fluent collection field is a plain
/// HTML wrapper element carrying its own <c>@ref</c> — matching the issue's own anticipated fallback
/// ("confirm before relying on it; fall back to the <c>ElementReference</c> overload for every
/// target") — rather than the button itself.
/// </para>
/// </remarks>
internal static class FocusRestore
{
    /// <summary>
    /// Focuses <paramref name="target"/>, swallowing every failure that means "there is no live
    /// element to focus". See the class remarks for why this is the only overload Fluent needs.
    /// </summary>
    internal static Task FocusSafelyAsync(ElementReference target) =>
        global::FormCraft.FocusRestore.SafelyAsync(target);
}
