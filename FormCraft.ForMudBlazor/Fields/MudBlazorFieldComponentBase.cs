using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Names of the cascading values supplied by <see cref="FormCraftComponent{TModel}"/>
/// to the MudBlazor field components it renders.
/// </summary>
public static class FormCraftCascadingValues
{
    /// <summary>
    /// Name of the cascading <see cref="Variant"/> that provides the form-level
    /// default input variant. Individual fields override it via
    /// <c>.WithVariant(...)</c> (the "Variant" additional attribute).
    /// </summary>
    public const string DefaultVariant = "FormCraftDefaultVariant";

    /// <summary>
    /// Name of the cascading <see cref="bool"/> that provides the form-level default
    /// for MudBlazor's <c>ShrinkLabel</c>. Individual fields override it via
    /// <c>.WithShrinkLabel(...)</c> (the "ShrinkLabel" additional attribute).
    /// </summary>
    public const string DefaultShrinkLabel = "FormCraftDefaultShrinkLabel";

    /// <summary>
    /// Name of the cascading <see cref="ShrinkLabelDiagnosticCollector"/> that gathers
    /// ShrinkLabel conflicts so the form can report them in a single warning (#181).
    /// </summary>
    public const string ShrinkLabelDiagnostics = "FormCraftShrinkLabelDiagnostics";

    /// <summary>
    /// Name of the cascading <see cref="CollectionItemFieldScope"/> supplied by
    /// <c>CollectionFieldComponent</c> to the item fields it renders (#203). Absent — and therefore
    /// null — for an ordinary field, which is how a component tells the two apart.
    /// </summary>
    public const string ItemFieldScope = "FormCraftItemFieldScope";
}

/// <summary>
/// The nested context an item field renders in: which collection owns it, and the once-per-field
/// latch its diagnostics need.
/// </summary>
/// <remarks>
/// <para>
/// Introduced by #203, when collection item fields stopped being rendered by a hand-rolled
/// <c>RenderTreeBuilder</c> path and started going through <see cref="IFieldRendererService"/> like
/// every other field. That convergence is what this type pays for: the per-type components are now
/// shared, so the two things the old path knew and a component does not — <i>which collection am I
/// in</i> and <i>has this field already warned</i> — have to reach them some other way.
/// </para>
/// <para>
/// Cascaded rather than passed as a parameter because it must reach every field component the
/// service may select, including ones that do not exist yet. A field rendered outside a collection
/// sees no scope at all.
/// </para>
/// </remarks>
public sealed class CollectionItemFieldScope
{
    private readonly HashSet<string> _warnedOnce = [];

    /// <summary>
    /// Creates a scope for the collection field named <paramref name="collectionName"/>.
    /// </summary>
    /// <param name="collectionName">The owning collection field's name, e.g. <c>Items</c>.</param>
    public CollectionItemFieldScope(string collectionName) => CollectionName = collectionName;

    /// <summary>Gets the owning collection field's name.</summary>
    public string CollectionName { get; }

    /// <summary>
    /// The identity a field inside this collection is reported under by the form-wide diagnostic
    /// collectors: <c>&lt;collection&gt;[].&lt;field&gt;</c> (#213).
    /// </summary>
    /// <remarks>
    /// Row-agnostic on purpose — the <c>[]</c> carries no index. These diagnostics describe a
    /// field's <i>configuration</i>, so they are emitted once per field however many rows exist;
    /// keying them to whichever row rendered first would read as though row 0 were special.
    /// <para>
    /// The qualification is load-bearing: <c>ShrinkLabelDiagnosticCollector</c> is form-wide and
    /// keys by field identity, but a bare field name is unique only inside one item form — so a
    /// top-level "Name" and an item "Name" overwrote each other, and so did item fields of the same
    /// name in two different collections.
    /// </para>
    /// </remarks>
    /// <param name="fieldName">The item field's own name.</param>
    public string DiagnosticKey(string fieldName) => $"{CollectionName}[].{fieldName}";

