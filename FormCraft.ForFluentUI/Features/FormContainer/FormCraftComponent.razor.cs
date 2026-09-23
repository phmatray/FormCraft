using FormCraft.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a FormCraft configuration as a Fluent UI form.
/// </summary>
/// <remarks>
/// Deliberately shares its type name with <c>FormCraft.ForMudBlazor.FormCraftComponent&lt;TModel&gt;</c>
/// so switching adapters is a namespace change and a DI call, and nothing else. The two packages are
/// mutually exclusive - <c>AddFormCraftFluentUI()</c> refuses to register alongside the MudBlazor
/// adapter - so the shared name can never be ambiguous in a working configuration.
/// </remarks>
/// <typeparam name="TModel">The form's model type.</typeparam>
public partial class FormCraftComponent<TModel> where TModel : new()
{
    /// <summary>The model instance the form edits.</summary>
    [Parameter]
    public TModel Model { get; set; } = new();

    /// <summary>The form configuration produced by <c>FormBuilder&lt;TModel&gt;.Build()</c>.</summary>
    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    /// <summary>Invoked with the model once validation passes on submit.</summary>
    [Parameter]
    public EventCallback<TModel> OnValidSubmit { get; set; }

    /// <summary>Invoked whenever a field's value changes, with the field name and new value.</summary>
    [Parameter]
    public EventCallback<(string fieldName, object? value)> OnFieldChanged { get; set; }

    /// <summary>Whether the built-in submit button is rendered. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool ShowSubmitButton { get; set; } = true;

    /// <summary>The submit button's label.</summary>
    [Parameter]
    public string SubmitButtonText { get; set; } = "Submit";

    /// <summary>The submit button's label while <see cref="IsSubmitting"/> is set.</summary>
    [Parameter]
    public string SubmittingText { get; set; } = "Submitting...";

    /// <summary>Disables the submit button and shows <see cref="SubmittingText"/>.</summary>
    [Parameter]
    public bool IsSubmitting { get; set; }

    /// <summary>CSS class applied to the submit button.</summary>
    [Parameter]
    public string? SubmitButtonClass { get; set; }

    /// <summary>Content rendered above the form element.</summary>
    [Parameter]
    public RenderFragment? BeforeForm { get; set; }

    /// <summary>Content rendered below the form element.</summary>
    [Parameter]
    public RenderFragment? AfterForm { get; set; }

    /// <summary>Invoked with the <see cref="EditContext"/> once it has been created.</summary>
    [Parameter]
    public EventCallback<EditContext> OnEditContextCreated { get; set; }

    /// <summary>
    /// Stable identifier used for security enforcement (rate limiting and audit log entries)
    /// configured via <c>WithSecurity()</c>. Set this to a per-user or per-session value (e.g. user
    /// id, circuit id, IP address) so limits are not shared across all users. Defaults to the model
    /// type name.
    /// </summary>
    [Parameter]
    public string? SecurityContextId { get; set; }

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// This form's diagnostic latch, shared with the MudBlazor adapter from core since #398.
    /// Currently backs only <see cref="WarnUnresolvedCustomTemplateField"/>; a readonly field
    /// initialised once per component instance so the latch survives across renders.
    /// </summary>
    private readonly FormDiagnosticScope _formDiagnosticScope = new();

    private EditContext? _editContext;
    // The shared validator from core since #279, not this adapter's own copy.
    private DynamicFormValidator<TModel>? _validator;

    /// <summary>
    /// The configuration's collection fields, when it carries any. A configuration built without
    /// <c>.AddCollectionField(...)</c> does not implement this interface, so the cast is the test.
    /// </summary>
    private ICollectionFormConfiguration<TModel>? CollectionConfiguration =>
        Configuration as ICollectionFormConfiguration<TModel>;

    /// <summary>
    /// The configuration's field groups, when it carries any.
    /// </summary>
    private IGroupedFormConfiguration<TModel>? GroupedConfiguration =>
        Configuration as IGroupedFormConfiguration<TModel>;

    /// <summary>
    /// Whether this form renders grouped. All three conditions matter: a configuration that never
    /// called <c>.AddFieldGroup(...)</c> does not implement the interface, one that turned grouping
    /// off must render flat, and an empty group list would otherwise produce a form with no fields
    /// at all - every field would fall through to <c>RenderUngroupedFields</c>, which is correct,
    /// but the grouped arm's chrome around nothing is not.
    /// </summary>
    private bool HasFieldGroups =>
        GroupedConfiguration is { UseFieldGroups: true } grouped && grouped.FieldGroups.Count > 0;

