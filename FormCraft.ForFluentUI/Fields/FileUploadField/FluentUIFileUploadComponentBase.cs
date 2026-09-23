namespace FormCraft.ForFluentUI;

/// <summary>
/// Shared behaviour for the single- and multiple-file upload components.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
/// <typeparam name="TValue">The field's value type.</typeparam>
/// <remarks>
/// <para>
/// ⛔ <b>A required file upload is NOT announced via <c>aria-required</c> here</b>, unlike every
/// other field type in this adapter. <c>FluentInputFile</c> renders an <c>&lt;input type="file"&gt;</c>
/// that is visually hidden behind a drop zone and reached through a separate button, so an ARIA
/// attribute on that input satisfies a DOM assertion while reaching no one who navigates by focus.
/// This mirrors the MudBlazor adapter's #262 finding rather than re-deriving it.
/// </para>
/// <para>
/// The requirement is carried on two channels a user actually meets instead: a visible <c>*</c> in
/// the field's own label, and an <c>aria-describedby</c> on the focusable browse control pointing at
/// a visually-hidden description. The rules live here, and the markup in
/// <c>FileUploadRequiredMarker</c>/<c>FileUploadRequiredHint</c>, so the two upload components
/// cannot drift - centralising only the values would still leave two copies of the markup.
/// </para>
/// </remarks>
public abstract class FluentUIFileUploadComponentBase<TModel, TValue> : FluentUIFieldComponentBase<TModel, TValue>
{

    /// <summary>
    /// Whether this field is marked required, by the same rule as every other field type: an
    /// explicit <c>"Required"</c> attribute wins over <c>IsRequired</c> in both directions.
    /// </summary>
    protected bool NativeRequiredValue => EffectiveNativeRequired;

    /// <summary>Whether the component renders a label of its own at all.</summary>
    protected bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    /// <summary>
    /// The accessible name for the hidden file input. Falls back rather than null-coalescing: a
    /// blank label is a configured value, so <c>Label ?? "File upload"</c> would leave the input
    /// with an empty accessible name instead of the fallback.
    /// </summary>
    protected string FileInputAccessibleName => HasLabel ? Label! : "File upload";

    /// <summary>The id of the requirement hint, unique per rendered field instance.</summary>
    protected string RequiredDescriptionId
    {
        get =>
        $"formcraft-{Context.Field.FieldName}-required-{field}";
    } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// The value for the browse control's <c>aria-describedby</c>: the hint's id when the field is
    /// required, otherwise <c>null</c>, which makes Blazor omit the attribute altogether.
    /// </summary>
    protected string? RequiredDescribedBy => NativeRequiredValue ? RequiredDescriptionId : null;

    /// <summary>
    /// The hint text. Falls back to label-free wording on purpose: the component renders its label
    /// only when one is configured, so a field with a blank label has the button description as its
    /// only remaining channel.
    /// </summary>
    protected string RequiredDescription =>
        HasLabel ? $"{Label} is required." : "This file upload is required.";

    // -------------------------------------------------------------------------------------------
    // Upload constraints.
    //
    // Resolved through UploadConstraintResolver (#340) - the same path the MudBlazor adapter's
    // upload components use - rather than read here a second way. Two independent readers is
    // exactly how this drifted in the first place: an earlier draft of this adapter read only the
    // raw attribute keys, which are also NOT the ones FileUploadConfiguration uses
    // (MaxFileSize/MaxFiles vs MaximumFileSize/MaximumFileCount), so every constraint configured
    // through the public builder API was silently ignored. The resolver additionally tolerates a
    // raw key boxed as `int` where these properties are `long`/`int?` - GetAttribute<T>'s
    // exact-type match would otherwise drop a hand-written `.WithAttribute("MaxFileSize", 5_000_000)`.
    // -------------------------------------------------------------------------------------------

    /// <summary>The configuration object the builder extensions write, when one was configured.</summary>
    private FileUploadConfiguration? UploadConfiguration =>
        UploadConstraintResolver.GetConfiguration(Context.Field.AdditionalAttributes);

    /// <summary>
    /// The <c>accept</c> list for the file input, as a comma-separated string, or <c>null</c> when
    /// every type is allowed.
    /// </summary>
    protected string? AcceptedFileTypes =>
        UploadConstraintResolver.ResolveAccept(UploadConfiguration, Context.Field.AdditionalAttributes);

    /// <summary>The largest accepted file in bytes. Defaults to 10 MB, matching MudBlazor.</summary>
    protected long MaximumFileSize =>
        UploadConstraintResolver.ResolveMaxFileSize(
            UploadConfiguration, Context.Field.AdditionalAttributes, "MaxFileSize", "MaximumFileSize")
        ?? 10L * 1024 * 1024;

    /// <summary>The most files that may be chosen at once.</summary>
    protected int MaximumFileCount =>
        UploadConstraintResolver.ResolveMaxFiles(
            UploadConfiguration, Context.Field.AdditionalAttributes, "MaxFiles", "MaximumFileCount")
        ?? 10;
}