    /// <summary>
    /// Returns <c>true</c> the first time a given (diagnostic, field) pair is presented, and
    /// <c>false</c> forever after.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A collection renders one component instance <i>per row</i>, and a diagnostic that fires from
    /// <c>OnInitialized</c> therefore fires once per row: a 50-item collection would emit 50
    /// identical warnings about a single field's configuration. The hand-rolled path latched
    /// exactly this way before #203; the latch simply moved here when the render did.
    /// </para>
    /// <para>
    /// ⛔ <paramref name="category"/> is part of the key, not decoration. The code this replaced kept
    /// <i>two</i> separate <c>HashSet</c>s — <c>_warnedItemFields</c> and
    /// <c>_maskedLinesWarnedFields</c> — with an explicit note that "a shared latch would let
    /// whichever fired first silence the other on the same field". A single field can legitimately
    /// trip several diagnostics (a masked multi-line password whose adornment is displaced trips
    /// two), and latching them together would report only the first and hide the rest for good.
    /// </para>
    /// <para>
    /// Needed by the diagnostics that log directly. The ShrinkLabel one usually reports to a
    /// collector that already dedupes by key — but only when a collector exists, which it does not
    /// for a collection rendered outside a <c>FormCraftComponent</c>, so that fallback latches here
    /// too.
    /// </para>
    /// </remarks>
    /// <param name="category">
    /// The diagnostic's logger category, e.g. <see cref="MaskedLinesDiagnostic.Category"/>.
    /// </param>
    /// <param name="key">The field identity to latch on, normally <see cref="DiagnosticKey"/>.</param>
    public bool ShouldWarnOnce(string category, string key) => _warnedOnce.Add($"{category}|{key}");
}

/// <summary>
/// Base class for MudBlazor field components. Extends the framework-agnostic
/// <see cref="FieldComponentBase{TModel, TValue}"/> with MudBlazor-specific
/// presentation concerns such as the configurable input <see cref="Variant"/>.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
/// <typeparam name="TValue">The type of the field value.</typeparam>
public abstract class MudBlazorFieldComponentBase<TModel, TValue> : FieldComponentBase<TModel, TValue>
{
    /// <summary>
    /// Gets or sets the form-level default variant cascaded by
    /// <see cref="FormCraftComponent{TModel}"/>. Used as a fallback when the field
    /// does not configure its own "Variant" additional attribute.
    /// </summary>
    [CascadingParameter(Name = FormCraftCascadingValues.DefaultVariant)]
    public Variant? FormDefaultVariant { get; set; }

    /// <summary>
    /// Gets or sets the form-level default <c>ShrinkLabel</c> cascaded by
    /// <see cref="FormCraftComponent{TModel}"/>. Used as a fallback when the field does
    /// not configure its own "ShrinkLabel" additional attribute.
    /// </summary>
    [CascadingParameter(Name = FormCraftCascadingValues.DefaultShrinkLabel)]
    public bool? FormDefaultShrinkLabel { get; set; }

    /// <summary>
    /// Gets the variant to apply to the MudBlazor input: the field-level "Variant"
    /// additional attribute (set via <c>.WithVariant(...)</c>) when present, otherwise
    /// the cascaded form-level default, otherwise <see cref="Variant.Outlined"/>.
    /// </summary>
    protected Variant EffectiveVariant =>
        GetAttribute<Variant?>("Variant") ?? FormDefaultVariant ?? Variant.Outlined;

    /// <summary>
    /// Gets whether the MudBlazor input's label should stay in its shrunk position: the
    /// field-level "ShrinkLabel" additional attribute (set via
    /// <c>.WithShrinkLabel(...)</c>) when present, otherwise the cascaded form-level
    /// default, otherwise <c>true</c>.
    /// </summary>
    /// <remarks>
    /// The <c>true</c> fallback preserves the rendering FormCraft has always produced,
    /// which suits <see cref="Variant.Outlined"/> and <see cref="Variant.Filled"/>.
    /// <see cref="Variant.Text"/> usually wants <c>false</c> so the label floats from
    /// inside the input on focus, since there is no border to anchor a shrunk label to.
    /// </remarks>
    protected bool EffectiveShrinkLabel =>
        GetAttribute<bool?>("ShrinkLabel") ?? FormDefaultShrinkLabel ?? true;

    /// <summary>
    /// Gets the adornment position to apply to the MudBlazor input: the field-level "Adornment"
    /// additional attribute when present, otherwise <see cref="Adornment.None"/>. There is no
    /// form-level default for adornments.
    /// </summary>
    /// <remarks>
    /// Only components whose MudBlazor input defaults to <see cref="Adornment.None"/> may bind this
    /// unconditionally — MudTextField and MudNumericField do. MudDatePicker defaults to
    /// <see cref="Adornment.End"/> with a calendar icon that binding an unset adornment would erase,
    /// which is why the date components deliberately take no part (#184, #191).
    /// <para>
    /// Named <c>Effective*</c> rather than <c>Adornment*</c> both to match
    /// <see cref="EffectiveVariant"/> and because <c>MudBlazorLovFieldComponent</c> already declares
    /// its own <c>AdornmentIcon</c>; a base member of that name would hide it (CS0108), which this
    /// repo compiles as an error.
    /// </para>
    /// </remarks>
    protected Adornment EffectiveAdornment =>
        GetAttribute<Adornment?>("Adornment") ?? Adornment.None;