    /// <summary>
    /// Maps FormCraft's integer <c>CardElevation</c> onto Fluent's five-bucket
    /// <see cref="CardShadow"/>.
    /// </summary>
    /// <remarks>
    /// The elevation number is MudBlazor's scale (0-25), which Fluent has no equivalent of, so this
    /// is a deliberate lossy mapping rather than a passthrough - the alternative, ignoring the
    /// setting, would make <c>.ShowInCard(elevation: 8)</c> silently identical to
    /// <c>.ShowInCard()</c>. Callers who want an exact shadow should style the card's CSS class.
    /// </remarks>
    /// <param name="elevation">The configured elevation.</param>
    /// <returns>The nearest Fluent shadow bucket.</returns>
    private static CardShadow ShadowFor(int elevation) => elevation switch
    {
        <= 0 => CardShadow.None,
        <= 2 => CardShadow.Small,
        <= 6 => CardShadow.Default,
        <= 12 => CardShadow.Medium,
        _ => CardShadow.Large,
    };

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (Model is not null)
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

    /// <summary>
    /// This form's security pipeline (rate limiting, CSRF, audit logging, encryption), shared with
    /// the MudBlazor adapter from core since #321. Only the <c>FluentMessageBar</c> that shows its
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
    /// <see cref="IEncryptionService"/>, so applications can persist them safely in one call. The
    /// bound model is never modified.
    /// </summary>
    /// <returns>A field-name to ciphertext map covering only the configured fields.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IEncryptionService"/> is registered (call <c>AddFormCraft()</c>).
    /// </exception>
    public IReadOnlyDictionary<string, string?> GetEncryptedFieldValues() =>
        SecurityEnforcer.EncryptConfiguredFields(Model, Configuration?.Security);

    /// <summary>Validates the form synchronously. Prefer <see cref="ValidateAsync"/>.</summary>
    public bool Validate() => _editContext?.Validate() ?? false;

    /// <summary>
    /// Validates the form, awaiting any asynchronous validators before returning.
    /// </summary>
    public async Task<bool> ValidateAsync()
    {
        if (_validator is not null)
        {
            return await _validator.ValidateModelAsync();
        }

        return _editContext?.Validate() ?? false;
    }

    /// <summary>The form's <see cref="EditContext"/>, or null before the first render.</summary>
    public EditContext? GetEditContext() => _editContext;

    private bool ShouldShowField(IFieldConfiguration<TModel, object> field) =>
        field.VisibilityCondition?.Invoke(Model) ?? field.IsVisible;

