using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FormCraft;

/// <summary>
/// Simple audit log service that writes to console/logger.
/// For production use, consider writing to a database or dedicated logging service.
/// </summary>
public class ConsoleAuditLogService : IAuditLogService
{
    private readonly ILogger<ConsoleAuditLogService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConsoleAuditLogService(ILogger<ConsoleAuditLogService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public Task LogAsync(AuditLogEntry entry)
    {
        var logLevel = entry.EventType switch
        {
            AuditEventTypes.ValidationError => LogLevel.Warning,
            AuditEventTypes.RateLimitExceeded => LogLevel.Warning,
            AuditEventTypes.CsrfValidationFailed => LogLevel.Warning,
            _ => LogLevel.Information
        };

        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        _logger.Log(logLevel, "FormCraft Audit: {AuditEntry}", json);

        return Task.CompletedTask;
    }
}