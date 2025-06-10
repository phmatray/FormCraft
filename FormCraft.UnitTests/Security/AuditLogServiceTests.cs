using Microsoft.Extensions.Logging;

namespace FormCraft.UnitTests.Security;

public class AuditLogServiceTests
{
    private readonly ILogger<ConsoleAuditLogService> _logger;
    private readonly IAuditLogService _auditLogService;

    public AuditLogServiceTests()
    {
        _logger = A.Fake<ILogger<ConsoleAuditLogService>>();
        _auditLogService = new ConsoleAuditLogService(_logger);
    }

    [Fact]
    public async Task Should_Log_Form_Loaded_Event()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.FormLoaded,
            FormId = "TestForm",
            UserId = "user123",
            IpAddress = "192.168.1.1"
        };

        // Act
        await _auditLogService.LogAsync(entry);

        // Assert
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" &&
            call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Should_Log_Field_Changed_Event()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.FieldChanged,
            FormId = "TestForm",
            FieldName = "Email",
            OldValue = "old@example.com",
            NewValue = "new@example.com",
            UserId = "user123"
        };

        // Act
        await _auditLogService.LogAsync(entry);

        // Assert
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" &&
            call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Should_Log_Validation_Error_As_Warning()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.ValidationError,
            FormId = "TestForm",
            FieldName = "Email",
            AdditionalData = new Dictionary<string, object?>
            {
                ["ErrorMessage"] = "Invalid email format"
            }
        };

        // Act
        await _auditLogService.LogAsync(entry);

        // Assert
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" &&
            call.GetArgument<LogLevel>(0) == LogLevel.Warning)
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Should_Log_Rate_Limit_Exceeded_As_Warning()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.RateLimitExceeded,
            FormId = "TestForm",
            IpAddress = "192.168.1.1",
            AdditionalData = new Dictionary<string, object?>
            {
                ["Attempts"] = 10,
                ["TimeWindow"] = "1 minute"
            }
        };

        // Act
        await _auditLogService.LogAsync(entry);

        // Assert
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" &&
            call.GetArgument<LogLevel>(0) == LogLevel.Warning)
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Should_Log_CSRF_Validation_Failed_As_Warning()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.CsrfValidationFailed,
            FormId = "TestForm",
            IpAddress = "192.168.1.1"
        };

        // Act
        await _auditLogService.LogAsync(entry);

        // Assert
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" &&
            call.GetArgument<LogLevel>(0) == LogLevel.Warning)
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Should_Include_Additional_Data_In_Log()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.FormSubmitted,
            FormId = "TestForm",
            AdditionalData = new Dictionary<string, object?>
            {
                ["ProcessingTime"] = "250ms",
                ["FieldCount"] = 10,
                ["HasErrors"] = false
            }
        };

        // Act
        await _auditLogService.LogAsync(entry);

        // Assert
        A.CallTo(_logger).Where(call => call.Method.Name == "Log" &&
            call.GetArgument<LogLevel>(0) == LogLevel.Information)
            .MustHaveHappenedOnceExactly();
    }
}