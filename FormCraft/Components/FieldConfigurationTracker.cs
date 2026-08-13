namespace FormCraft;

/// <summary>
/// Tracks which field a component has loaded its configuration from, so it can tell when it has been
/// handed a different one (#298).
/// </summary>
/// <remarks>
/// <para>
/// Blazor reuses a component instance whenever the render-tree shape matches, so an instance can be
/// re-parameterised with a different <c>Context</c> — a swapped <c>FormCraftComponent.Configuration</c>
/// is the everyday case. A component that read its attributes once in <c>OnInitialized</c> then keeps
/// rendering the previous field's mask, adornment and input type, silently and with output that looks
/// entirely plausible.
/// </para>
/// <para>
/// Comparison is by <b>reference</b>, and that is the whole mechanism. A built
/// <c>IFieldConfiguration</c> is immutable and handed out by reference, so "same object" answers
/// exactly the question being asked — <i>am I still showing the field I loaded?</i> — for the cost of
/// one comparison. That matters because the check runs on every parameter change, which for fields
/// bound with <c>Immediate="true"</c> means every keystroke; reloading unconditionally would repeat a
/// dictionary lookup and a type test per attribute, eight of them on the text field, per character.
/// #269 keys its compiled-getter cache on the same property.
/// </para>
/// <para>
/// A value-based key such as <c>FieldName</c> would be wrong rather than merely slower: two
/// collections can each hold a field called <c>Phone</c>, the collision #283's diagnostic key already
/// had to qualify around.
/// </para>
/// <para>
/// It lives in core, beside <see cref="FieldComponentBase{TModel, TValue}"/>, because every adapter
/// needs it and it references no UI type — one <see cref="object"/> field and a
/// <see cref="object.ReferenceEquals"/> call. #298 shipped it inside the MudBlazor package and had to
/// wire it in <i>three</i> places there (the field base, the file-upload base, and the boolean
/// component, which derives from <see cref="FieldComponentBase{TModel, TValue}"/> directly); #335
/// found the Fluent adapter needed the same again. Three wirings in one package was the signal that
/// this belongs a layer down — copying it per adapter is the duplication #279 spent a PR undoing.
/// </para>
/// </remarks>
internal sealed class FieldConfigurationTracker
{
    private object? _loadedField;

    /// <summary>
    /// Whether <paramref name="field"/> differs from the one last loaded — and records it if so.
    /// </summary>
    /// <remarks>
    /// Has a side effect by design: it is asked once per parameter change and answers "yes" exactly
    /// once per field, so the caller can treat a <c>true</c> as "reload now".
    /// </remarks>
    /// <param name="field">The field currently being rendered, or <c>null</c> before one arrives.</param>
    internal bool HasChanged(object? field)
    {
        if (ReferenceEquals(field, _loadedField))
        {
            return false;
        }

        _loadedField = field;

        return true;
    }
}
