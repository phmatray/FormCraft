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
    /// Stable identifier used for security enforcement (rate limiting and audit log
    /// entries) configured via <c>WithSecurity()</c>. Set this to a per-user or
    /// per-session value (e.g. user id, circuit id, IP address) so limits are not
    /// shared across all users. Defaults to the model type name.
    /// </summary>
    [Parameter]
    public string? SecurityContextId { get; set; }

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    private EditContext? _editContext;
    private DynamicFormValidator<TModel>? _validator;
    private string? _csrfToken;
    private string? _securityError;
    private IGroupedFormConfiguration<TModel>? GroupedConfiguration => Configuration as IGroupedFormConfiguration<TModel>;
    private ICollectionFormConfiguration<TModel>? CollectionConfiguration => Configuration as ICollectionFormConfiguration<TModel>;

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
        await InitializeSecurityAsync();
        await base.OnInitializedAsync();
    }

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
    /// Enforces the security settings configured via <c>WithSecurity()</c> before a
    /// submission is processed. Returns false (and sets a user-visible error) when the
    /// submission must be blocked.
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
    /// <see cref="IEncryptionService"/>, so applications can persist them safely in one
    /// call. The bound model is never modified.
    /// </summary>
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
                var property = typeof(TModel).GetProperty(field.FieldName);
                if (property == null) return;

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

            // Single rendering path: every field is dispatched through the
            // FieldRendererService registry (#148). Type- and configuration-based
            // selection (text, numeric, boolean, date, select, LOV, lookup,
            // autocomplete, file upload, custom renderers) lives in the
            // registered IFieldRenderer implementations.
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
        if (property != null)
        {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = value;

            // Convert value to the target type if necessary
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

            property.SetValue(Model, convertedValue);

            // Notify the EditContext so field-level validation runs and stale
            // error messages clear as soon as the user corrects the value.
            _editContext?.NotifyFieldChanged(_editContext.Field(fieldName));

            if (OnFieldChanged.HasDelegate)
            {
                await OnFieldChanged.InvokeAsync((fieldName, convertedValue));
            }

            // Handle dependencies
            await HandleFieldDependencyChanged(fieldName);

            StateHasChanged();
        }
    }

    private Task HandleFieldDependencyChanged(string fieldName)
    {
        if (Configuration.FieldDependencies.TryGetValue(fieldName, out var dependencies))
        {
            foreach (IFieldDependency<TModel> dependency in dependencies)
            {
                dependency.OnDependencyChanged(Model);
            }
        }

        return Task.CompletedTask;
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
        if (!await EnforceSecurityAsync())
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
            await LogSubmissionAuditEventAsync(AuditEventTypes.FormSubmitted);
            await OnValidSubmit.InvokeAsync(Model);
        }
    }
}