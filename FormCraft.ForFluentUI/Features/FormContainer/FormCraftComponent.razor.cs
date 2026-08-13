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

    private EditContext? _editContext;
    // The shared validator from core since #279, not this adapter's own copy.
    private DynamicFormValidator<TModel>? _validator;
    private string? _csrfToken;
    private string? _securityError;

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

        await InitializeSecurityAsync();
        await base.OnInitializedAsync();
    }

    // ---------------------------------------------------------------------------------------
    // Security enforcement (#278).
    //
    // ⚠️ Deliberate, tracked duplication. The members below are a line-for-line port of
    // FormCraft.ForMudBlazor.FormCraftComponent<TModel>'s security pipeline. Not one of them
    // references a UI type, so the pair is a candidate for the shared-machinery move discussed on
    // #278 - which had not landed when this was written, and the plan's instruction for that case
    // is to copy and say so rather than block on it.
    //
    // Until that move happens, treat the two copies as one unit: a fix applied here must be
    // applied to the MudBlazor container too, or the adapters diverge on security behaviour,
    // which is the one place a silent divergence is least acceptable. The behavioural contract is
    // pinned on both sides by matching suites (FormCraftComponentSecurityTests).
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Identifier used for rate limiting and audit log entries:
    /// <see cref="SecurityContextId"/> when provided, otherwise the model type name.
    /// </summary>
    private string EffectiveSecurityContextId =>
        string.IsNullOrWhiteSpace(SecurityContextId) ? typeof(TModel).Name : SecurityContextId;

    private async Task InitializeSecurityAsync()
    {
        if (Configuration?.Security?.IsCsrfProtectionEnabled != true)
        {
            return;
        }

        var csrfTokenService = ServiceProvider.GetService<ICsrfTokenService>();
        if (csrfTokenService == null)
        {
            _securityError = "CSRF protection is enabled for this form, but no ICsrfTokenService is registered. Call AddFormCraft() (or register a custom ICsrfTokenService) to enable submissions.";
            LogSecurityError("CSRF protection is enabled on form '{FormId}' but no ICsrfTokenService is registered in DI.", EffectiveSecurityContextId);
            return;
        }

        _csrfToken = await csrfTokenService.GenerateTokenAsync();
    }

    /// <summary>
    /// Enforces the security settings configured via <c>WithSecurity()</c> before a submission is
    /// processed. Returns false (and sets a user-visible error) when the submission must be blocked.
    /// </summary>
    private async Task<bool> EnforceSecurityAsync()
    {
        _securityError = null;
        var security = Configuration?.Security;
        if (security == null)
        {
            return true;
        }

        // Rate limiting runs first so blocked submissions never reach validation.
        if (security.RateLimit is { } rateLimit)
        {
            var rateLimitService = ServiceProvider.GetService<IRateLimitService>();
            if (rateLimitService == null)
            {
                _securityError = "Rate limiting is enabled for this form, but no IRateLimitService is registered. Call AddFormCraft() (or register a custom IRateLimitService) to enable submissions.";
                LogSecurityError("Rate limiting is enabled on form '{FormId}' but no IRateLimitService is registered in DI.", EffectiveSecurityContextId);
                return false;
            }

            var rateLimitResult = await rateLimitService.CheckRateLimitAsync(
                EffectiveSecurityContextId, rateLimit.MaxAttempts, rateLimit.TimeWindow);

            if (!rateLimitResult.IsAllowed)
            {
                _securityError = rateLimitResult.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero
                    ? $"Too many submissions. Please try again in {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))} seconds."
                    : "Too many submissions. Please try again later.";
                await LogSubmissionAuditEventAsync(AuditEventTypes.FormRejected, AuditEventTypes.RateLimitExceeded);
                return false;
            }

            await rateLimitService.RecordAttemptAsync(EffectiveSecurityContextId);
        }

        if (security.IsCsrfProtectionEnabled)
        {
            var csrfTokenService = ServiceProvider.GetService<ICsrfTokenService>();
            if (csrfTokenService == null || _csrfToken == null)
            {
                _securityError ??= "This form could not be submitted because its security token is missing. Please reload the page and try again.";
                LogSecurityError("CSRF validation could not run on form '{FormId}': service or token missing.", EffectiveSecurityContextId);
                await LogSubmissionAuditEventAsync(AuditEventTypes.FormRejected, AuditEventTypes.CsrfValidationFailed);
                return false;
            }

            if (!await csrfTokenService.ValidateTokenAsync(_csrfToken))
            {
                _securityError = "Your session could not be verified. Please reload the page and try again.";
                await LogSubmissionAuditEventAsync(AuditEventTypes.FormRejected, AuditEventTypes.CsrfValidationFailed);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Writes a submission-related audit entry via the optional <see cref="IAuditLogService"/>,
    /// redacting fields listed in ExcludedFields as well as fields marked for encryption.
    /// </summary>
    private async Task LogSubmissionAuditEventAsync(string eventType, string? reason = null)
    {
        var security = Configuration?.Security;
        if (security is not { IsAuditLoggingEnabled: true })
        {
            return;
        }

        if (security.AuditLog is { LogSubmissions: false })
        {
            return;
        }

        var auditLogService = ServiceProvider.GetService<IAuditLogService>();
        if (auditLogService == null)
        {
            return;
        }

        var entry = new AuditLogEntry
        {
            EventType = eventType,
            FormId = EffectiveSecurityContextId,
        };

        if (reason != null)
        {
            entry.AdditionalData["Reason"] = reason;
        }

        var excludedFields = security.AuditLog?.ExcludedFields;
        foreach (var field in Configuration!.Fields)
        {
            if (excludedFields?.Contains(field.FieldName) == true ||
                security.EncryptedFields.Contains(field.FieldName))
            {
                entry.AdditionalData[field.FieldName] = "[REDACTED]";
                continue;
            }

            var property = typeof(TModel).GetProperty(field.FieldName);
            entry.AdditionalData[field.FieldName] = property?.GetValue(Model)?.ToString();
        }

        await auditLogService.LogAsync(entry);
    }

    private void LogSecurityError(string message, params object?[] args)
    {
        var logger = ServiceProvider.GetService<ILogger<FormCraftComponent<TModel>>>();
#pragma warning disable CA2254 // Template is a constant supplied by the callers above
        logger?.LogError(message, args);
#pragma warning restore CA2254
    }

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
    public IReadOnlyDictionary<string, string?> GetEncryptedFieldValues()
    {
        var encryptionService = ServiceProvider.GetService<IEncryptionService>()
            ?? throw new InvalidOperationException(
                "No IEncryptionService is registered. Call AddFormCraft() (or register a custom IEncryptionService) before using GetEncryptedFieldValues().");

        return encryptionService.EncryptConfiguredFields(Model, Configuration?.Security);
    }

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
                var property = typeof(TModel).GetProperty(field.FieldName);
                if (property == null)
                {
                    return;
                }

                var templateContext = new FieldContext<TModel, object>(
                    Model,
                    field,
                    _editContext,
                    () => property.GetValue(Model)!,
                    newValue => _ = UpdateFieldValue(field.FieldName, newValue),
                    EventCallback.Factory.Create<object>(this, newValue => UpdateFieldValue(field.FieldName, newValue)));

                builder.AddContent(0, field.CustomTemplate(templateContext));
                return;
            }

            // Single rendering path: every field is dispatched through the IFieldRendererService
            // registry, which resolves the Fluent renderers registered by AddFormCraftFluentUI().
            builder.AddContent(0, FieldRendererService.RenderField(
                Model,
                field,
                EventCallback.Factory.Create<object?>(this, val => UpdateFieldValue(field.FieldName, val)),
                EventCallback.Factory.Create(this, () => HandleFieldDependencyChanged(field.FieldName))));
        };
    }

    private async Task UpdateFieldValue(string fieldName, object? value)
    {
        var property = typeof(TModel).GetProperty(fieldName);
        if (property is null)
        {
            return;
        }

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
                // Conversion failed - hand the value over as-is and let validation report it.
            }
        }

        property.SetValue(Model, convertedValue);

        // Notify the EditContext so field-level validation runs and stale errors clear as soon as
        // the user corrects the value.
        _editContext?.NotifyFieldChanged(_editContext.Field(fieldName));

        if (OnFieldChanged.HasDelegate)
        {
            await OnFieldChanged.InvokeAsync((fieldName, convertedValue));
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
        if (!await EnforceSecurityAsync())
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
            await LogSubmissionAuditEventAsync(AuditEventTypes.FormSubmitted);
            await OnValidSubmit.InvokeAsync(Model);
        }
    }
}
