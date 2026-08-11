using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// A MudBlazor component that renders a collection (one-to-many) field with add, remove, reorder capabilities.
/// Each item in the collection is rendered as a sub-form using the configured item form fields.
/// </summary>
/// <typeparam name="TModel">The parent model type.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
public partial class CollectionFieldComponent<TModel, TItem>
    where TModel : new()
    where TItem : new()
{
    /// <summary>
    /// Gets or sets the parent model instance.
    /// </summary>
    [Parameter]
    public TModel Model { get; set; } = default!;

    /// <summary>
    /// Gets or sets the collection field configuration.
    /// </summary>
    [Parameter]
    public ICollectionFieldConfiguration<TModel, TItem> Configuration { get; set; } = default!;

    /// <summary>
    /// Gets or sets the callback invoked when the collection changes (items added, removed, or reordered).
    /// </summary>
    [Parameter]
    public EventCallback OnCollectionChanged { get; set; }

    /// <summary>
    /// Gets or sets the form-level default variant cascaded by <see cref="FormCraftComponent{TModel}"/>.
    /// Used as a fallback for item fields that do not configure their own "Variant" attribute.
    /// </summary>
    [CascadingParameter(Name = FormCraftCascadingValues.DefaultVariant)]
    public Variant? FormDefaultVariant { get; set; }

    /// <summary>
    /// Gets or sets the form-level default ShrinkLabel cascaded by <see cref="FormCraftComponent{TModel}"/>.
    /// Used as a fallback for item fields that do not configure their own "ShrinkLabel" attribute.
    /// </summary>
    [CascadingParameter(Name = FormCraftCascadingValues.DefaultShrinkLabel)]
    public bool? FormDefaultShrinkLabel { get; set; }

    /// <summary>
    /// Service provider used to resolve an optional <see cref="ILoggerFactory"/> for the
    /// ShrinkLabel diagnostic (#181). Degrades silently when no logger is registered.
    /// </summary>
    [Inject]
    private IServiceProvider? DiagnosticServices { get; set; }

    /// <summary>
    /// The form's diagnostic collector, so item-field conflicts join the form's single warning.
    /// </summary>
    [CascadingParameter(Name = FormCraftCascadingValues.ShrinkLabelDiagnostics)]
    public ShrinkLabelDiagnosticCollector? ShrinkLabelDiagnostics { get; set; }

    /// <summary>
    /// Item fields already reported, so the diagnostic fires once per field rather than once
    /// per row — a collection of 50 items must not produce 50 identical warnings.
    /// </summary>
    private readonly HashSet<string> _warnedItemFields = [];

    /// <summary>
    /// Gets or sets the parent form's EditContext, cascaded from the surrounding EditForm.
    /// When present, item field changes raise <see cref="EditContext.NotifyFieldChanged(in FieldIdentifier)"/>
    /// with a nested field identifier (e.g. <c>Items[0].ProductName</c>) on the root model, so
    /// modification tracking and Blazor's validation infrastructure see collection item edits.
    /// </summary>
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    private List<TItem> Items => Configuration.CollectionAccessor(Model);

    private bool HasReachedMax => Configuration.MaxItems > 0 && Items.Count >= Configuration.MaxItems;

    private bool HasReachedMin => Configuration.MinItems > 0 && Items.Count <= Configuration.MinItems;

    private List<string> ValidationErrors { get; set; } = new();

    private async Task AddItem()
    {
        if (HasReachedMax) return;

        Items.Add(new TItem());
        await NotifyCollectionChanged();
    }

    private async Task RemoveItem(int index)
    {
        if (HasReachedMin) return;
        if (index < 0 || index >= Items.Count) return;

        Items.RemoveAt(index);
        await NotifyCollectionChanged();
    }

    private async Task MoveItemUp(int index)
    {
        if (index <= 0 || index >= Items.Count) return;

        (Items[index], Items[index - 1]) = (Items[index - 1], Items[index]);
        await NotifyCollectionChanged();
    }

    private async Task MoveItemDown(int index)
    {
        if (index < 0 || index >= Items.Count - 1) return;

        (Items[index], Items[index + 1]) = (Items[index + 1], Items[index]);
        await NotifyCollectionChanged();
    }

    private async Task NotifyCollectionChanged()
    {
        if (OnCollectionChanged.HasDelegate)
        {
            await OnCollectionChanged.InvokeAsync();
        }

        StateHasChanged();
    }

    private async Task UpdateItemFieldValue(int itemIndex, string fieldName, object? value)
    {
        if (itemIndex < 0 || itemIndex >= Items.Count) return;

        var item = Items[itemIndex];
        var property = typeof(TItem).GetProperty(fieldName);
        if (property != null)
        {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = value;

            if (value != null && value.GetType() != targetType)
            {
                try
                {
                    convertedValue = Convert.ChangeType(value, targetType);
                }
                catch
                {
                    // If conversion fails, use the value as-is
                }
            }

            property.SetValue(item, convertedValue);

            // Notify the parent EditContext with a nested field identifier
            // (Blazor convention: model stays the root model, the field name
            // encodes the collection path, e.g. "Items[0].ProductName") so
            // IsModified tracking and validation messages work natively.
            if (EditContext != null && Model is not null)
            {
                EditContext.NotifyFieldChanged(GetItemFieldIdentifier(itemIndex, fieldName));
            }
        }

        await NotifyCollectionChanged();
    }

    private FieldIdentifier GetItemFieldIdentifier(int itemIndex, string fieldName)
        => new(Model!, $"{Configuration.FieldName}[{itemIndex}].{fieldName}");

    private RenderFragment RenderItemFields(int itemIndex)
    {
        return builder =>
        {
            if (Configuration.ItemFormConfiguration == null) return;

            var item = Items[itemIndex];

            foreach (var field in Configuration.ItemFormConfiguration.Fields.OrderBy(f => f.Order))
            {
                var capturedIndex = itemIndex;
                var capturedFieldName = field.FieldName;

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "mb-3");
                builder.AddContent(2, RenderItemField(item, field, capturedIndex));
                // Surface validation messages attached to the nested field identifier
                // (e.g. Items[0].ProductName) next to the item field input.
                builder.OpenComponent<FieldValidationMessage>(3);
                builder.AddAttribute(4, "FieldName", $"{Configuration.FieldName}[{capturedIndex}].{capturedFieldName}");
                builder.CloseComponent();
                builder.CloseElement();
            }
        };
    }

    private RenderFragment RenderItemField(TItem item, IFieldConfiguration<TItem, object> field, int itemIndex)
    {
        return builder =>
        {
            var property = typeof(TItem).GetProperty(field.FieldName);
            if (property == null) return;

            var fieldType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            var value = property.GetValue(item);

            if (fieldType == typeof(string))
            {
                RenderTextField(builder, field, value as string, itemIndex);
            }
            else if (underlyingType == typeof(int))
            {
                RenderNumericField<int>(builder, field, (int)(value ?? 0), itemIndex);
            }
            else if (underlyingType == typeof(decimal))
            {
                RenderNumericField<decimal>(builder, field, (decimal)(value ?? 0m), itemIndex);
            }
            else if (underlyingType == typeof(double))
            {
                RenderNumericField<double>(builder, field, (double)(value ?? 0.0), itemIndex);
            }
            else if (underlyingType == typeof(bool))
            {
                RenderBooleanField(builder, field, value ?? false, itemIndex);
            }
            else if (underlyingType == typeof(DateTime))
            {
                RenderDateTimeField(builder, field, value as DateTime?, itemIndex);
            }
        };
    }

    private void RenderTextField(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, string? value, int itemIndex)
    {
        builder.OpenComponent<MudTextField<string>>(0);
        AddCommonFieldAttributes(builder, field, CommonAttributeStart, rendersAdornment: true);
        builder.AddAttribute(CallerAttributeStart, "Value", value);
        builder.AddAttribute(CallerAttributeStart + 1, "ValueChanged",
            EventCallback.Factory.Create<string>(this,
                newValue => UpdateItemFieldValue(itemIndex, field.FieldName, newValue)));
        builder.AddAttribute(CallerAttributeStart + 2, "Immediate", true);
        builder.CloseComponent();
    }

    private void RenderNumericField<T>(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, T value, int itemIndex)
        where T : struct
    {
        builder.OpenComponent(0, typeof(MudNumericField<>).MakeGenericType(typeof(T)));
        AddCommonFieldAttributes(builder, field, CommonAttributeStart, rendersAdornment: true);
        builder.AddAttribute(CallerAttributeStart, "Value", value);
        builder.AddAttribute(CallerAttributeStart + 1, "ValueChanged",
            EventCallback.Factory.Create<T>(this,
                newValue => UpdateItemFieldValue(itemIndex, field.FieldName, newValue)));
        builder.AddAttribute(CallerAttributeStart + 2, "Immediate", true);
        // MudBlazor appends '*' to Pattern before emitting the HTML attribute, so a
        // fully-anchored regex here becomes invalid (e.g. "...?*"). The component's
        // default pattern already handles decimal input; only Culture is needed.
        builder.AddAttribute(CallerAttributeStart + 3, "Culture", System.Globalization.CultureInfo.InvariantCulture);
        builder.CloseComponent();
    }

    private void RenderBooleanField(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, object value, int itemIndex)
    {
        builder.OpenComponent<MudCheckBox<bool>>(0);
        builder.AddAttribute(1, "Label", field.Label);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<bool>(this,
                newValue => UpdateItemFieldValue(itemIndex, field.FieldName, newValue)));
        builder.AddAttribute(4, "ReadOnly", field.IsReadOnly);
        builder.AddAttribute(5, "Disabled", field.IsDisabled);
        builder.CloseComponent();
    }

    private void RenderDateTimeField(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, DateTime? value, int itemIndex)
    {
        builder.OpenComponent<MudDatePicker>(0);
        // rendersAdornment: false — MudDatePicker defaults to Adornment.End with its calendar
        // icon, so forwarding a field's (usually unset) adornment here would silently strip that
        // icon on every date item field. Out of scope for #184; see the issue's non-goals.
        AddCommonFieldAttributes(builder, field, CommonAttributeStart, rendersAdornment: false);
        builder.AddAttribute(CallerAttributeStart, "Date", value);
        builder.AddAttribute(CallerAttributeStart + 1, "DateChanged",
            EventCallback.Factory.Create<DateTime?>(this,
                newValue => UpdateItemFieldValue(itemIndex, field.FieldName, newValue)));
        builder.CloseComponent();
    }

    /// <summary>
    /// First sequence number <see cref="AddCommonFieldAttributes"/> may use. It never emits more
    /// than <see cref="CallerAttributeStart"/> minus this many attributes.
    /// </summary>
    private const int CommonAttributeStart = 1;

    /// <summary>
    /// First sequence number an item-field renderer may use for its own attributes. Deliberately
    /// well clear of the common block: Blazor requires sequence numbers to be constants tied to a
    /// source position, never computed, so callers cannot resume from "wherever the shared helper
    /// stopped" — the gap is what lets the common set grow without every caller moving.
    /// </summary>
    private const int CallerAttributeStart = 20;

    /// <summary>
    /// Emits the presentation attributes every item field shares.
    /// </summary>
    /// <param name="builder">The render tree builder for the open item-field component.</param>
    /// <param name="field">The item field's configuration.</param>
    /// <param name="startIndex">The first sequence number this method may use.</param>
    /// <param name="rendersAdornment">
    /// Whether the target component takes MudBlazor's three adornment parameters and defaults them
    /// to "no adornment" (#184). True for MudTextField and MudNumericField. False for MudDatePicker,
    /// whose own default is <see cref="Adornment.End"/> with a calendar icon that forwarding would
    /// erase.
    /// </param>
    private void AddCommonFieldAttributes(
        RenderTreeBuilder builder,
        IFieldConfiguration<TItem, object> field,
        int startIndex,
        bool rendersAdornment)
    {
        builder.AddAttribute(startIndex++, "Label", field.Label);
        builder.AddAttribute(startIndex++, "Placeholder", field.Placeholder);
        builder.AddAttribute(startIndex++, "HelperText", field.HelpText);
        // Resolved from an explicit attribute, NOT from field.IsRequired (#190). Validation here is
        // server-side — forms render novalidate, messages come from the configured validator, and no
        // component-path renderer emits Required — so driving this from IsRequired put required and
        // aria-required="true" on the rendered input of a .Required("…") item field while the same
        // call on an ordinary field emitted neither. Measured: it armed no second validator (item
        // fields have no MudForm and no For), so this is a convention and a11y-semantics fix, not a
        // duplicate-message one. Reading the attribute keeps .WithAttribute("Required", true) working.
        builder.AddAttribute(startIndex++, "Required", GetItemFieldAttribute(field, "Required", false));
        builder.AddAttribute(startIndex++, "ReadOnly", field.IsReadOnly);
        builder.AddAttribute(startIndex++, "Disabled", field.IsDisabled);
        builder.AddAttribute(startIndex++, "Variant", GetItemFieldVariant(field));
        builder.AddAttribute(startIndex++, "Margin", Margin.Dense);

        var shrinkLabel = GetItemFieldShrinkLabel(field);
        builder.AddAttribute(startIndex++, "ShrinkLabel", shrinkLabel);

        // Null on a path that draws no adornment of ours — which the diagnostic below needs to
        // tell apart from "none configured".
        var adornment = rendersAdornment ? GetItemFieldAdornment(field) : (Adornment?)null;

        // The branch is per-CALL-SITE, not per-configuration: `adornment` is null exactly when the
        // caller passed rendersAdornment: false, so a given renderer always emits the same frames
        // in the same order. Within the branch all three are emitted whether or not the field
        // configured one, using the component's own defaults — so an item field with no adornment
        // renders exactly as it did before #184.
        if (adornment is { } rendered)
        {
            builder.AddAttribute(startIndex++, "Adornment", rendered);
            builder.AddAttribute(startIndex++, "AdornmentIcon", GetItemFieldAttribute<string?>(field, "AdornmentIcon", null));
            builder.AddAttribute(startIndex, "AdornmentColor", GetItemFieldAttribute(field, "AdornmentColor", Color.Default));
        }

        // The diagnostic has to judge what this path actually RENDERS, not what was configured:
        // a dropped adornment cannot pin the label, so reporting one would tell the developer to
        // remove a setting that was working (#183).
        WarnIfShrinkLabelUnhonoured(field, shrinkLabel, adornment);
    }

    /// <summary>
    /// Reports a ShrinkLabel conflict for an item field (#181), using the same rule as the
    /// component render path — <see cref="ShrinkLabelDiagnostic.Conflict"/> is the single
    /// implementation, so the two paths cannot drift apart.
    /// </summary>
    /// <param name="field">The item field's configuration.</param>
    /// <param name="shrinkLabel">The ShrinkLabel value this field renders with.</param>
    /// <param name="renderedAdornment">
    /// The adornment this render path actually draws, or <c>null</c> when it draws none — which is
    /// not the same as the field's configured adornment. Item fields whose component takes the
    /// forward (#184) pass the real value; the date path, which keeps MudDatePicker's own calendar
    /// adornment instead, passes null so that a configured-but-dropped adornment is not reported.
    /// </param>
    private void WarnIfShrinkLabelUnhonoured(
        IFieldConfiguration<TItem, object> field,
        bool shrinkLabel,
        Adornment? renderedAdornment)
    {
        if (shrinkLabel || !_warnedItemFields.Add(field.FieldName))
        {
            return;
        }

        var conflict = ShrinkLabelDiagnostic.Conflict(field.Placeholder, renderedAdornment);

        if (conflict is null)
        {
            return;
        }

        // Prefer the form's collector so item fields join the single aggregated warning.
        if (ShrinkLabelDiagnostics is not null)
        {
            ShrinkLabelDiagnostics.Report(field.FieldName, field.Label, conflict);
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
                field.Label ?? field.FieldName,
                conflict);
        }
        catch
        {
            // Ignored: a failing diagnostic must not take the form down with it.
        }
    }

    /// <summary>
    /// Resolves the variant for an item field: the field-level "Variant" attribute when
    /// present, otherwise the cascaded form-level default, otherwise Outlined.
    /// </summary>
    private Variant GetItemFieldVariant(IFieldConfiguration<TItem, object> field)
    {
        if (field.AdditionalAttributes.TryGetValue("Variant", out var value) && value is Variant variant)
        {
            return variant;
        }

        return FormDefaultVariant ?? Variant.Outlined;
    }

    /// <summary>
    /// Resolves the adornment position for an item field (#184): the field-level "Adornment"
    /// attribute when present, otherwise none. There is no form-level default for adornments.
    /// </summary>
    private static Adornment GetItemFieldAdornment(IFieldConfiguration<TItem, object> field)
        => GetItemFieldAttribute(field, "Adornment", Adornment.None);

    /// <summary>
    /// Reads a strongly-typed item-field attribute, returning <paramref name="fallback"/> when it
    /// is absent or holds a value of another type.
    /// </summary>
    /// <remarks>
    /// <c>WithAdornment(...)</c> always writes all three adornment attributes together, but a field
    /// that set only "Adornment" through raw <c>WithAttribute(...)</c> has no icon or colour to
    /// read — so each is resolved independently rather than assuming the others are present.
    /// </remarks>
    private static T GetItemFieldAttribute<T>(IFieldConfiguration<TItem, object> field, string name, T fallback)
        => field.AdditionalAttributes.TryGetValue(name, out var value) && value is T typed
            ? typed
            : fallback;

    /// <summary>
    /// Resolves ShrinkLabel for an item field: the field-level "ShrinkLabel" attribute when
    /// present, otherwise the cascaded form-level default, otherwise true.
    /// </summary>
    private bool GetItemFieldShrinkLabel(IFieldConfiguration<TItem, object> field)
    {
        if (field.AdditionalAttributes.TryGetValue("ShrinkLabel", out var value) && value is bool shrinkLabel)
        {
            return shrinkLabel;
        }

        return FormDefaultShrinkLabel ?? true;
    }
}
