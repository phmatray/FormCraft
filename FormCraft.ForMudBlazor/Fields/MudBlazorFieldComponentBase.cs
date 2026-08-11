using Microsoft.AspNetCore.Components;
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
}
