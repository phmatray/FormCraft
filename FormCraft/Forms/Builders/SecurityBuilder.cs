using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Builder for configuring form security features.
/// </summary>
public class SecurityBuilder<TModel> where TModel : new()
{
    private readonly FormSecurity _security = new();
    private readonly FormBuilder<TModel> _formBuilder;

    public SecurityBuilder(FormBuilder<TModel> formBuilder)
    {
        _formBuilder = formBuilder;
    }

    /// <summary>
    /// Enables encryption for a specific field.
    /// </summary>
    public SecurityBuilder<TModel> EncryptField<TValue>(Expression<Func<TModel, TValue>> fieldExpression)
    {
        var fieldName = GetFieldName(fieldExpression);
        _security.EncryptedFields.Add(fieldName);
        return this;
    }

    /// <summary>
    /// Enables CSRF protection for the form.
    /// </summary>
    public SecurityBuilder<TModel> EnableCsrfProtection(string tokenFieldName = "__RequestVerificationToken")
    {
        _security.IsCsrfProtectionEnabled = true;
        _security.CsrfTokenFieldName = tokenFieldName;
        return this;
    }

    /// <summary>
    /// Configures rate limiting for form submissions.
    /// </summary>
    public SecurityBuilder<TModel> WithRateLimit(int maxAttempts, TimeSpan timeWindow, string identifierType = "IP")
    {
        _security.RateLimit = new RateLimitConfiguration
        {
            MaxAttempts = maxAttempts,
            TimeWindow = timeWindow,
            IdentifierType = identifierType
        };
        return this;
    }

    /// <summary>
    /// Enables audit logging with configuration.
    /// </summary>
    public SecurityBuilder<TModel> EnableAuditLogging(Action<AuditLogConfiguration>? configure = null)
    {
        _security.IsAuditLoggingEnabled = true;
        _security.AuditLog = new AuditLogConfiguration();
        configure?.Invoke(_security.AuditLog);
        return this;
    }

    /// <summary>
    /// Builds the security configuration and returns to the form builder.
    /// </summary>
    public FormBuilder<TModel> And()
    {
        _formBuilder.SetSecurity(_security);
        return _formBuilder;
    }

    private static string GetFieldName<TValue>(Expression<Func<TModel, TValue>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        throw new ArgumentException("Expression must be a member expression");
    }
}