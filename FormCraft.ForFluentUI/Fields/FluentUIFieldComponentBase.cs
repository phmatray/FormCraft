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
    /// ⚠️ The typed <c>.WithNativeRequired(...)</c> builder method lives in the MudBlazor package, so
    /// Fluent consumers use the raw <c>.WithAttribute("Required", ...)</c> form that method writes.
    /// Both resolve through this property identically.
    /// </para>
    /// </remarks>
    protected bool EffectiveNativeRequired =>
        Context.Field.AdditionalAttributes.TryGetValue("Required", out var configured)
        && configured is bool optIn
            ? optIn
            : IsRequired;

    /// <summary>
    /// The value of <c>aria-required</c> to splat onto the rendered input, or <c>null</c> when the
    /// field is optional and the attribute should be omitted entirely.
    /// </summary>
    /// <remarks>
    /// Written explicitly rather than relying on <c>Required</c> to emit it. Fluent UI v5's
    /// published documentation covers <c>aria-label</c>, <c>aria-live</c> and <c>aria-level</c> but
    /// never <c>aria-required</c>, so the attribute this library's accessibility guarantee depends
    /// on is not one the component library promises. Writing it here makes the guarantee ours, and
    /// costs nothing if Fluent also emits it - Blazor resolves duplicate attributes
    /// last-write-wins, and both writes carry the same value.
    /// </remarks>
    protected string? AriaRequired => EffectiveNativeRequired ? "true" : null;
}
