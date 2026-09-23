using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Moves keyboard focus to a control, and never lets the attempt throw (#318).
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
/// #337 moved the swallow-safe catch list itself into <see cref="global::FormCraft.FocusRestore"/> in
/// core, once a second adapter (<c>FormCraft.ForFluentUI</c>) needed it too. This type is now a thin,
/// MudBlazor-typed wrapper over that shared call — behaviour-preserving, so every test written
/// against it keeps passing unmodified.
/// </para>
/// </remarks>
internal static class FocusRestore
{
    /// <summary>
    /// Focuses <paramref name="target"/> if there is one, swallowing every failure that means
    /// "there is no live element to focus".
    /// </summary>
    /// <param name="target">
    /// The control to focus. <see cref="MudBaseButton"/> rather than <c>MudButton</c> on purpose:
    /// the collection field's reorder and delete controls are <c>MudIconButton</c>, and a signature
    /// narrowed to <c>MudButton</c> would compile everywhere except where it is most needed.
    /// <see langword="null"/> is an ordinary case, not an error — a <c>@ref</c> is unassigned until a
    /// render completes, and a control behind an <c>@if</c> may never have rendered at all.
    /// </param>
    internal static Task FocusSafelyAsync(MudBaseButton? target) =>
        target is null ? Task.CompletedTask : global::FormCraft.FocusRestore.SafelyAsync(target.FocusAsync);

    /// <summary>
    /// Focuses a plain element, for the case where no button survives the action at all.
    /// </summary>
    /// <remarks>
    /// The collection field needs this: removing a row down to <c>MinItems</c> in a field that also
    /// forbids adding leaves the field with no focusable control, so the fallback target is the
    /// collection's own header — which carries the collection's label, so a screen reader says where
    /// the user has landed rather than going silent.
    /// </remarks>
    internal static Task FocusSafelyAsync(ElementReference target) =>
        global::FormCraft.FocusRestore.SafelyAsync(target);
}