    /// <summary>
    /// Gets the adornment icon for the MudBlazor input, or null when none is configured.
    /// </summary>
    protected string? EffectiveAdornmentIcon => GetAttribute<string?>("AdornmentIcon");

    /// <summary>
    /// Gets the adornment colour for the MudBlazor input, defaulting to <see cref="Color.Default"/>.
    /// </summary>
    /// <remarks>
    /// Resolved independently of the position and icon: <c>WithAdornment(...)</c> writes all three
    /// together, but a field that set only "Adornment" through raw <c>WithAttribute(...)</c> has no
    /// colour to read, so this must supply one rather than assume the trio is present.
    /// </remarks>
    protected Color EffectiveAdornmentColor => GetAttribute("AdornmentColor", Color.Default);

    /// <summary>
    /// The adornment this component actually <b>renders</b>, or <c>null</c> when it renders none of
    /// ours. Defaults to <c>null</c>; only components that bind an adornment override it (#212).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ShrinkLabel diagnostic judges this rather than <c>GetAttribute&lt;Adornment?&gt;("Adornment")</c>.
    /// Reading the configured value made date, select, autocomplete, lookup and file-upload fields
    /// warn about an adornment they never draw — telling the developer to remove a
    /// <c>ShrinkLabel=false</c> that was being honoured. A diagnostic that is wrong on most field
    /// types trains people to ignore the channel, which costs more than the warning is worth.
    /// </para>
    /// <para>
    /// This is the component-path half of the rule the collection path already states: *"the
    /// diagnostic has to judge what this path actually RENDERS, not what was configured"* (#183).
    /// There it is a per-call-site <c>rendersAdornment</c> flag; here it is this override.
    /// </para>
    /// <para>
    /// <c>null</c> and <see cref="Adornment.None"/> are equally harmless —
    /// <c>ShrinkLabelDiagnostic.Conflict</c> reacts only to <see cref="Adornment.Start"/>, since only
    /// a start adornment sits where a floating label would go.
    /// </para>
    /// <para>
    /// ⛔ Defaulting to <c>null</c> is deliberate: a newly added component type is silent until it
    /// opts in. Warning wrongly is the defect this exists to fix, so the safe default is quiet.
    /// </para>
    /// </remarks>
    protected virtual Adornment? RenderedAdornment => null;

    /// <summary>
    /// Whether MudBlazor's native required decoration is rendered: the explicit
    /// <c>.WithNativeRequired(...)</c> opt-in (or the equivalent raw <c>"Required"</c> attribute)
    /// when the field sets one, otherwise <c>Context.Field.IsRequired</c> (#199, #204).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The explicit attribute wins in <b>both</b> directions — a field that asked for the decoration
    /// without <c>.Required(...)</c> gets it, and one that suppressed it with
    /// <c>.WithNativeRequired(false)</c> keeps it suppressed even when <c>.Required(...)</c> is
    /// configured. Only an unconfigured field falls through to the validator's own answer.
    /// </para>
    /// <para>
    /// ⚠️ This property used to read the attribute and <b>never</b> <c>IsRequired</c>, because #190
    /// had removed that forward. #199 restores it deliberately: a required field that is not
    /// announced as required fails WCAG 2.1 <b>3.3.2</b> (Level A), and on MudBlazor 9.8.0 there is
    /// no way to say so without this flag. <c>MudInput</c> splats <c>UserAttributes</c> and then
    /// writes its own <c>required</c> and <c>aria-required</c> afterwards, both off this single
    /// bool; Blazor resolves duplicate attributes last-write-wins, so a caller-supplied
    /// <c>aria-required</c> is always overwritten and the two attributes cannot be separated.
    /// </para>
    /// <para>
    /// What #190 actually fixed — the same <c>.Required("…")</c> call decorating an item field but
    /// not an ordinary one — stays fixed: <c>CollectionFieldComponent</c> resolves the flag by the
    /// same rule, so the two render paths agree. The HTML5 attribute that returns with it is inert
    /// for validation here, since FormCraft forms render <c>novalidate</c> (#206).
    /// </para>
    /// </remarks>
    protected bool EffectiveNativeRequired =>
        NativeRequired.Resolve(Context.Field.AdditionalAttributes, IsRequired);

