using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FormCraft;

/// <summary>
/// Simple audit log service that writes to console/logger.
/// For production use, consider writing to a database or dedicated logging service.
/// </summary>
/// <remarks>
/// When an <see cref="AuditLogConfiguration"/> is supplied, entries for fields listed in
/// <see cref="AuditLogConfiguration.ExcludedFields"/> are redacted before serialization:
/// <see cref="AuditLogEntry.OldValue"/>, <see cref="AuditLogEntry.NewValue"/> and any matching
/// <see cref="AuditLogEntry.AdditionalData"/> entries are replaced with <c>[REDACTED]</c>.
/// </remarks>
public class ConsoleAuditLogService : IAuditLogService
{
    private const string RedactedValue = "[REDACTED]";

    private readonly ILogger<ConsoleAuditLogService> _logger;
    private readonly AuditLogConfiguration? _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleAuditLogService"/> class.
    /// </summary>
    /// <param name="logger">The logger used to emit audit entries.</param>
    /// <param name="configuration">
    /// Optional audit log configuration. When provided, fields listed in
    /// <see cref="AuditLogConfiguration.ExcludedFields"/> have their values redacted before logging.
    /// </param>
    public ConsoleAuditLogService(ILogger<ConsoleAuditLogService> logger, AuditLogConfiguration? configuration = null)
    {
        _logger = logger;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public Task LogAsync(AuditLogEntry entry)
    {
        var logLevel = entry.EventType switch
        {
            AuditEventTypes.ValidationError => LogLevel.Warning,
            AuditEventTypes.RateLimitExceeded => LogLevel.Warning,
            AuditEventTypes.CsrfValidationFailed => LogLevel.Warning,
            _ => LogLevel.Information
        };

        var json = JsonSerializer.Serialize(Redact(entry), _jsonOptions);
        _logger.Log(logLevel, "FormCraft Audit: {AuditEntry}", json);

        return Task.CompletedTask;
    }

    private AuditLogEntry Redact(AuditLogEntry entry)
    {
        var excludedFields = _configuration?.ExcludedFields;
        if (excludedFields == null || excludedFields.Count == 0)
        {
            return entry;
        }

        var isFieldExcluded = entry.FieldName != null && excludedFields.Contains(entry.FieldName);
        var hasExcludedAdditionalData = entry.AdditionalData.Keys.Any(excludedFields.Contains);
        if (!isFieldExcluded && !hasExcludedAdditionalData)
        {
            return entry;
        }

        // Never mutate the caller's entry — log a redacted copy instead.
        return new AuditLogEntry
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            EventType = entry.EventType,
            FormId = entry.FormId,
            UserId = entry.UserId,
            IpAddress = entry.IpAddress,
            FieldName = entry.FieldName,
            OldValue = isFieldExcluded && entry.OldValue != null ? RedactedValue : entry.OldValue,
            NewValue = isFieldExcluded && entry.NewValue != null ? RedactedValue : entry.NewValue,
            AdditionalData = entry.AdditionalData.ToDictionary(
                kvp => kvp.Key,
                kvp => excludedFields.Contains(kvp.Key) ? RedactedValue : kvp.Value)
        };
    }
}
