using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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

        var fieldName = Context.Field.FieldName;
        var field = Label ?? fieldName;

        // Inside a FormCraftComponent, report to the form's collector so all conflicting fields
        // arrive in one warning. Rendered standalone there is no collector, so log directly
        // rather than lose the diagnostic entirely.
        if (ShrinkLabelDiagnostics is not null)
        {
            ShrinkLabelDiagnostics.Report(fieldName, Label, conflict);
            return;
        }

        // A diagnostic must never break a render, so a logger that throws is swallowed.
        try
        {
            var logger = DiagnosticServices?
                .GetService<ILoggerFactory>()?
                .CreateLogger(ShrinkLabelDiagnostic.Category);

            logger?.LogWarning(
                "Field '{Field}' sets ShrinkLabel=false but also has {Conflict}, which MudBlazor " +
                "lets win — the label stays pinned and will not float. Remove that property to get " +
                "a floating label, or drop ShrinkLabel=false.",
                field,
                conflict);
        }
        catch
        {
            // Ignored: a failing diagnostic must not take the form down with it.
        }
    }

    /// <summary>
    /// Returns the property that will override <c>ShrinkLabel=false</c> for this field, or
    /// null when the setting will be honoured.
    /// </summary>
    private string? ShrinkLabelConflict() =>
        ShrinkLabelDiagnostic.Conflict(Placeholder, GetAttribute<Adornment?>("Adornment"));
}

/// <summary>
/// The single implementation of the ShrinkLabel conflict rule, shared by the component render
/// path (<see cref="MudBlazorFieldComponentBase{TModel, TValue}"/>) and the imperative
/// RenderTreeBuilder path used for collection item fields.
/// </summary>
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
