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
    /// Per-render-instance discriminator for <see cref="RequiredDescriptionId"/>.
    /// </summary>
    /// <remarks>
    /// The field name alone is not unique in a document: item fields render through these very
    /// components, so a collection emits one hint per row; two forms over one model on a page
    /// collide the same way; and two nested fields can share a member name. Duplicate ids are
    /// invalid HTML and, worse, point every later button at the first one's description.
    /// </remarks>
    private readonly string _instanceDiscriminator = Guid.NewGuid().ToString("N")[..8];

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
    protected string RequiredDescriptionId =>
        $"formcraft-{Context.Field.FieldName}-required-{_instanceDiscriminator}";

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
    // These read the FileUploadConfiguration object that .AsFileUpload(...) and
    // .AsMultipleFileUpload(...) actually write, falling back to the raw attribute keys the
    // MudBlazor components accept. Reading only the raw keys - which an earlier draft of this
    // adapter did - meant every constraint configured through the public builder API was silently
    // ignored: .AsFileUpload(acceptedFileTypes: [".pdf"], maxFileSize: 2_000_000) rendered an
    // upload that took any file up to the default cap, with no error. The keys are also NOT the
    // ones the object uses (MaxFileSize/MaxFiles vs MaximumFileSize/MaximumFileCount), so guessing
    // from the MudBlazor component's fallbacks alone reproduces the same silence.
    //
    // Centralised here rather than in each component for the reason this base class exists: two
    // copies of a rule drift, and a constraint that applies to single uploads but not multiple
    // ones is exactly the kind of divergence nobody notices until a user uploads a 500 MB file.
    // -------------------------------------------------------------------------------------------

    /// <summary>The configuration object the builder extensions write, when one was configured.</summary>
    private FileUploadConfiguration? UploadConfiguration =>
        GetAttribute<FileUploadConfiguration>("FileUploadConfiguration");

    /// <summary>
    /// The <c>accept</c> list for the file input, as a comma-separated string, or <c>null</c> when
    /// every type is allowed.
    /// </summary>
    protected string? AcceptedFileTypes
    {
        get
        {
            var configured = UploadConfiguration?.AcceptedFileTypes;
            if (configured is { Length: > 0 })
            {
                return string.Join(",", configured);
            }

            return GetAttribute<string>("Accept");
        }
    }

    /// <summary>The largest accepted file in bytes. Defaults to 10 MB, matching MudBlazor.</summary>
    protected long MaximumFileSize =>
        UploadConfiguration?.MaxFileSize
        ?? GetAttribute("MaxFileSize", GetAttribute("MaximumFileSize", 10L * 1024 * 1024));

    /// <summary>The most files that may be chosen at once.</summary>
    protected int MaximumFileCount =>
        UploadConfiguration?.MaxFiles
        ?? GetAttribute("MaxFiles", GetAttribute("MaximumFileCount", 10));
}
