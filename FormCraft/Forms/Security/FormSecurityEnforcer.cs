using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FormCraft;

/// <summary>
/// Enforces the settings configured via <c>WithSecurity()</c> — rate limiting, CSRF protection,
/// audit logging and field encryption — on behalf of a form container. One instance per rendered
/// form: it holds that form's CSRF token and the user-visible error, nothing else.
/// </summary>
/// <typeparam name="TModel">The form model type.</typeparam>
/// <remarks>
/// <para>
/// Both UI adapters' <c>FormCraftComponent</c> used to carry this pipeline line for line (#291 copied
/// it into the Fluent adapter before the shared-machinery move of #279 had landed). A fix applied to
/// one copy and not the other would have left one adapter quietly weaker, so it lives here once
/// (#321). Presentation stays per adapter: each container renders <see cref="Error"/> in its own
/// banner component.
/// </para>
/// <para>
/// The configuration, model and security context id are taken <b>per call</b> rather than captured at
/// construction, because in a container they are component parameters that can be re-pointed after
/// initialisation, and the pipeline has always read their current values.
/// </para>
/// <para>
/// A form without <c>WithSecurity()</c> resolves no security service at all.
/// </para>
/// </remarks>
public sealed class FormSecurityEnforcer<TModel>
    where TModel : new()
{
    private readonly IServiceProvider _services;
    private readonly ILogger? _logger;
    private string? _csrfToken;

    /// <summary>
    /// Creates the enforcer for one form instance.
    /// </summary>
    /// <param name="services">The service provider the security services are resolved from, lazily.</param>
    /// <param name="logger">
    /// Where security misconfigurations are reported. Containers pass their own logger so the log
    /// category stays the component's. <c>null</c> disables that logging.
    /// </param>
    public FormSecurityEnforcer(IServiceProvider services, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// The message to show the user when security blocked (or cannot allow) a submission;
    /// <c>null</c> when there is nothing to show.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Generates the form's CSRF token when CSRF protection is enabled. Call once, when the form
    /// initialises.
    /// </summary>
    /// <param name="configuration">The form configuration.</param>
    /// <param name="securityContextId">The container's security context id, or <c>null</c>.</param>
    /// <returns>A task that completes when the token has been generated.</returns>
    public async Task InitializeAsync(IFormConfiguration<TModel>? configuration, string? securityContextId)
    {
        if (configuration?.Security?.IsCsrfProtectionEnabled != true)
        {
            return;
        }

        var csrfTokenService = _services.GetService<ICsrfTokenService>();
        if (csrfTokenService == null)
        {
            Error = "CSRF protection is enabled for this form, but no ICsrfTokenService is registered. Call AddFormCraft() (or register a custom ICsrfTokenService) to enable submissions.";
            LogSecurityError("CSRF protection is enabled on form '{FormId}' but no ICsrfTokenService is registered in DI.", EffectiveContextId(securityContextId));
            return;
        }

        _csrfToken = await csrfTokenService.GenerateTokenAsync();
    }

    /// <summary>
    /// Enforces the security settings before a submission is processed. Returns <c>false</c> (and
    /// sets <see cref="Error"/>) when the submission must be blocked.
    /// </summary>
    /// <param name="configuration">The form configuration.</param>
    /// <param name="model">The model being submitted, for the audit entry of a rejection.</param>
    /// <param name="securityContextId">
    /// The container's security context id, or <c>null</c> to fall back to the model type name.
    /// </param>
    /// <returns><c>true</c> when the submission may proceed.</returns>
    public async Task<bool> EnforceAsync(IFormConfiguration<TModel>? configuration, TModel model, string? securityContextId)
    {
        Error = null;
        var security = configuration?.Security;
        if (security == null)
        {
            return true;
        }

        var contextId = EffectiveContextId(securityContextId);

        // Rate limiting runs first so blocked submissions never reach validation.
        if (security.RateLimit is { } rateLimit)
        {
            var rateLimitService = _services.GetService<IRateLimitService>();
            if (rateLimitService == null)
            {
                Error = "Rate limiting is enabled for this form, but no IRateLimitService is registered. Call AddFormCraft() (or register a custom IRateLimitService) to enable submissions.";
                LogSecurityError("Rate limiting is enabled on form '{FormId}' but no IRateLimitService is registered in DI.", contextId);
                return false;
            }

            var rateLimitResult = await rateLimitService.CheckRateLimitAsync(
                contextId, rateLimit.MaxAttempts, rateLimit.TimeWindow);

            if (!rateLimitResult.IsAllowed)
            {
                Error = rateLimitResult.RetryAfter is { } retryAfter && retryAfter > TimeSpan.Zero
                    ? $"Too many submissions. Please try again in {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))} seconds."
                    : "Too many submissions. Please try again later.";
                await LogAuditEventAsync(configuration!, model, contextId, AuditEventTypes.FormRejected, AuditEventTypes.RateLimitExceeded);
                return false;
            }

            await rateLimitService.RecordAttemptAsync(contextId);
        }

        if (security.IsCsrfProtectionEnabled)
        {
            var csrfTokenService = _services.GetService<ICsrfTokenService>();
            if (csrfTokenService == null || _csrfToken == null)
            {
                Error ??= "This form could not be submitted because its security token is missing. Please reload the page and try again.";
                LogSecurityError("CSRF validation could not run on form '{FormId}': service or token missing.", contextId);
                await LogAuditEventAsync(configuration!, model, contextId, AuditEventTypes.FormRejected, AuditEventTypes.CsrfValidationFailed);
                return false;
            }

            if (!await csrfTokenService.ValidateTokenAsync(_csrfToken))
            {
                Error = "Your session could not be verified. Please reload the page and try again.";
                await LogAuditEventAsync(configuration!, model, contextId, AuditEventTypes.FormRejected, AuditEventTypes.CsrfValidationFailed);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Writes the <see cref="AuditEventTypes.FormSubmitted"/> audit entry for a submission that
    /// passed validation, when audit logging is enabled.
    /// </summary>
    /// <param name="configuration">The form configuration.</param>
    /// <param name="model">The submitted model.</param>
    /// <param name="securityContextId">The container's security context id, or <c>null</c>.</param>
    /// <returns>A task that completes when the entry has been written.</returns>
    public Task LogSubmittedAsync(IFormConfiguration<TModel>? configuration, TModel model, string? securityContextId) =>
        configuration == null
            ? Task.CompletedTask
            : LogAuditEventAsync(configuration, model, EffectiveContextId(securityContextId), AuditEventTypes.FormSubmitted);

    /// <summary>
    /// Returns the values of the fields configured for encryption, encrypted with the registered
    /// <see cref="IEncryptionService"/>. The model is never modified.
    /// </summary>
    /// <param name="model">The model holding the plaintext values.</param>
    /// <param name="security">The form's security settings.</param>
    /// <returns>A field-name to ciphertext map covering only the configured fields.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IEncryptionService"/> is registered (call <c>AddFormCraft()</c>).
    /// </exception>
    public IReadOnlyDictionary<string, string?> EncryptConfiguredFields(TModel model, IFormSecurity? security)
    {
        var encryptionService = _services.GetService<IEncryptionService>()
            ?? throw new InvalidOperationException(
                "No IEncryptionService is registered. Call AddFormCraft() (or register a custom IEncryptionService) before using GetEncryptedFieldValues().");

        return encryptionService.EncryptConfiguredFields(model, security);
    }

    /// <summary>
    /// The identifier used for rate limiting and audit entries: the container's security context id
    /// when provided, otherwise the model type name.
    /// </summary>
    private static string EffectiveContextId(string? securityContextId) =>
        string.IsNullOrWhiteSpace(securityContextId) ? typeof(TModel).Name : securityContextId;

    /// <summary>
    /// Writes a submission-related audit entry via the optional <see cref="IAuditLogService"/>,
    /// redacting fields listed in ExcludedFields as well as fields marked for encryption.
    /// </summary>
    private async Task LogAuditEventAsync(
        IFormConfiguration<TModel> configuration, TModel model, string contextId, string eventType, string? reason = null)
    {
        var security = configuration.Security;
        if (security is not { IsAuditLoggingEnabled: true })
        {
            return;
        }

        if (security.AuditLog is { LogSubmissions: false })
        {
            return;
        }

        var auditLogService = _services.GetService<IAuditLogService>();
        if (auditLogService == null)
        {
            return;
        }

        var entry = new AuditLogEntry
        {
            EventType = eventType,
            FormId = contextId,
        };

        if (reason != null)
        {
            entry.AdditionalData["Reason"] = reason;
        }

        var excludedFields = security.AuditLog?.ExcludedFields;
        foreach (var field in configuration.Fields)
        {
            var key = BuildAuditKey(field);
            // Match the bare FieldName and the full-path key, so both "City" and "Address.City"
            // listings redact (#417). Fail closed: any match on either set redacts.
            if (AuditLogConfiguration.Matches(excludedFields, key) ||
                AuditLogConfiguration.Matches(excludedFields, field.FieldName) ||
                AuditLogConfiguration.Matches(security.EncryptedFields, key) ||
                AuditLogConfiguration.Matches(security.EncryptedFields, field.FieldName))
            {
                entry.AdditionalData[key] = "[REDACTED]";
                continue;
            }

            entry.AdditionalData[key] = ReadFieldValue(field, model)?.ToString();
        }

        await auditLogService.LogAsync(entry);
    }

    /// <summary>
    /// Reads a field's value through its binding expression (the getter rendering and validation
    /// share), so a nested binding such as <c>x =&gt; x.Address.City</c> is audited with its real
    /// value. The lookup this replaced resolved <c>field.FieldName</c> — only the expression's last
    /// member — against <typeparamref name="TModel"/>, so every nested field was audited as null.
    /// </summary>
    /// <remarks>
    /// A binding that cannot be evaluated (a null intermediate in a nested path) is audited as null,
    /// as it always was: an audit entry must never turn a submission into an exception.
    /// </remarks>
    private static object? ReadFieldValue(IFieldConfiguration<TModel, object> field, TModel model)
    {
        try
        {
            return FieldValueGetterCache<TModel>.GetOrCompile(field)(model);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Builds the <c>AdditionalData</c> key for a field from its full dotted member path (e.g.
    /// <c>"Address.City"</c>) instead of <see cref="IFieldConfiguration{TModel, TValue}.FieldName"/>'s
    /// last-member-only name, so two fields that end in the same member (<c>x =&gt; x.Home.City</c>
    /// and <c>x =&gt; x.Work.City</c>) no longer collide on one shared key (#406).
    /// </summary>
    private static string BuildAuditKey(IFieldConfiguration<TModel, object> field)
    {
        var expression = field.ValueExpression.Body;
        if (expression is UnaryExpression unary)
        {
            expression = unary.Operand;
        }

        var segments = new Stack<string>();
        while (expression is MemberExpression member)
        {
            segments.Push(member.Member.Name);
            expression = member.Expression;
        }

        // Every field built through the fluent builder already has a MemberExpression body (enforced
        // by FieldConfiguration<TModel, TValue>'s constructor), so this branch is unreachable for
        // those. IFormConfiguration<TModel>.Fields is a public, mutable list, though, and a
        // hand-rolled IFieldConfiguration<TModel, TValue> (see that interface's own XML doc example)
        // is not bound by that guard — this is its compatibility fallback, preserving the pre-#406
        // FieldName-only key for any such implementation.
        return segments.Count == 0 ? field.FieldName : string.Join(".", segments);
    }

    private void LogSecurityError(string message, params object?[] args)
    {
#pragma warning disable CA2254 // Template is a constant supplied by the callers above
        _logger?.LogError(message, args);
#pragma warning restore CA2254
    }
}