    private RenderFragment RenderField(IFieldConfiguration<TModel, object> field)
    {
        return builder =>
        {
            // A custom template takes precedence over every registered renderer.
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

            // Single rendering path: every field is dispatched through the IFieldRendererService
            // registry, which resolves the Fluent renderers registered by AddFormCraftFluentUI().
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
    /// <c>field.FieldName</c> is only the expression's last member (e.g. <c>"Value"</c> for
    /// <c>x =&gt; x.Nested.Value</c>), so the old lookup against <typeparamref name="TModel"/> failed
    /// — and the field rendered nothing — for anything but a direct top-level property. Reading
    /// through the expression itself removes that failure mode for any reachable binding. Mirrors
    /// the MudBlazor adapter's identical method, both routed through the shared
    /// <see cref="FormDiagnosticLog"/>/<see cref="FormDiagnosticScope"/> since #398.
    /// </summary>
    /// <remarks>
    /// A binding that still cannot be evaluated against the current model — most commonly a null
    /// intermediate reference in a nested path — reports once per field via
    /// <see cref="_formDiagnosticScope"/> and returns <c>null</c>, so the template still renders
    /// instead of the exception reaching the render pipeline or the field staying invisible.
    /// </remarks>
    private object GetCustomTemplateValue(IFieldConfiguration<TModel, object> field)
    {
        try
        {
            return FieldValueGetterCache<TModel>.GetOrCompile(field)(Model);
        }
        catch (Exception ex)
        {
            WarnUnresolvedCustomTemplateField(field, ex);
            return null!;
        }
    }

    /// <summary>Logger category for the unresolved-custom-template-field diagnostic (#330).</summary>
    private const string CustomTemplateFieldDiagnosticCategory = "FormCraft.ForFluentUI.CustomTemplateField";

    /// <summary>
    /// Reports a custom-template binding that could not be evaluated, once per distinct binding for
    /// this form instance rather than once per render.
    /// </summary>
    /// <remarks>
    /// ⚠️ Keyed by the field's <b>expression text</b>, not <c>field.FieldName</c> — <c>FieldName</c>
    /// is only the value expression's last member (#330's own defect), so two different nested
    /// bindings ending in the same member name (<c>x =&gt; x.A.Value</c> and <c>x =&gt; x.B.Value</c>
    /// both report <c>"Value"</c>) would otherwise collide on one latch slot and silently suppress
    /// each other's warning. The expression's <c>ToString()</c> is unique per distinct binding, which
    /// is what <see cref="FormDiagnosticScope.ShouldWarnOnce"/> is keyed on below.
    /// </remarks>
    private void WarnUnresolvedCustomTemplateField(IFieldConfiguration<TModel, object> field, Exception ex)
    {
        if (!_formDiagnosticScope.ShouldWarnOnce(CustomTemplateFieldDiagnosticCategory, field.ValueExpression.ToString()))
        {
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(field.Label) ? field.FieldName : field.Label;
        FormDiagnosticLog.Warn(
            ServiceProvider,
            CustomTemplateFieldDiagnosticCategory,
            "Field '{Field}' has a custom template whose value could not be read from the " +
            "model ({ExceptionType}: {ExceptionMessage}), so it renders with no value instead " +
            "of the whole form failing. Check that its binding expression is reachable (e.g. " +
            "no null intermediate in a nested path).",
            displayName,
            ex.GetType().Name,
            ex.Message);
    }

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
    /// nested path — reports once per field via <see cref="WarnUnresolvedCustomTemplateField"/> and
    /// drops the edit, instead of the exception escaping this event callback.
    /// </remarks>
    private async Task UpdateFieldValue(IFieldConfiguration<TModel, object> field, object? value)
    {
        try
        {
            FieldValueSetterCache<TModel>.GetOrCompile(field)(Model, value);
        }
        catch (Exception ex)
        {
            WarnUnresolvedCustomTemplateField(field, ex);
            return;
        }

        var fieldName = field.FieldName;

        // Notify the EditContext so field-level validation runs and stale errors clear as soon as
        // the user corrects the value.
        _editContext?.NotifyFieldChanged(_editContext.Field(fieldName));

        if (OnFieldChanged.HasDelegate)
        {
            await OnFieldChanged.InvokeAsync((fieldName, value));
        }

        await HandleFieldDependencyChanged(fieldName);

        StateHasChanged();
    }

    private void HandleCollectionChanged()
    {
        _editContext?.NotifyValidationStateChanged();
        StateHasChanged();
    }

    private async Task HandleFieldDependencyChanged(string fieldName)
    {
        if (!Configuration.FieldDependencies.TryGetValue(fieldName, out var dependencies))
        {
            return;
        }

        foreach (var dependency in dependencies)
        {
            await dependency.OnDependencyChangedAsync(Model);
        }

        // Re-render once the async callbacks settle, so cascaded model mutations reach the UI
        // without the caller having to force it.
        StateHasChanged();
    }

    private async Task HandleSubmit()
    {
        // Enforce WithSecurity() settings (rate limiting, CSRF) before validation so blocked
        // submissions never reach the application's submit handler.
        if (!await SecurityEnforcer.EnforceAsync(Configuration, Model, SecurityContextId))
        {
            StateHasChanged();
            return;
        }

        // EditForm's OnValidSubmit relies on the synchronous EditContext.Validate(), which returns
        // before async validators finish. Await the full pass explicitly so they can block submit.
        var isValid = _validator is not null
            ? await _validator.ValidateModelAsync()
            : _editContext?.Validate() ?? false;

        if (isValid && OnValidSubmit.HasDelegate)
        {
            await SecurityEnforcer.LogSubmittedAsync(Configuration, Model, SecurityContextId);
            await OnValidSubmit.InvokeAsync(Model);
        }
    }
}
