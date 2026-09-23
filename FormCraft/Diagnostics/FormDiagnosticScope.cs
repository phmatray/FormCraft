namespace FormCraft.Diagnostics;

/// <summary>
/// The form-wide diagnostic latch: reports each (diagnostic, field) pair once per <b>form</b>, however
/// many times the field's component is destroyed and re-created (#304).
/// </summary>
/// <remarks>
/// <para>
/// Originally <c>FormCraft.ForMudBlazor.FormDiagnosticScope</c> (#304). Moved into core under #398 so
/// <c>FormCraft.ForFluentUI</c>'s once-per-field custom-template warning (#330) could share this
/// latch instead of a local, unguarded <c>HashSet&lt;string&gt;</c> — the same "shared collaborator,
/// not a copy" precedent #279 set for <c>AdapterRegistration</c>/<c>NativeRequired</c> and #321
/// applied to the security pipeline. Purely a move: the keying and behaviour are unchanged.
/// </para>
/// <para>
/// <b>Why a component-level flag cannot do this job.</b> A latch that lives on the component instance
/// is gone the moment that instance is — and a re-mount (e.g. a field gated by
/// <c>.VisibleWhen(...)</c> toggling) is by definition a new instance with fresh fields. This outlives
/// the field's component because the <i>form</i> owns it.
/// </para>
/// <para>
/// ⚠️ <b>It does not outlive the form.</b> This is a field on the form container component, so
/// anything that destroys the form component takes it along — including a wizard that puts a separate
/// form in each step, if the step host unmounts inactive content. What is fixed here is re-mounting a
/// <i>field</i> within one form; a re-mounted <i>form</i> starts over, by construction.
/// </para>
/// <para>
/// <b>Scoped to one form deliberately.</b> Two forms over the same model each get their own instance,
/// so each reports — they are separate forms, and suppressing the second one's warning because the
/// first already spoke would hide a real diagnostic from whoever is looking at the second.
/// </para>
/// </remarks>
public sealed class FormDiagnosticScope
{
    private readonly HashSet<string> _warnedOnce = [];

    /// <summary>
    /// Returns <c>true</c> the first time a given (diagnostic, field) pair is presented to this form,
    /// and <c>false</c> forever after.
    /// </summary>
    /// <remarks>
    /// ⛔ This mutates: consulting it burns the latch. Call it only once the diagnostic's rule has
    /// already said yes, or a field with nothing to report spends the one warning it was owed (#274).
    /// </remarks>
    /// <param name="category">The diagnostic's logger category.</param>
    /// <param name="key">
    /// The field identity to latch on. The category is part of the key, not decoration — a single
    /// field can legitimately trip several diagnostics, and latching them together would report only
    /// the first and hide the rest for good (#274).
    /// </param>
    public bool ShouldWarnOnce(string category, string key) => _warnedOnce.Add($"{category}|{key}");
}
