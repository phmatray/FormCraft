namespace FormCraft;

/// <summary>
/// The single implementation of the native-required rule (#199), used by every UI adapter's field
/// component base.
/// <para>
/// #199 introduced it as a rule the component path and the imperative collection path both had to
/// apply identically — one implementation so <c>RenderPipelineParityTests</c> guarded against
/// regressions rather than against a copy-paste drifting. #203 then deleted the imperative path
/// outright, so the rule now reaches collection item fields the same way every other field
/// capability does: they render through the component. The type stays because the rule — an
/// explicit opt-in/out winning over <c>IsRequired</c> in <i>both</i> directions — is worth stating
/// and testing on its own.
/// </para>
/// <para>
/// It lived in <c>FormCraft.ForMudBlazor</c> and was <c>internal</c> until #279, at which point a
/// second adapter had already hand-copied the expression — the drift this doc comment claims to
/// prevent, happening for the reason the type could not be shared. It is now public and in core, so
/// there is one implementation again.
/// </para>
/// </summary>
public static class NativeRequired
{
    /// <summary>The attribute name carrying the explicit opt-in/opt-out.</summary>
    public const string AttributeName = "Required";

    /// <summary>
    /// Whether the UI framework's <c>Required</c> decoration should be set: the explicit
    /// <c>.WithNativeRequired(...)</c> attribute when the field sets one, otherwise the field's own
    /// <c>IsRequired</c>.
    /// </summary>
    /// <remarks>
    /// Presence is tested separately from value on purpose. Collapsing "not configured" and
    /// "configured false" into one fallback — which a plain get-with-default does — would make
    /// <c>.WithNativeRequired(false)</c> on a <c>.Required(...)</c> field silently re-acquire the
    /// decoration it was written to suppress. The explicit value has to win in both directions.
    /// </remarks>
    /// <param name="additionalAttributes">The field's configured additional attributes.</param>
    /// <param name="isRequired">The field's own required flag, used when nothing is configured.</param>
    /// <returns><c>true</c> when the field should carry the native required decoration.</returns>
    public static bool Resolve(IReadOnlyDictionary<string, object> additionalAttributes, bool isRequired)
        => additionalAttributes.TryGetValue(AttributeName, out var configured) && configured is bool optIn
            ? optIn
            : isRequired;
}
