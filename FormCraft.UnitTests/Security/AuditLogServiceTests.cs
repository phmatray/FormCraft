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
    public async Task Should_Redact_Values_Of_Excluded_Fields()
    {
        // Arrange
        var capturingLogger = new CapturingLogger();
        var configuration = new AuditLogConfiguration
        {
            ExcludedFields = { "CreditCard" }
        };
        var service = new ConsoleAuditLogService(capturingLogger, configuration);
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.FieldChanged,
            FormId = "TestForm",
            FieldName = "CreditCard",
            OldValue = "4111111111111111",
            NewValue = "5500005555555559"
        };

        // Act
        await service.LogAsync(entry);

        // Assert
        var logged = capturingLogger.Messages.ShouldHaveSingleItem();
        logged.ShouldNotContain("4111111111111111");
        logged.ShouldNotContain("5500005555555559");
        logged.ShouldContain("[REDACTED]");
        logged.ShouldContain("CreditCard"); // the field name itself is still logged
    }

    [Fact]
    public async Task Should_Redact_Excluded_Fields_In_Additional_Data()
    {
        // Arrange
        var capturingLogger = new CapturingLogger();
        var configuration = new AuditLogConfiguration
        {
            ExcludedFields = { "Password" }
        };
        var service = new ConsoleAuditLogService(capturingLogger, configuration);
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.FormSubmitted,
            FormId = "TestForm",
            AdditionalData = new Dictionary<string, object?>
            {
                ["Password"] = "SuperSecret123!",
                ["FieldCount"] = 10
            }
        };

        // Act
        await service.LogAsync(entry);

        // Assert
        var logged = capturingLogger.Messages.ShouldHaveSingleItem();
        logged.ShouldNotContain("SuperSecret123!");
        logged.ShouldContain("[REDACTED]");
        logged.ShouldContain("10"); // non-excluded additional data is preserved
    }

    [Fact]
    public async Task Should_Not_Redact_Fields_That_Are_Not_Excluded()
    {
        // Arrange
        var capturingLogger = new CapturingLogger();
        var configuration = new AuditLogConfiguration
        {
            ExcludedFields = { "Password" }
        };
        var service = new ConsoleAuditLogService(capturingLogger, configuration);
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.FieldChanged,
            FormId = "TestForm",
            FieldName = "Email",
            OldValue = "old@example.com",
            NewValue = "new@example.com"
        };

        // Act
        await service.LogAsync(entry);

        // Assert
        var logged = capturingLogger.Messages.ShouldHaveSingleItem();
        logged.ShouldContain("old@example.com");
        logged.ShouldContain("new@example.com");
        logged.ShouldNotContain("[REDACTED]");
    }

    [Fact]
    public async Task Should_Not_Mutate_The_Original_Entry_When_Redacting()
    {
        // Arrange
        var capturingLogger = new CapturingLogger();
        var configuration = new AuditLogConfiguration
        {
            ExcludedFields = { "SSN" }
        };
        var service = new ConsoleAuditLogService(capturingLogger, configuration);
        var entry = new AuditLogEntry
        {
            EventType = AuditEventTypes.FieldChanged,
            FormId = "TestForm",
            FieldName = "SSN",
            OldValue = "123-45-6789",
            NewValue = "987-65-4321"
        };

        // Act
        await service.LogAsync(entry);

        // Assert
        entry.OldValue.ShouldBe("123-45-6789");
        entry.NewValue.ShouldBe("987-65-4321");
    }

    private sealed class CapturingLogger : ILogger<ConsoleAuditLogService>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
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