    /// <summary>
    /// Service provider used to resolve an optional <see cref="ILoggerFactory"/> for the
    /// ShrinkLabel diagnostic. Diagnostics degrade silently when no logger is registered.
    /// </summary>
    /// <remarks>
    /// Private on purpose: derived components inject their own <c>ServiceProvider</c>
    /// (MudBlazorLovFieldComponent does), and a protected member of the same name would hide
    /// theirs (CS0108). Blazor injects non-public properties, so privacy costs nothing here.
    /// </remarks>
    [Inject]
    private IServiceProvider? DiagnosticServices { get; set; }

    /// <summary>
    /// The same provider, exposed to derived components that emit a diagnostic of their own
    /// (<see cref="MaskedLinesDiagnostic"/>, #207).
    /// </summary>
    /// <remarks>
    /// Deliberately named differently from the injected property above rather than simply making
    /// that one protected: derived components inject their own <c>ServiceProvider</c>
    /// (<c>MudBlazorLovFieldComponent</c> does), and a protected member sharing a name with theirs
    /// would hide it (CS0108) — which under <c>TreatWarningsAsErrors</c> is a build break.
    /// </remarks>
    protected IServiceProvider? DiagnosticServiceProvider => DiagnosticServices;

    /// <summary>
    /// The form's diagnostic collector, when this field is rendered inside a
    /// <see cref="FormCraftComponent{TModel}"/>. Null for a standalone field.
    /// </summary>
    [CascadingParameter(Name = FormCraftCascadingValues.ShrinkLabelDiagnostics)]
    public ShrinkLabelDiagnosticCollector? ShrinkLabelDiagnostics { get; set; }

    /// <summary>
    /// The collection this field is an item field of, or <c>null</c> when it is an ordinary field
    /// (#203).
    /// </summary>
    [CascadingParameter(Name = FormCraftCascadingValues.ItemFieldScope)]
    public CollectionItemFieldScope? ItemFieldScope { get; set; }

    /// <summary>
    /// The identity this field is reported under by the form-wide diagnostic collectors: its bare
    /// field name, or <c>&lt;collection&gt;[].&lt;field&gt;</c> when it is an item field (#213).
    /// </summary>
    /// <remarks>
    /// Deliberately distinct from <c>Context.Field.FieldName</c>, which is unique only within one
    /// item form. Two collections with same-named item fields — or a top-level field colliding with
    /// an item field — must be counted as the separate fields they are.
    /// </remarks>
    protected string DiagnosticFieldKey =>
        ItemFieldScope?.DiagnosticKey(Context.Field.FieldName) ?? Context.Field.FieldName;

    /// <summary>
    /// Whether this component should report the given diagnostic for this field, consuming the
    /// once-per-field latch when there is one (#284).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No scope means report.</b> The latch exists because a collection renders one component
    /// instance <i>per row</i>, so a diagnostic emitted from <c>OnInitialized</c> would fire fifty
    /// times about one field's configuration in a fifty-item collection. Outside a collection there
    /// is no scope, nothing to de-duplicate, and the field must report — the common case, and why
    /// the default is <c>true</c>. Inverting it silences the diagnostics for ordinary fields while
    /// leaving them working inside collections, so the failure hides in the case least likely to be
    /// tested. Pinned by <c>DiagnosticLatchTests</c>.
    /// </para>
    /// <para>
    /// <b>The category is part of the key.</b> One field can legitimately trip several diagnostics —
    /// a masked multi-line password whose adornment is displaced trips two — and a key without the
    /// category would let whichever fired first silence the rest for good. See
    /// <see cref="CollectionItemFieldScope.ShouldWarnOnce"/>, which keeps that note at the other end.
    /// </para>
    /// <para>
    /// ⛔ <b>Call this only once the diagnostic's rule has already said yes.</b> It has a side
    /// effect: consulting it burns the latch. Asking on a row that had nothing to report would spend
    /// the one warning a later row was entitled to, which would make whether a field is reported at
    /// all depend on row order (#274).
    /// </para>
    /// </remarks>
    /// <param name="category">
    /// The diagnostic's logger category, e.g. <see cref="MaskedLinesDiagnostic.Category"/>.
    /// </param>
    protected bool ShouldReport(string category) =>
        ItemFieldScope?.ShouldWarnOnce(category, DiagnosticFieldKey) ?? true;

    private bool _shrinkLabelDiagnosticEmitted;

