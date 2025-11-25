namespace FormCraft.UnitTests.Security;

public class SecurityBuilderTests
{
    private class TestModel
    {
        public string Name { get; set; } = "";
        public string SSN { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [Fact]
    public void Should_Add_Encrypted_Fields()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security
                .EncryptField(x => x.SSN)
                .EncryptField(x => x.Password))
            .Build();

        // Assert
        config.Security.ShouldNotBeNull();
        config.Security.EncryptedFields.ShouldContain("SSN");
        config.Security.EncryptedFields.ShouldContain("Password");
        config.Security.EncryptedFields.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_Enable_CSRF_Protection()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security
                .EnableCsrfProtection())
            .Build();

        // Assert
        config.Security.ShouldNotBeNull();
        config.Security.IsCsrfProtectionEnabled.ShouldBeTrue();
        config.Security.CsrfTokenFieldName.ShouldBe("__RequestVerificationToken");
    }

    [Fact]
    public void Should_Enable_CSRF_With_Custom_Field_Name()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security
                .EnableCsrfProtection("_csrf_token"))
            .Build();

        // Assert
        config.Security.ShouldNotBeNull();
        config.Security.IsCsrfProtectionEnabled.ShouldBeTrue();
        config.Security.CsrfTokenFieldName.ShouldBe("_csrf_token");
    }

    [Fact]
    public void Should_Configure_Rate_Limiting()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security
                .WithRateLimit(5, TimeSpan.FromMinutes(1)))
            .Build();

        // Assert
        config.Security.ShouldNotBeNull();
        config.Security.RateLimit.ShouldNotBeNull();
        config.Security.RateLimit.MaxAttempts.ShouldBe(5);
        config.Security.RateLimit.TimeWindow.ShouldBe(TimeSpan.FromMinutes(1));
        config.Security.RateLimit.IdentifierType.ShouldBe("IP");
    }

    [Fact]
    public void Should_Configure_Rate_Limiting_With_Custom_Identifier()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security
                .WithRateLimit(10, TimeSpan.FromHours(1), "UserId"))
            .Build();

        // Assert
        config.Security.ShouldNotBeNull();
        config.Security.RateLimit.ShouldNotBeNull();
        config.Security.RateLimit.MaxAttempts.ShouldBe(10);
        config.Security.RateLimit.TimeWindow.ShouldBe(TimeSpan.FromHours(1));
        config.Security.RateLimit.IdentifierType.ShouldBe("UserId");
    }

    [Fact]
    public void Should_Enable_Audit_Logging_With_Defaults()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security
                .EnableAuditLogging())
            .Build();

        // Assert
        config.Security.ShouldNotBeNull();
        config.Security.IsAuditLoggingEnabled.ShouldBeTrue();
        config.Security.AuditLog.ShouldNotBeNull();
        config.Security.AuditLog.LogFieldChanges.ShouldBeTrue();
        config.Security.AuditLog.LogValidationErrors.ShouldBeTrue();
        config.Security.AuditLog.LogSubmissions.ShouldBeTrue();
        config.Security.AuditLog.ExcludedFields.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Configure_Audit_Logging_With_Custom_Settings()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security
                .EnableAuditLogging(audit =>
                {
                    audit.LogFieldChanges = false;
                    audit.LogValidationErrors = true;
                    audit.LogSubmissions = true;
                    audit.ExcludedFields.Add("Password");
                    audit.ExcludedFields.Add("SSN");
                }))
            .Build();

        // Assert
        config.Security.ShouldNotBeNull();
        config.Security.IsAuditLoggingEnabled.ShouldBeTrue();
        config.Security.AuditLog.ShouldNotBeNull();
        config.Security.AuditLog.LogFieldChanges.ShouldBeFalse();
        config.Security.AuditLog.LogValidationErrors.ShouldBeTrue();
        config.Security.AuditLog.LogSubmissions.ShouldBeTrue();
        config.Security.AuditLog.ExcludedFields.ShouldContain("Password");
        config.Security.AuditLog.ExcludedFields.ShouldContain("SSN");
    }

    [Fact]
    public void Should_Configure_Multiple_Security_Features()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security
                .EncryptField(x => x.SSN)
                .EncryptField(x => x.Password)
                .EnableCsrfProtection()
                .WithRateLimit(3, TimeSpan.FromMinutes(5))
                .EnableAuditLogging(audit =>
                {
                    audit.ExcludedFields.Add("Password");
                }))
            .Build();

        // Assert
        config.Security.ShouldNotBeNull();

        // Encryption
        config.Security.EncryptedFields.Count.ShouldBe(2);
        config.Security.EncryptedFields.ShouldContain("SSN");
        config.Security.EncryptedFields.ShouldContain("Password");

        // CSRF
        config.Security.IsCsrfProtectionEnabled.ShouldBeTrue();

        // Rate Limiting
        config.Security.RateLimit.ShouldNotBeNull();
        config.Security.RateLimit.MaxAttempts.ShouldBe(3);
        config.Security.RateLimit.TimeWindow.ShouldBe(TimeSpan.FromMinutes(5));

        // Audit Logging
        config.Security.IsAuditLoggingEnabled.ShouldBeTrue();
        config.Security.AuditLog.ShouldNotBeNull();
        config.Security.AuditLog.ExcludedFields.ShouldContain("Password");
    }

    [Fact]
    public void Should_Work_With_Regular_Form_Configuration()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .Required())
            .AddField(x => x.SSN, field => field
                .WithLabel("Social Security Number")
                .Required())
            .WithSecurity(security => security
                .EncryptField(x => x.SSN))
            .Build();

        // Assert
        config.Fields.Count.ShouldBe(2);
        config.Security.ShouldNotBeNull();
        config.Security.EncryptedFields.ShouldContain("SSN");
    }
}