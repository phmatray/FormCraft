using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Shared base for the single- and multiple-file upload components, holding the required-marking
/// rule the two have to apply identically (#262), and the constraint resolution both consume so
/// they cannot each pick their own reading of <c>FileUploadConfiguration</c> (#340).
/// </summary>
/// <remarks>
/// <para>
/// The two upload components drifting apart is the failure class this library keeps re-filing
/// (#146, #177, #184, #189), so the rule lives here once rather than as a copy in each component.
/// The <i>markup</i> lives once too — see <c>FileUploadRequiredMarker</c> and
/// <c>FileUploadRequiredHint</c> — because centralising only the values still leaves two copies of
/// the thing that can drift.
/// </para>
/// <para>
/// <b>Why not the hidden input.</b> Both components render <c>MudFileUpload</c>'s real
/// <c>&lt;input type="file"&gt;</c> hidden, with <c>tabindex="-1"</c>, beneath a custom
/// drop zone, deliberately out of the tab order. Binding <c>Required</c> there would emit
/// <c>aria-required</c> on an element no keyboard or screen-reader user ever reaches — the
/// "forwarded but inert" failure this library's parity tests exist to catch — and it is measurably
/// harmful besides (see <c>MudBlazorFileUploadFieldComponent</c>). So the requirement is identified
/// where the user actually is, on two channels: visibly in the field's own <c>&lt;MudText&gt;</c>
/// label, and programmatically via <c>aria-describedby</c> on the <c>MudButton</c> that takes focus.
/// </para>
/// </remarks>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
/// <typeparam name="TValue">The field's value type — one file, or a list of them.</typeparam>
public abstract class MudBlazorFileUploadComponentBase<TModel, TValue> : FieldComponentBase<TModel, TValue>
{

    /// <summary>
    /// Whether this field is marked as required, resolved by the same rule as every other field
    /// type: an explicit <c>.WithNativeRequired(...)</c> wins over <c>IsRequired</c> in both
    /// directions (#199).
    /// </summary>
    protected bool NativeRequiredValue =>
        NativeRequired.Resolve(Context.Field.AdditionalAttributes, IsRequired);

    /// <summary>
    /// The <see cref="FileUploadConfiguration"/> <c>.AsFileUpload</c>/<c>.AsMultipleFileUpload</c>
    /// write, resolved once here so both upload components read constraints through the same path
    /// (#340) instead of each picking its own keys.
    /// </summary>
    protected FileUploadConfiguration? UploadConfiguration =>
        UploadConstraintResolver.GetConfiguration(Context.Field.AdditionalAttributes);

    /// <summary>
    /// Whether the component renders its own <c>&lt;MudText&gt;</c> label at all.
    /// </summary>
    /// <remarks>
    /// One predicate, used by both the label gate and <see cref="RequiredDescription"/>. They used
    /// to disagree — <c>IsNullOrEmpty</c> in the markup, <c>IsNullOrWhiteSpace</c> in the
    /// description — so a whitespace-only label rendered a bare asterisk with nothing beside it
    /// while the description simultaneously claimed the field had no label.
    /// </remarks>
    protected bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    /// <summary>
    /// The accessible name for the hidden <c>&lt;input type="file"&gt;</c>. Falls back rather than
    /// null-coalescing: a blank label is a configured value, so <c>Label ?? "File upload"</c> would
    /// leave the input with an empty accessible name instead of the fallback.
    /// </summary>
    protected string FileInputAccessibleName => HasLabel ? Label! : "File upload";

    /// <summary>
    /// The id of the requirement hint, unique per rendered field instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>field</c> is this property's own backing store (C# 14), initialised by the trailing
    /// initializer to a short GUID <b>once per component instance</b> — that is the whole mechanism,
    /// so do not "simplify" it to a constant or a shared static.
    /// </para>
    /// <para>
    /// The field name alone is NOT unique in a document. Item fields render through these very
    /// components since #203, so a required upload inside <c>.WithItemForm(...)</c> emits one hint
    /// per row; two forms over the same model on one page collide the same way; and two nested
    /// fields can share a member name (<c>x =&gt; x.Passport.Scan</c> and <c>x =&gt; x.Visa.Scan</c>).
    /// Duplicate ids are invalid HTML and, worse, point every later button at the first row's
    /// description. MudBlazor solves this the same way, with a per-component identifier.
    /// </para>
    /// <para>
    /// This was an explicit <c>_instanceDiscriminator</c> field until #301, when <c>IDE0032</c> —
    /// newly reachable now that the format gate is enforced — folded it into the property. The
    /// rewrite is value-identical; it deleted this explanation, which is why the explanation is
    /// back.
    /// </para>
    /// </remarks>
    protected string RequiredDescriptionId
    {
        get => $"formcraft-{IdSanitizer.ToCssSafeId(Context.Field.FieldName)}-required-{field}";
    } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// The value for the focusable button's <c>aria-describedby</c>: the hint's id when the field is
    /// required, otherwise <c>null</c>, which makes Blazor omit the attribute altogether.
    /// </summary>
    protected string? RequiredDescribedBy => NativeRequiredValue ? RequiredDescriptionId : null;

