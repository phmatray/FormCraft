namespace FormCraft;

/// <summary>
/// Service for audit logging.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs a form event.
    /// </summary>
    /// <param name="entry">The audit log entry.</param>
    Task LogAsync(AuditLogEntry entry);
}

/// <summary>
/// Represents an audit log entry.
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of event (e.g., FieldChanged, ValidationError, FormSubmitted).
    /// </summary>
    public string EventType { get; set; } = "";

    /// <summary>
    /// Form identifier.
    /// </summary>
    public string FormId { get; set; } = "";

    /// <summary>
    /// User identifier.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// IP address of the user.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Field name (for field-specific events).
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Old value (for field change events).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value (for field change events).
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Additional data as key-value pairs.
    /// </summary>
    public Dictionary<string, object?> AdditionalData { get; set; } = new();
}

/// <summary>
/// Common audit event types.
/// </summary>
public static class AuditEventTypes
{
    public const string FieldChanged = "FieldChanged";
    public const string ValidationError = "ValidationError";
    public const string FormSubmitted = "FormSubmitted";
    public const string FormLoaded = "FormLoaded";
    public const string RateLimitExceeded = "RateLimitExceeded";
    public const string CsrfValidationFailed = "CsrfValidationFailed";
}