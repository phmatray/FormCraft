namespace FormCraft;

/// <summary>
/// Default implementation of form security configuration.
/// </summary>
public class FormSecurity : IFormSecurity
{
    public HashSet<string> EncryptedFields { get; } = new();
    public bool IsCsrfProtectionEnabled { get; set; }
    public string CsrfTokenFieldName { get; set; } = "__RequestVerificationToken";
    public RateLimitConfiguration? RateLimit { get; set; }
    public bool IsAuditLoggingEnabled { get; set; }
    public AuditLogConfiguration? AuditLog { get; set; }
}