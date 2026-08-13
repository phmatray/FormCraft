using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>
/// The Fluent container must enforce the settings configured via <c>WithSecurity()</c> - rate
/// limiting, CSRF, audit logging - automatically, with no app-side code in the submit handler.
/// </summary>
/// <remarks>
/// Mirrors <c>FormCraft.ForMudBlazor.UnitTests.Components.FormCraftComponentSecurityTests</c>
/// assertion for assertion: parity here is behavioural, not structural (#278). Deleting the old
/// <c>NotSupportedException</c> guard is not what makes this adapter secure - these tests are.
/// </remarks>
public class FormCraftComponentSecurityTests : FluentUITestBase
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ICsrfTokenService _csrfTokenService;
    private readonly IAuditLogService _auditLogService;
    private readonly List<AuditLogEntry> _auditEntries = new();

    public FormCraftComponentSecurityTests()
    {
        _rateLimitService = A.Fake<IRateLimitService>();
        A.CallTo(() => _rateLimitService.CheckRateLimitAsync(A<string>._, A<int>._, A<TimeSpan>._))
            .Returns(new RateLimitResult { IsAllowed = true, RemainingAttempts = 4 });

        _csrfTokenService = A.Fake<ICsrfTokenService>();
        A.CallTo(() => _csrfTokenService.GenerateTokenAsync()).Returns("test-csrf-token");
        A.CallTo(() => _csrfTokenService.ValidateTokenAsync(A<string>._)).Returns(true);

        _auditLogService = A.Fake<IAuditLogService>();
        A.CallTo(() => _auditLogService.LogAsync(A<AuditLogEntry>._))
            .Invokes((AuditLogEntry entry) => _auditEntries.Add(entry))
            .Returns(Task.CompletedTask);

        // Later registrations win over the AddFormCraft() defaults for GetService<T>().
        Services.AddSingleton(_rateLimitService);
        Services.AddSingleton(_csrfTokenService);
        Services.AddSingleton(_auditLogService);
    }

    [Fact]
    public void Form_With_Security_Configured_Should_Render_Instead_Of_Throwing()
    {
        // Arrange - the configuration the adapter used to refuse outright
        var config = BuildConfig(security => security.WithRateLimit(5, TimeSpan.FromMinutes(1)));

        // Act
        var component = RenderForm(config, _ => { });

        // Assert
        component.Find("form").ShouldNotBeNull();
    }

    [Fact]
    public async Task Submit_Should_Be_Blocked_And_Show_Message_When_Rate_Limited()
    {
        // Arrange
        A.CallTo(() => _rateLimitService.CheckRateLimitAsync(A<string>._, A<int>._, A<TimeSpan>._))
            .Returns(new RateLimitResult { IsAllowed = false, RetryAfter = TimeSpan.FromSeconds(30) });

        var submitted = false;
        var component = RenderForm(
            BuildConfig(security => security
                .WithRateLimit(5, TimeSpan.FromMinutes(1))
                .EnableAuditLogging()),
            _ => submitted = true);

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        submitted.ShouldBeFalse();
        component.WaitForAssertion(() =>
            component.Find("[data-testid=formcraft-security-error]").TextContent
                .ShouldContain("Too many submissions"));
        A.CallTo(() => _rateLimitService.RecordAttemptAsync(A<string>._)).MustNotHaveHappened();

        var rejection = _auditEntries.ShouldHaveSingleItem();
        rejection.EventType.ShouldBe(AuditEventTypes.FormRejected);
        rejection.AdditionalData["Reason"].ShouldBe(AuditEventTypes.RateLimitExceeded);
    }

    [Fact]
    public async Task Submit_Should_Pass_And_Record_Attempt_When_Rate_Limit_Allows()
    {
        // Arrange
        var submitted = false;
        var component = RenderForm(
            BuildConfig(security => security.WithRateLimit(5, TimeSpan.FromMinutes(1))),
            _ => submitted = true);

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        component.WaitForAssertion(() => submitted.ShouldBeTrue());
        A.CallTo(() => _rateLimitService.CheckRateLimitAsync("SecurityTestModel", 5, TimeSpan.FromMinutes(1)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _rateLimitService.RecordAttemptAsync("SecurityTestModel"))
            .MustHaveHappenedOnceExactly();
        component.FindAll("[data-testid=formcraft-security-error]").ShouldBeEmpty();
    }

    [Fact]
    public async Task Submit_Should_Use_SecurityContextId_As_Rate_Limit_Identifier()
    {
        // Arrange
        var component = RenderForm(
            BuildConfig(security => security.WithRateLimit(5, TimeSpan.FromMinutes(1))),
            _ => { },
            parameters => parameters.Add(p => p.SecurityContextId, "user-42"));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        A.CallTo(() => _rateLimitService.CheckRateLimitAsync("user-42", 5, TimeSpan.FromMinutes(1)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _rateLimitService.RecordAttemptAsync("user-42"))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Submit_Should_Be_Blocked_When_Csrf_Validation_Fails()
    {
        // Arrange
        A.CallTo(() => _csrfTokenService.ValidateTokenAsync(A<string>._)).Returns(false);

        var submitted = false;
        var component = RenderForm(
            BuildConfig(security => security
                .EnableCsrfProtection()
                .EnableAuditLogging()),
            _ => submitted = true);

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        submitted.ShouldBeFalse();
        component.WaitForAssertion(() =>
            component.Find("[data-testid=formcraft-security-error]").TextContent
                .ShouldContain("could not be verified"));

        var rejection = _auditEntries.ShouldHaveSingleItem();
        rejection.EventType.ShouldBe(AuditEventTypes.FormRejected);
        rejection.AdditionalData["Reason"].ShouldBe(AuditEventTypes.CsrfValidationFailed);
    }

    [Fact]
    public async Task Submit_Should_Pass_When_Csrf_Token_Is_Valid()
    {
        // Arrange
        var submitted = false;
        var component = RenderForm(
            BuildConfig(security => security.EnableCsrfProtection()),
            _ => submitted = true);

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        component.WaitForAssertion(() => submitted.ShouldBeTrue());
        A.CallTo(() => _csrfTokenService.GenerateTokenAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _csrfTokenService.ValidateTokenAsync("test-csrf-token"))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Component_Should_Show_Error_Instead_Of_Crashing_When_CsrfService_Is_Missing()
    {
        // Arrange - CSRF enabled but no ICsrfTokenService registered at all
        Services.RemoveAll<ICsrfTokenService>();

        // Act
        var component = RenderForm(
            BuildConfig(security => security.EnableCsrfProtection()),
            _ => { });

        // Assert
        component.Find("[data-testid=formcraft-security-error]").TextContent
            .ShouldContain("ICsrfTokenService");
    }

    [Fact]
    public async Task Submit_Should_Log_FormSubmitted_With_Excluded_And_Encrypted_Fields_Redacted()
    {
        // Arrange
        var model = new SecurityTestModel { Name = "John", Password = "hunter2", Ssn = "123-45-6789" };
        var config = FormBuilder<SecurityTestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .AddField(x => x.Password, field => field.WithLabel("Password"))
            .AddField(x => x.Ssn, field => field.WithLabel("SSN"))
            .WithSecurity(security => security
                .EncryptField(x => x.Ssn)
                .EnableAuditLogging(audit => audit.ExcludedFields.Add(nameof(SecurityTestModel.Password))))
            .Build();

        var component = RenderForm(config, _ => { }, model: model);

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        var entry = _auditEntries.ShouldHaveSingleItem();
        entry.EventType.ShouldBe(AuditEventTypes.FormSubmitted);
        entry.FormId.ShouldBe("SecurityTestModel");
        entry.AdditionalData["Name"].ShouldBe("John");
        entry.AdditionalData["Password"].ShouldBe("[REDACTED]");
        entry.AdditionalData["Ssn"].ShouldBe("[REDACTED]");
    }

    [Fact]
    public async Task Form_Without_Security_Should_Submit_Normally_And_Touch_No_Security_Services()
    {
        // Arrange
        var submitted = false;
        var component = RenderForm(
            FormBuilder<SecurityTestModel>
                .Create()
                .AddField(x => x.Name, field => field.WithLabel("Name"))
                .Build(),
            _ => submitted = true);

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        component.WaitForAssertion(() => submitted.ShouldBeTrue());
        component.FindAll("[data-testid=formcraft-security-error]").ShouldBeEmpty();
        A.CallTo(_rateLimitService).MustNotHaveHappened();
        A.CallTo(_csrfTokenService).MustNotHaveHappened();
        A.CallTo(_auditLogService).MustNotHaveHappened();
    }

    [Fact]
    public void GetEncryptedFieldValues_Should_Return_Encrypted_Copies_Without_Mutating_Model()
    {
        // Arrange
        var encryptionService = A.Fake<IEncryptionService>();
        A.CallTo(() => encryptionService.Encrypt(A<string?>._))
            .ReturnsLazily((string? value) => $"enc({value})");
        Services.AddSingleton(encryptionService);

        var model = new SecurityTestModel { Name = "John", Ssn = "123-45-6789" };
        var config = BuildConfig(security => security.EncryptField(x => x.Ssn));
        var component = RenderForm(config, _ => { }, model: model);

        // Act
        var encrypted = component.Instance.GetEncryptedFieldValues();

        // Assert
        encrypted.ShouldHaveSingleItem();
        encrypted["Ssn"].ShouldBe("enc(123-45-6789)");
        model.Ssn.ShouldBe("123-45-6789");
    }

    private static IFormConfiguration<SecurityTestModel> BuildConfig(
        Action<SecurityBuilder<SecurityTestModel>> securityConfig)
    {
        return FormBuilder<SecurityTestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .WithSecurity(securityConfig)
            .Build();
    }

    private IRenderedComponent<FormCraftComponent<SecurityTestModel>> RenderForm(
        IFormConfiguration<SecurityTestModel> config,
        Action<SecurityTestModel> onValidSubmit,
        Action<ComponentParameterCollectionBuilder<FormCraftComponent<SecurityTestModel>>>? extraParameters = null,
        SecurityTestModel? model = null)
    {
        return Render<FormCraftComponent<SecurityTestModel>>(parameters =>
        {
            parameters
                .Add(p => p.Model, model ?? new SecurityTestModel { Name = "John" })
                .Add(p => p.Configuration, config)
                .Add(p => p.OnValidSubmit, onValidSubmit);
            extraParameters?.Invoke(parameters);
        });
    }

    /// <summary>Model for the security suite - needs fields to exclude and to encrypt.</summary>
    public class SecurityTestModel
    {
        /// <summary>A plain string field, logged verbatim.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Excluded from audit entries via <c>ExcludedFields</c>.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Marked for encryption, so audit entries redact it.</summary>
        public string? Ssn { get; set; }
    }
}