    /// <summary>
    /// When true, this component never reports a ShrinkLabel conflict. Override in components
    /// whose label is structurally always pinned, where the warning would be unactionable.
    /// </summary>
    protected virtual bool SuppressShrinkLabelDiagnostic => false;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        EmitShrinkLabelDiagnosticIfNeeded();
    }

    /// <summary>
    /// Warns when the field asked for a floating label that MudBlazor will not give it.
    /// <para>
    /// MudInput decides the shrunk state by OR-ing <c>ShrinkLabel</c> with "has a value",
    /// "has a placeholder" and "has a start adornment", so <c>ShrinkLabel=false</c> is only
    /// observable on an empty field with neither. Rendering is untouched — this only tells the
    /// developer why their setting appears to do nothing.
    /// </para>
    /// </summary>
    private void EmitShrinkLabelDiagnosticIfNeeded()
    {
        // Once per component instance: the conflict is a configuration fact, so re-reporting it
        // on every parameter change would flood the console as the user types.
        if (_shrinkLabelDiagnosticEmitted || EffectiveShrinkLabel || SuppressShrinkLabelDiagnostic)
        {
            return;
        }

        var conflict = ShrinkLabelConflict();
        if (conflict is null)
        {
            return;
        }

        _shrinkLabelDiagnosticEmitted = true;

        // The collector is form-wide and keys by identity, so an item field reports under its
        // qualified key rather than its bare name (#213).
        var fieldName = DiagnosticFieldKey;
        var field = Label ?? Context.Field.FieldName;

        // Inside a FormCraftComponent, report to the form's collector so all conflicting fields
        // arrive in one warning. Rendered standalone there is no collector, so log directly
        // rather than lose the diagnostic entirely.
        if (ShrinkLabelDiagnostics is not null)
        {
            ShrinkLabelDiagnostics.Report(fieldName, Label, conflict);
            return;
        }

        // No collector to dedupe for us on this branch, so the scope's latch has to. A collection
        // rendered outside a FormCraftComponent — CollectionFieldRenderer.Render is public API — has
        // one component instance per row and _shrinkLabelDiagnosticEmitted is per INSTANCE, so an
        // unlatched fallback would log N identical warnings for one field's configuration. The
        // hand-rolled path latched this per field before #203.
        //
        // ShouldReport latches on DiagnosticFieldKey, which is what `fieldName` above already holds.
        if (!ShouldReport(ShrinkLabelDiagnostic.Category))
        {
            return;
        }

        DiagnosticLog.Warn(
            DiagnosticServices,
            ShrinkLabelDiagnostic.Category,
            "Field '{Field}' sets ShrinkLabel=false but also has {Conflict}, which MudBlazor " +
            "lets win — the label stays pinned and will not float. Remove that property to get " +
            "a floating label, or drop ShrinkLabel=false.",
            field,
            conflict);
    }

    /// <summary>
    /// Returns the property that will override <c>ShrinkLabel=false</c> for this field, or
    /// null when the setting will be honoured.
    /// </summary>
    private string? ShrinkLabelConflict() =>
        ShrinkLabelDiagnostic.Conflict(Placeholder, RenderedAdornment);
}

/// <summary>
/// The single implementation of the ShrinkLabel conflict rule, used by
/// <see cref="MudBlazorFieldComponentBase{TModel, TValue}"/>.
/// </summary>
/// <remarks>
/// Extracted when a second, imperative render path existed for collection item fields and the rule
/// had to be applied identically by both. #203 deleted that path; the rule keeps its own type
/// because it is the part worth stating and testing independently of any component.
/// </remarks>
internal static class ShrinkLabelDiagnostic
{
    /// <summary>Logger category for the ShrinkLabel diagnostic.</summary>
    internal const string Category = "FormCraft.ForMudBlazor.ShrinkLabel";

    /// <summary>
    /// Names the property that will override <c>ShrinkLabel=false</c>, or returns null when
    /// nothing does and the setting will be honoured.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT consider "the field has a value". A populated field must shrink its
    /// label or the two overlap, so that override is correct behaviour rather than a surprise —
    /// warning about it would be noise on every filled form.
    /// </remarks>
    internal static string? Conflict(string? placeholder, Adornment? adornment)
    {
        if (!string.IsNullOrWhiteSpace(placeholder))
        {
            return "a Placeholder";
        }

        // Only a START adornment sits where a floating label would go; End is harmless.
        return adornment == Adornment.Start ? "a start Adornment" : null;
    }
}
