namespace FormCraft;

/// <summary>
/// Defines security features for a form.
/// </summary>
public interface IFormSecurity
{
    /// <summary>
    /// Gets the fields that should be encrypted.
    /// </summary>
    HashSet<string> EncryptedFields { get; }

    /// <summary>
    /// Gets whether CSRF protection is enabled.
    /// </summary>
    bool IsCsrfProtectionEnabled { get; }

    /// <summary>
    /// Gets the CSRF token field name.
    /// </summary>
    string CsrfTokenFieldName { get; }

    /// <summary>
    /// Gets the rate limiting configuration.
    /// </summary>
    RateLimitConfiguration? RateLimit { get; }

    /// <summary>
    /// Gets whether audit logging is enabled.
    /// </summary>
    bool IsAuditLoggingEnabled { get; }

    /// <summary>
    /// Gets the audit log configuration.
    /// </summary>
    AuditLogConfiguration? AuditLog { get; }
}

/// <summary>
/// Configuration for rate limiting.
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Maximum number of submissions allowed.
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Time window for rate limiting.
    /// </summary>
    public TimeSpan TimeWindow { get; set; }

    /// <summary>
    /// Identifier used for rate limiting (e.g., IP address, user ID).
    /// </summary>
    public string IdentifierType { get; set; } = "IP";
}

/// <summary>
/// Configuration for audit logging.
/// </summary>
public class AuditLogConfiguration
{
    /// <summary>
    /// Whether to log field changes.
    /// </summary>
    public bool LogFieldChanges { get; set; } = true;

    /// <summary>
    /// Whether to log validation errors.
    /// </summary>
    public bool LogValidationErrors { get; set; } = true;

    /// <summary>
    /// Whether to log form submissions.
    /// </summary>
    public bool LogSubmissions { get; set; } = true;

    /// <summary>
    /// Fields to exclude from logging (e.g., passwords).
    /// </summary>
    /// <remarks>
    /// Audit keys for nested fields are full dotted paths (<c>"Address.City"</c>). An entry here
    /// matches a key either exactly or by the key's last segment, so a bare member name
    /// (<c>"City"</c>) redacts every field ending in that member, while a full path redacts only
    /// that one field.
    /// </remarks>
    public HashSet<string> ExcludedFields { get; set; } = new();

    /// <summary>
    /// Whether <paramref name="key"/> — a field name or dotted audit path — is named by
    /// <paramref name="fields"/>, either exactly or by its last dot-separated segment (#417).
    /// </summary>
    internal static bool Matches(HashSet<string>? fields, string? key)
    {
        if (fields is not { Count: > 0 } || key is null)
        {
            return false;
        }

        return fields.Contains(key) || fields.Contains(key[(key.LastIndexOf('.') + 1)..]);
    }
}
