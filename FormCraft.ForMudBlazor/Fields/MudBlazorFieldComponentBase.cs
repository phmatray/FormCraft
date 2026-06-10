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
    /// Gets the variant to apply to the MudBlazor input: the field-level "Variant"
    /// additional attribute (set via <c>.WithVariant(...)</c>) when present, otherwise
    /// the cascaded form-level default, otherwise <see cref="Variant.Outlined"/>.
    /// </summary>
    protected Variant EffectiveVariant =>
        GetAttribute<Variant?>("Variant") ?? FormDefaultVariant ?? Variant.Outlined;
}
