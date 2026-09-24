using FormCraft.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class FormCraftComponent<TModel>
{
    [Parameter]
    public TModel Model { get; set; } = new();

    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    [Parameter]
    public EventCallback<TModel> OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback<(string fieldName, object? value)> OnFieldChanged { get; set; }

    [Parameter]
    public bool ShowSubmitButton { get; set; } = true;

    [Parameter]
    public string SubmitButtonText { get; set; } = "Submit";

    [Parameter]
    public string SubmittingText { get; set; } = "Submitting...";

    [Parameter]
    public bool IsSubmitting { get; set; }

    [Parameter]
    public string? SubmitButtonClass { get; set; }

    [Parameter]
    public RenderFragment? BeforeForm { get; set; }

    [Parameter]
    public RenderFragment? AfterForm { get; set; }

    [Parameter]
    public EventCallback<EditContext> OnEditContextCreated { get; set; }

    /// <summary>
    /// Default MudBlazor <see cref="Variant"/> applied to every rendered input field.
    /// Individual fields override it via the <c>.WithVariant(...)</c> builder extension.
    /// Defaults to <see cref="Variant.Outlined"/>.
    /// </summary>
    [Parameter]
    public Variant DefaultVariant { get; set; } = Variant.Outlined;

    /// <summary>
    /// Default value for MudBlazor's <c>ShrinkLabel</c> on every rendered input field.
    /// Individual fields override it via the <c>.WithShrinkLabel(...)</c> builder extension.
    /// Defaults to <c>true</c>, which keeps each label pinned above its input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set this to <c>false</c> alongside <see cref="DefaultVariant"/> =
    /// <see cref="Variant.Text"/>: that variant draws no border for a shrunk label to sit
    /// in, so the label should float up from inside the input on focus instead.
    /// </para>
    /// <para>
    /// <b><c>false</c> only has a visible effect on empty fields with no placeholder and no
    /// start adornment.</b> MudBlazor ORs <c>ShrinkLabel</c> with those conditions, so fields
    /// that have a value, a placeholder or a start adornment keep their label pinned
    /// regardless. Fields configured with a placeholder are unaffected by this parameter.
    /// </para>
    /// </remarks>
    [Parameter]
    public bool DefaultShrinkLabel { get; set; } = true;

    /// <summary>
    /// Stable identifier used for security enforcement (rate limiting and audit log
    /// entries) configured via <c>WithSecurity()</c>. Set this to a per-user or
    /// per-session value (e.g. user id, circuit id, IP address) so limits are not
    /// shared across all users. Defaults to the model type name.
    /// </summary>
    [Parameter]
    public string? SecurityContextId { get; set; }

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Collects ShrinkLabel conflicts reported by this form's fields during the render pass, so
    /// they surface as one warning naming every affected field rather than one warning each (#181).
    /// </summary>
    private readonly ShrinkLabelDiagnosticCollector _shrinkLabelDiagnostics = new();

    /// <summary>
    /// This form's diagnostic latch (#304), cascaded to every field it renders.
    /// </summary>
    /// <remarks>
    /// A readonly field initialised once per component, deliberately — created in the markup or in a
    /// lifecycle method it would be rebuilt on re-render and latch nothing. Cascaded with
    /// <c>IsFixed="true"</c> for the same reason the collector above is: the reference never changes,
    /// so subscribers need no change notifications.
    /// <para>
    /// ⚠️ Deliberately <b>not</b> rebuilt when <see cref="Configuration"/> is re-pointed, which is
    /// where this differs from <c>CollectionFieldComponent</c>'s scope (that one is rebuilt in
    /// <c>OnParametersSet</c> when it is aimed at a different collection). Swapping one form's
    /// configuration for another keeps the latch, so a field of the same name tripping the same
    /// diagnostic in both configurations reports once rather than twice. That is the intended
    /// reading — it is the same form component telling the developer the same thing — and the
    /// alternative costs a per-configuration rebuild to say it twice.
    /// </para>
    /// </remarks>
    private readonly FormDiagnosticScope _formDiagnosticScope = new();

    private EditContext? _editContext;
    private DynamicFormValidator<TModel>? _validator;
    private IGroupedFormConfiguration<TModel>? GroupedConfiguration => Configuration as IGroupedFormConfiguration<TModel>;
    private ICollectionFormConfiguration<TModel>? CollectionConfiguration => Configuration as ICollectionFormConfiguration<TModel>;

    /// <summary>
    /// The <c>formcraft-layout-{value}</c> class the ungrouped-fields wrapper carries (#457).
    /// </summary>
    private string LayoutCssClass => $"formcraft-layout-{Configuration.Layout.ToString().ToLowerInvariant()}";

    protected override async Task OnInitializedAsync()
    {
        if (Model != null)
        {
            _editContext = new EditContext(Model);
            if (OnEditContextCreated.HasDelegate)
            {
                await OnEditContextCreated.InvokeAsync(_editContext);
            }
        }
        await SecurityEnforcer.InitializeAsync(Configuration, SecurityContextId);
        await base.OnInitializedAsync();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        // Every field has now rendered and reported, so the collector holds the current set.
        // Flush reports each field once but stays live, so fields revealed later (a visibility
        // condition, an expanded group, a new collection row) are still picked up. Resolving the
        // logger is Flush's job — it happens inside that method's exception guard.
        _shrinkLabelDiagnostics.Flush(ServiceProvider);
    }

    /// <summary>
    /// This form's security pipeline (rate limiting, CSRF, audit logging, encryption), shared with
    /// the Fluent UI adapter from core since #321. Only the banner that shows its
    /// <see cref="FormSecurityEnforcer{TModel}.Error"/> is this adapter's own.
    /// </summary>
    /// <remarks>
    /// Created on first use rather than in <see cref="OnInitializedAsync"/>: the markup reads its
    /// error, and a render can land before initialisation finishes (it awaits
    /// <see cref="OnEditContextCreated"/> first). The logger is this component's own, so security
    /// misconfigurations keep reporting under the component's log category.
    /// </remarks>
    private FormSecurityEnforcer<TModel> SecurityEnforcer =>
        field ??= new FormSecurityEnforcer<TModel>(
            ServiceProvider,
            ServiceProvider.GetService<ILogger<FormCraftComponent<TModel>>>());

    /// <summary>
    /// Returns the values of the fields configured for encryption via
    /// <c>WithSecurity(s =&gt; s.EncryptField(...))</c>, encrypted with the registered
    /// <see cref="IEncryptionService"/>, so applications can persist them safely in one
    /// call. The bound model is never modified.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IEncryptionService"/> is registered (call <c>AddFormCraft()</c>).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an <c>EncryptedFields</c> entry cannot be resolved to a readable, settable
    /// string property chain — typically a typo in the dotted-path key or a non-string entry
    /// added by hand. See the "How It Works" section in <c>docs/security.md</c> for the expected
    /// property path format.
    /// </exception>
    public IReadOnlyDictionary<string, string?> GetEncryptedFieldValues() =>
        SecurityEnforcer.EncryptConfiguredFields(Model, Configuration?.Security);

    public bool Validate()
    {
        return _editContext?.Validate() ?? false;
    }

    /// <summary>
    /// Validates the form, awaiting any asynchronous validators before returning.
    /// Prefer this over <see cref="Validate"/> when async validators are configured.
    /// </summary>
    public async Task<bool> ValidateAsync()
    {
        if (_validator != null)
        {
            return await _validator.ValidateModelAsync();
        }

        return _editContext?.Validate() ?? false;
    }

    public EditContext? GetEditContext()
    {
        return _editContext;
    }

    private RenderFragment RenderField(IFieldConfiguration<TModel, object> field)
    {
        return builder =>
        {
            // Custom templates take precedence over every renderer
            if (field.CustomTemplate != null && _editContext != null)
            {
                var templateContext = new FieldContext<TModel, object>(
                    Model,
                    field,
                    _editContext,
                    () => GetCustomTemplateValue(field),
                    newValue => _ = UpdateFieldValue(field, newValue),
                    EventCallback.Factory.Create<object>(this, newValue => UpdateFieldValue(field, newValue)));
                builder.AddContent(0, field.CustomTemplate(templateContext));
                return;
            }

            // Single rendering path: every field is dispatched through the
            // FieldRendererService registry (#148). Type- and configuration-based
            // selection (text, numeric, boolean, date, select, LOV, lookup,
            // autocomplete, file upload, custom renderers) lives in the
            // registered IFieldRenderer implementations.
            builder.AddContent(0, FieldRendererService.RenderField(
                Model,
                field,
                EventCallback.Factory.Create<object?>(this, val => UpdateFieldValue(field, val)),
                EventCallback.Factory.Create(this, () => HandleFieldDependencyChanged(field.FieldName))));
        };
    }

    /// <summary>
    /// Reads a custom-template field's value through the compiled getter the renderer and
    /// validators already share (<see cref="FieldValueGetterCache{TModel}"/>, #312), instead of the
    /// per-render <c>GetProperty</c>/<c>GetValue</c> reflection this replaced (#330).
    /// <c>field.FieldName</c> was, before #437, only the expression's last member (e.g. <c>"Value"</c> for
    /// <c>x =&gt; x.Nested.Value</c>), so the old lookup against <typeparamref name="TModel"/> failed
    /// — and the field rendered nothing — for anything but a direct top-level property. Reading
    /// through the expression itself removes that failure mode for any reachable binding.
    /// </summary>
    /// <remarks>
    /// A binding that still cannot be evaluated against the current model — most commonly a null
    /// intermediate reference in a nested path — reports once per field via
    /// <see cref="_formDiagnosticScope"/> and returns <c>null</c>, so the template still renders
    /// instead of the exception reaching the render pipeline or the field staying invisible.
    /// <para>
    /// ⚠️ The latch key is the field's <b>expression text</b>, not <c>field.FieldName</c> —
    /// <c>FieldName</c> was, before #437, only the value expression's last member (#330's own defect), so two
    /// different nested bindings that happen to end in the same member name (<c>x =&gt; x.A.Value</c>
    /// and <c>x =&gt; x.B.Value</c> both report <c>"Value"</c>) would otherwise share one latch slot:
    /// whichever field failed first would silently suppress the other's warning forever, the same
    /// over-latching failure <c>CollectionItemFieldScope</c>'s own doc warns about. The expression's
    /// <c>ToString()</c> is unique per distinct binding, which a bare field name is not.
    /// </para>
    /// </remarks>
    private object GetCustomTemplateValue(IFieldConfiguration<TModel, object> field)
    {
        try
        {
            return FieldValueGetterCache<TModel>.GetOrCompile(field)(Model);
        }
        catch (Exception ex)
        {
            if (_formDiagnosticScope.ShouldWarnOnce(CustomTemplateFieldDiagnosticCategory, field.ValueExpression.ToString()))
            {
                var displayName = string.IsNullOrWhiteSpace(field.Label) ? field.FieldName : field.Label;
                FormDiagnosticLog.Warn(
                    ServiceProvider,
                    CustomTemplateFieldDiagnosticCategory,
                    "Field '{Field}' has a custom template whose value could not be read from the " +
                    "model ({ExceptionType}: {ExceptionMessage}), so it renders with no value " +
                    "instead of the whole form failing. Check that its binding expression is " +
                    "reachable (e.g. no null intermediate in a nested path).",
                    displayName,
                    ex.GetType().Name,
                    ex.Message);
            }

            return null!;
        }
    }

    /// <summary>Logger category for the unresolved-custom-template-field diagnostic (#330).</summary>
    private const string CustomTemplateFieldDiagnosticCategory = "FormCraft.ForMudBlazor.CustomTemplateField";

    /// <summary>
    /// Writes a new value back through the field's compiled <c>ValueExpression</c>
    /// (<see cref="FieldValueSetterCache{TModel}"/>), instead of the per-write
    /// <c>GetProperty</c>/<c>SetValue</c> reflection this replaced (#396). That old lookup mirrored
    /// the read side's #330 defect: it only ever resolved a direct top-level property, so a
    /// custom-template field bound to a nested path (e.g. <c>x =&gt; x.Nested.Value</c>) rendered
    /// correctly after #330 but silently dropped every edit, because
    /// <c>typeof(TModel).GetProperty("Value")</c> never found anything on <typeparamref name="TModel"/>.
    /// </summary>
    /// <remarks>
    /// A binding that still cannot be written — most commonly a null intermediate reference in a
    /// nested path — reports once per field via <see cref="_formDiagnosticScope"/> and drops the
    /// edit, instead of the exception escaping this event callback.
    /// </remarks>
    private async Task UpdateFieldValue(IFieldConfiguration<TModel, object> field, object? value)
    {
        try
        {
            FieldValueSetterCache<TModel>.GetOrCompile(field)(Model, value);
        }
        catch (Exception ex)
        {
            if (_formDiagnosticScope.ShouldWarnOnce(CustomTemplateFieldDiagnosticCategory, field.ValueExpression.ToString()))
            {
                var displayName = string.IsNullOrWhiteSpace(field.Label) ? field.FieldName : field.Label;
                FormDiagnosticLog.Warn(
                    ServiceProvider,
                    CustomTemplateFieldDiagnosticCategory,
                    "Field '{Field}' has a custom template whose value could not be written back to " +
                    "the model ({ExceptionType}: {ExceptionMessage}), so the edit is dropped instead " +
                    "of throwing out of the change handler. Check that its binding expression is " +
                    "reachable (e.g. no null intermediate in a nested path).",
                    displayName,
                    ex.GetType().Name,
                    ex.Message);
            }

            return;
        }

        var fieldName = field.FieldName;

        // Notify the EditContext so field-level validation runs and stale
        // error messages clear as soon as the user corrects the value.
        _editContext?.NotifyFieldChanged(_editContext.Field(fieldName));

        if (OnFieldChanged.HasDelegate)
        {
            // Report what actually landed on the model, not the raw pre-coercion value the caller
            // passed in - FieldValueSetterCache converts to the target member's type before
            // assigning, and reading it back through the same compiled getter the render path uses
            // (FieldValueGetterCache) is simpler and more accurate than re-deriving that conversion
            // here (it reflects the true post-write state rather than merely what conversion was
            // attempted). Matches the old reflection-based UpdateFieldValue, which reported its own
            // Convert.ChangeType result rather than the caller's raw value.
            await OnFieldChanged.InvokeAsync((fieldName, FieldValueGetterCache<TModel>.GetOrCompile(field)(Model)));
        }

        // Handle dependencies
        await HandleFieldDependencyChanged(fieldName);

        StateHasChanged();
    }

    private async Task HandleFieldDependencyChanged(string fieldName)
    {
        if (Configuration.FieldDependencies.TryGetValue(fieldName, out var dependencies))
        {
            foreach (IFieldDependency<TModel> dependency in dependencies)
            {
                await dependency.OnDependencyChangedAsync(Model);
            }

            // Re-render after async callbacks settle so cascaded model mutations
            // reach the UI without requiring a manual StateHasChanged call.
            StateHasChanged();
        }
    }

    private void HandleCollectionChanged()
    {
        _editContext?.NotifyValidationStateChanged();
        StateHasChanged();
    }

    private bool ShouldShowField(IFieldConfiguration<TModel, object> field)
    {
        if (field.VisibilityCondition != null)
        {
            return field.VisibilityCondition(Model);
        }

        return field.IsVisible;
    }

    private async Task HandleSubmit()
    {
        // Enforce WithSecurity() settings (rate limiting, CSRF) before validation so
        // blocked submissions never reach the application's submit handler.
        if (!await SecurityEnforcer.EnforceAsync(Configuration, Model, SecurityContextId))
        {
            StateHasChanged();
            return;
        }

        // EditForm's OnValidSubmit relies on the synchronous EditContext.Validate(),
        // which returns before async validators finish. Await the full validation
        // pass explicitly so async validators can block submission.
        var isValid = _validator != null
            ? await _validator.ValidateModelAsync()
            : _editContext?.Validate() ?? false;

        if (isValid && OnValidSubmit.HasDelegate)
        {
            await SecurityEnforcer.LogSubmittedAsync(Configuration, Model, SecurityContextId);
            await OnValidSubmit.InvokeAsync(Model);
        }
    }
}
