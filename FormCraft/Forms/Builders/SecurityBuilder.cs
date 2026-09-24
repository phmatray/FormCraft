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
    /// Enables encryption for a specific field, stored in <see cref="IFormSecurity.EncryptedFields"/>
    /// under its full dotted path (<c>x =&gt; x.Address.City</c> → <c>"Address.City"</c>).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldExpression"/> is not a chain of public, readable properties on
    /// <typeparamref name="TModel"/> ending in a <see cref="string"/>. Encryption fails closed: a field
    /// that could not be reached would otherwise stay in plaintext without any error (#423).
    /// </exception>
    public SecurityBuilder<TModel> EncryptField<TValue>(Expression<Func<TModel, TValue>> fieldExpression)
    {
        ArgumentNullException.ThrowIfNull(fieldExpression);
        var path = MemberPathResolver.GetDottedPath(fieldExpression)
            ?? throw new ArgumentException(
                $"EncryptField expects a member access on the model, such as x => x.Address.City; got '{fieldExpression}'.",
                nameof(fieldExpression));
        MemberPathResolver.ResolveStringProperty(typeof(TModel), path);
        _security.EncryptedFields.Add(path);
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
}
