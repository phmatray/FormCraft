namespace FormCraft.ForFluentUI;

/// <summary>
/// Base class for Fluent UI field components. Extends the framework-agnostic
/// <see cref="FieldComponentBase{TModel, TValue}"/> with the presentation concerns every Fluent
/// field shares.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
/// <typeparam name="TValue">The field's value type.</typeparam>
public abstract class FluentUIFieldComponentBase<TModel, TValue> : FieldComponentBase<TModel, TValue>
{
    /// <summary>
    /// Whether the Fluent input's native required decoration is rendered: the explicit
    /// <c>"Required"</c> additional attribute when the field sets one, otherwise
    /// <see cref="FieldComponentBase{TModel, TValue}.IsRequired"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrors the contract <c>MudBlazorFieldComponentBase.EffectiveNativeRequired</c> settled on in
    /// #199/#204: the explicit attribute wins in <b>both</b> directions - a field that asked for the
    /// decoration without <c>.Required(...)</c> gets it, and one that suppressed it with
    /// <c>.WithAttribute("Required", false)</c> stays suppressed even when <c>.Required(...)</c> is
    /// configured. Only an unconfigured field falls through to the validator's own answer.
    /// </para>
    /// <para>
    /// A required field that is not announced as required fails WCAG 2.1 <b>3.3.2</b> (Level A),
    /// which is why this defaults to <see cref="FieldComponentBase{TModel, TValue}.IsRequired"/>
    /// rather than to silence. The HTML5 <c>required</c> attribute that Fluent renders alongside the
    /// ARIA one is inert here: FormCraft forms render <c>novalidate</c> (#206).
    /// </para>
    /// <para>
    /// The rule itself is <see cref="NativeRequired.Resolve"/> in core. It was hand-copied here
    /// while it was <c>internal</c> to the MudBlazor package; #279 moved it to core so both
    /// adapters read one implementation, which is what the rule's own doc comment always claimed.
    /// </para>
    /// </remarks>
    protected bool EffectiveNativeRequired =>
        NativeRequired.Resolve(Context.Field.AdditionalAttributes, IsRequired);

    /// <summary>
    /// The value of <c>aria-required</c> to splat onto the rendered input, or <c>null</c> when the
    /// field is optional and the attribute should be omitted entirely.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written explicitly, and it is <b>load-bearing</b>: measured against
    /// <c>Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.5-26219.1</c>, setting
    /// <c>Required="true"</c> on <c>FluentTextInput</c> does <b>not</b> emit <c>aria-required</c>.
    /// Removing this attribute fails <c>AriaRequiredTests</c> - that ablation was run rather than
    /// assumed, precisely because the tests passed the first time and a passing test proves nothing
    /// about which of two candidate sources satisfied it.
    /// </para>
    /// <para>
    /// This is why the library's own guarantee cannot be delegated to Fluent's <c>Required</c> flag.
    /// The published v5 documentation covers <c>aria-label</c>, <c>aria-live</c> and
    /// <c>aria-level</c> and never mentions <c>aria-required</c>, which turned out to describe the
    /// behaviour accurately.
    /// </para>
    /// <para>
    /// Should a future Fluent release start emitting it too, this stays correct: Blazor resolves
    /// duplicate attributes last-write-wins and both writes carry the same value.
    /// </para>
    /// </remarks>
    protected string? AriaRequired => EffectiveNativeRequired ? "true" : null;

    /// <summary>
    /// Whether the bound model property is a nullable value type.
    /// </summary>
    /// <remarks>
    /// Components whose <c>TValue</c> is a non-nullable value type still have to round-trip a
    /// cleared input as <c>null</c> when the underlying property is nullable (#150). Writing
    /// <c>default</c> instead is not cosmetic: <c>0001-01-01</c> satisfies a <c>Required</c>
    /// validator, so a cleared mandatory field passes validation, and it is outside the SQL
    /// <c>datetime</c> range, so the mistake surfaces at persistence rather than at the point the
    /// user made it. Mirrors <c>MudBlazorDateOnlyFieldComponent.IsNullableField</c>.
    /// </remarks>
    protected bool IsNullableField => Nullable.GetUnderlyingType(Context.ActualFieldType) != null;

    /// <summary>
    /// The id of this field's help-text element, or <c>null</c> when the field configures none.
    /// </summary>
    /// <remarks>
    /// Help text is rendered by the container rather than bound to the input's own <c>Message</c>
    /// parameter - see <see cref="FieldHelpText"/> for why - so the association an assistive
    /// technology needs has to be made explicitly with <c>aria-describedby</c>. Without it the text
    /// is visible but unannounced, which is the failure mode this adapter already refuses to accept
    /// for <c>aria-required</c>.
    /// </remarks>
    protected string? AriaDescribedBy =>
        string.IsNullOrWhiteSpace(HelpText) ? null : FieldHelpText.IdFor(Context.Field.FieldName);
}
