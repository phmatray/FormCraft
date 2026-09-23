// #383: FluentButton (Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.5-26219.1, pinned) renders
// as a native custom element (<fluent-button>) with no Blazor ElementReference capture of its own -
// confirmed by probing bUnit's rendered markup, which showed no static CSS class list either, only
// attributes such as appearance/disabled/id driven by the custom element itself. So the collection
// field's Add/Remove/Move up/Move down controls cannot be reached through Blazor's own
// ElementReference.FocusAsync() plumbing; this module reaches the real element directly by the DOM
// id FormCraft assigns it via FluentButton's own Id parameter, which already renders straight
// through to the element's id attribute.
export function focusById(id) {
    document.getElementById(id)?.focus();
}
