namespace FormCraft.ForMudBlazor;

/// <summary>
/// Shared base for the single- and multiple-file upload components, holding the required-marking
/// rule the two have to apply identically (#262).
/// </summary>
/// <remarks>
/// <para>
/// The two upload components drifting apart is the failure class this library keeps re-filing
/// (#146, #177, #184, #189), so the rule lives here once rather than as a copy in each component.
/// </para>
/// <para>
/// <b>Why not the hidden input.</b> Both components render <c>MudFileUpload</c>'s real
/// <c>&lt;input type="file"&gt;</c> at <c>opacity-0</c> with <c>tabindex="-1"</c> beneath a custom
/// drop zone, deliberately out of the tab order. Binding <c>Required</c> there would emit
/// <c>aria-required</c> on an element no keyboard or screen-reader user ever reaches — the
/// "forwarded but inert" failure this library's parity tests exist to catch. So the requirement is
/// identified where the user actually is, on two channels: visibly in the field's own
/// <c>&lt;MudText&gt;</c> label, and programmatically via <c>aria-describedby</c> on the
/// <c>MudButton</c> that receives focus.
/// </para>
/// </remarks>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
/// <typeparam name="TValue">The field's value type — one file, or a list of them.</typeparam>
public abstract class MudBlazorFileUploadComponentBase<TModel, TValue> : FieldComponentBase<TModel, TValue>
{
    /// <summary>
    /// Hides the requirement hint visually while leaving it in the accessibility tree, so
    /// <c>aria-describedby</c> still resolves to it. Inline rather than a class because this library
    /// ships no stylesheet of its own — it styles entirely through MudBlazor's, which carries no
    /// visually-hidden utility.
    /// </summary>
    protected const string RequiredDescriptionStyle =
        "position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0";

    /// <summary>
    /// Whether this field is marked as required, resolved by the same rule as every other field
    /// type: an explicit <c>.WithNativeRequired(...)</c> wins over <c>IsRequired</c> in both
    /// directions (#199).
    /// </summary>
    protected bool NativeRequiredValue =>
        NativeRequired.Resolve(Context.Field.AdditionalAttributes, IsRequired);

    /// <summary>
    /// The id of the requirement hint, derived from the field name so that several upload fields on
    /// one form get distinct ids rather than colliding on a shared one.
    /// </summary>
    protected string RequiredDescriptionId => $"formcraft-{Context.Field.FieldName}-required";

    /// <summary>
    /// The value for the focusable button's <c>aria-describedby</c>: the hint's id when the field is
    /// required, otherwise <c>null</c>, which makes Blazor omit the attribute altogether.
    /// </summary>
    protected string? RequiredDescribedBy => NativeRequiredValue ? RequiredDescriptionId : null;

    /// <summary>
    /// The hint text. Falls back to a label-free wording on purpose: the component renders its
    /// <c>&lt;MudText&gt;</c> label only when one is configured, so an unlabelled required field has
    /// the button description as its only remaining channel.
    /// </summary>
    protected string RequiredDescription =>
        string.IsNullOrWhiteSpace(Label) ? "This file upload is required." : $"{Label} is required.";
}