    /// <summary>
    /// The hint text. Falls back to a label-free wording on purpose: the component renders its
    /// <c>&lt;MudText&gt;</c> label only when one is configured, so a field with a blank label has
    /// the button description as its only remaining channel.
    /// </summary>
    protected string RequiredDescription =>
        HasLabel ? $"{Label} is required." : "This file upload is required.";

    /// <summary>
    /// The field's <b>Browse</b> button, captured by <c>@ref</c> in both upload components.
    /// </summary>
    /// <remarks>
    /// Shared here rather than declared twice for the reason the rest of this class exists: the
    /// single- and multiple-file components drifting apart is the failure class this library keeps
    /// re-filing (#146, #177, #184, #189).
    /// </remarks>
    protected MudButton? BrowseButton { get; set; }

    /// <summary>
    /// Moves keyboard focus to this field's <b>Browse</b> button (#281).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this is needed at all.</b> Both components gate their <b>Clear</b> button on an
    /// <c>@if</c> over the very value the button's own handler removes, so activating Clear unmounts
    /// the element the keyboard user is standing on and focus falls to <c>&lt;body&gt;</c> — the
    /// next <kbd>Tab</kbd> restarts from the top of the document. WCAG 2.1 <b>2.4.3 Focus Order</b>
    /// (Level A) expects focus to move in an order that preserves meaning and operability.
    /// </para>
    /// <para>
    /// <b>Why Browse specifically.</b> It is the affordance that resolves the state the user has
    /// just created, it sits where Clear used to be in the tab order, and it carries the
    /// <c>aria-describedby</c> requirement description from #262 — so focusing it announces the
    /// requirement at the exact moment clearing makes the field unsatisfied.
    /// </para>
    /// <para>
    /// <b>Failures are swallowed, and the null case is ordinary</b> — both handled by
    /// <see cref="FocusRestore.FocusSafelyAsync(MudBaseButton?)"/>, which is shared with every other control that
    /// unmounts or disables itself (#318). The reasoning for the wide catch list, and the standing
    /// ⛔ against narrowing it, live there rather than being restated per caller. Note the null
    /// check does <b>not</b> stand in for the prerender guard: component reference captures are
    /// assigned while the render batch is applied, so <c>@ref</c> is set on the server pass too.
    /// </para>
    /// </remarks>
    protected Task FocusBrowseAsync() => FocusRestore.FocusSafelyAsync(BrowseButton);

    /// <summary>
    /// An empty <c>MudFileUpload&lt;T&gt;.SelectedTemplate</c> — passed by both upload components to
    /// suppress MudBlazor's own built-in file list (#338).
    /// </summary>
    /// <remarks>
    /// <c>MudFileUpload</c> renders its built-in chip list <i>in addition to</i> the
    /// <c>CustomContent</c> drop zone FormCraft supplies for exactly one reason: <c>CustomContent</c>
    /// replaces the drop target, not the file list, and the two are gated independently
    /// (<c>if (SelectedTemplate != null)</c> in MudBlazor 9.10.0's own render tree — measured by
    /// decompiling <c>MudFileUpload.razor.cs</c>, not assumed). Any <b>non-null</b>
    /// <c>SelectedTemplate</c> takes that branch instead of the default chip list, even one that
    /// renders nothing, so this is enough to suppress it while leaving <c>CustomContent</c> untouched.
    /// Shared here rather than duplicated per component for the same reason as the rest of this class:
    /// <typeparamref name="TValue"/> already equals the <c>T?</c> MudBlazor's own
    /// <c>RenderFragment&lt;T?&gt;</c> expects, since each component declares this base with its own
    /// <c>MudFileUpload</c>'s value type.
    /// </remarks>
    protected static RenderFragment SuppressBuiltInFileList(TValue value) => builder => { };
}
