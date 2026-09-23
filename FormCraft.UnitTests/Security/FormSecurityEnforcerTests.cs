using Microsoft.Extensions.Logging;

namespace FormCraft.UnitTests.Security;

/// <summary>
/// Pins the shared security pipeline (#321) directly, without a renderer. Both adapters'
/// <c>FormCraftComponentSecurityTests</c> suites exercise the same policy end to end through a
/// rendered form; these pin it at the seam both containers now delegate to.
/// </summary>
public class FormSecurityEnforcerTests
{
    private readonly IRateLimitService _rateLimitService = A.Fake<IRateLimitService>();
    private readonly ICsrfTokenService _csrfTokenService = A.Fake<ICsrfTokenService>();
    private readonly IAuditLogService _auditLogService = A.Fake<IAuditLogService>();
    private readonly List<AuditLogEntry> _auditEntries = [];

    public FormSecurityEnforcerTests()
    {
        A.CallTo(() => _rateLimitService.CheckRateLimitAsync(A<string>._, A<int>._, A<TimeSpan>._))
            .Returns(new RateLimitResult { IsAllowed = true, RemainingAttempts = 4 });
        A.CallTo(() => _csrfTokenService.GenerateTokenAsync()).Returns("test-csrf-token");
        A.CallTo(() => _csrfTokenService.ValidateTokenAsync(A<string>._)).Returns(true);
        A.CallTo(() => _auditLogService.LogAsync(A<AuditLogEntry>._))
            .Invokes((AuditLogEntry entry) => _auditEntries.Add(entry))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task EnforceAsync_Should_Block_And_Set_Error_When_Rate_Limited()
    {
        A.CallTo(() => _rateLimitService.CheckRateLimitAsync(A<string>._, A<int>._, A<TimeSpan>._))
            .Returns(new RateLimitResult { IsAllowed = false, RetryAfter = TimeSpan.FromSeconds(30) });
        var config = BuildConfig(s => s.WithRateLimit(5, TimeSpan.FromMinutes(1)).EnableAuditLogging());
        var enforcer = CreateEnforcer();

        var allowed = await enforcer.EnforceAsync(config, new TestModel(), null);

        allowed.ShouldBeFalse();
        enforcer.Error.ShouldBe("Too many submissions. Please try again in 30 seconds.");
        A.CallTo(() => _rateLimitService.RecordAttemptAsync(A<string>._)).MustNotHaveHappened();
        var rejection = _auditEntries.ShouldHaveSingleItem();
        rejection.EventType.ShouldBe(AuditEventTypes.FormRejected);
        rejection.AdditionalData["Reason"].ShouldBe(AuditEventTypes.RateLimitExceeded);
    }

    [Fact]
    public async Task EnforceAsync_Should_Record_Attempt_Under_The_Context_Id_When_Allowed()
    {
        var config = BuildConfig(s => s.WithRateLimit(5, TimeSpan.FromMinutes(1)));
        var enforcer = CreateEnforcer();

        (await enforcer.EnforceAsync(config, new TestModel(), "user-42")).ShouldBeTrue();
        (await enforcer.EnforceAsync(config, new TestModel(), " ")).ShouldBeTrue();

        enforcer.Error.ShouldBeNull();
        A.CallTo(() => _rateLimitService.CheckRateLimitAsync("user-42", 5, TimeSpan.FromMinutes(1)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _rateLimitService.RecordAttemptAsync("user-42")).MustHaveHappenedOnceExactly();
        // A blank context id falls back to the model type name.
        A.CallTo(() => _rateLimitService.RecordAttemptAsync(nameof(TestModel))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EnforceAsync_Should_Block_When_No_Rate_Limit_Service_Is_Registered()
    {
        var config = BuildConfig(s => s.WithRateLimit(5, TimeSpan.FromMinutes(1)));
        var enforcer = new FormSecurityEnforcer<TestModel>(new ServiceCollection().BuildServiceProvider());

        (await enforcer.EnforceAsync(config, new TestModel(), null)).ShouldBeFalse();

        enforcer.Error!.ShouldContain("no IRateLimitService is registered");
    }

    [Fact]
    public async Task Csrf_Token_Should_Be_Generated_At_Init_And_Validated_At_Enforce()
    {
        var config = BuildConfig(s => s.EnableCsrfProtection());
        var enforcer = CreateEnforcer();

        await enforcer.InitializeAsync(config, null);
        A.CallTo(() => _csrfTokenService.GenerateTokenAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _csrfTokenService.ValidateTokenAsync(A<string>._)).MustNotHaveHappened();

        (await enforcer.EnforceAsync(config, new TestModel(), null)).ShouldBeTrue();
        A.CallTo(() => _csrfTokenService.ValidateTokenAsync("test-csrf-token")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task EnforceAsync_Should_Block_When_Csrf_Token_Is_Invalid()
    {
        A.CallTo(() => _csrfTokenService.ValidateTokenAsync(A<string>._)).Returns(false);
        var config = BuildConfig(s => s.EnableCsrfProtection().EnableAuditLogging());
        var enforcer = CreateEnforcer();
        await enforcer.InitializeAsync(config, null);

        (await enforcer.EnforceAsync(config, new TestModel(), null)).ShouldBeFalse();

        enforcer.Error.ShouldBe("Your session could not be verified. Please reload the page and try again.");
        _auditEntries.ShouldHaveSingleItem().AdditionalData["Reason"].ShouldBe(AuditEventTypes.CsrfValidationFailed);
    }

    [Fact]
    public async Task Missing_Csrf_Service_Should_Surface_Misconfiguration_At_Init_And_Block_Submit()
    {
        var logger = A.Fake<ILogger>();
        A.CallTo(() => logger.IsEnabled(A<LogLevel>._)).Returns(true);
        var config = BuildConfig(s => s.EnableCsrfProtection());
        var enforcer = new FormSecurityEnforcer<TestModel>(new ServiceCollection().BuildServiceProvider(), logger);

        await enforcer.InitializeAsync(config, null);
        enforcer.Error!.ShouldContain("no ICsrfTokenService is registered");
        A.CallTo(logger).Where(call => call.Method.Name == nameof(ILogger.Log)).MustHaveHappenedOnceExactly();

        (await enforcer.EnforceAsync(config, new TestModel(), null)).ShouldBeFalse();
        // Enforcement starts from a cleared error, so the submit reports the missing token.
        enforcer.Error!.ShouldContain("security token is missing");
    }

    [Fact]
    public async Task LogSubmittedAsync_Should_Redact_Excluded_And_Encrypted_Fields()
    {
        var model = new TestModel { Name = "John", Password = "hunter2", Ssn = "123-45-6789" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name)
            .AddField(x => x.Password)
            .AddField(x => x.Ssn)
            .WithSecurity(s => s
                .EncryptField(x => x.Ssn)
                .EnableAuditLogging(audit => audit.ExcludedFields.Add(nameof(TestModel.Password))))
            .Build();

        await CreateEnforcer().LogSubmittedAsync(config, model, null);

        var entry = _auditEntries.ShouldHaveSingleItem();
        entry.EventType.ShouldBe(AuditEventTypes.FormSubmitted);
        entry.FormId.ShouldBe(nameof(TestModel));
        entry.AdditionalData["Name"].ShouldBe("John");
        entry.AdditionalData["Password"].ShouldBe("[REDACTED]");
        entry.AdditionalData["Ssn"].ShouldBe("[REDACTED]");
    }

    [Fact]
    public async Task LogSubmittedAsync_Should_Record_A_Nested_Fields_Real_Value()
    {
        // #321 (folded in from #387): the old GetProperty(FieldName) lookup could not resolve a
        // nested path, so this entry was always null regardless of the field's value.
        var model = new TestModel { Address = new TestAddress { City = "Brussels" } };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Address!.City)
            .WithSecurity(s => s.EnableAuditLogging())
            .Build();

        await CreateEnforcer().LogSubmittedAsync(config, model, null);

        _auditEntries.ShouldHaveSingleItem().AdditionalData["Address.City"].ShouldBe("Brussels");
    }

    [Fact]
    public async Task LogSubmittedAsync_Should_Record_Null_Rather_Than_Throw_For_An_Unreachable_Nested_Field()
    {
        var model = new TestModel { Address = null };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Address!.City)
            .WithSecurity(s => s.EnableAuditLogging())
            .Build();

        await CreateEnforcer().LogSubmittedAsync(config, model, null);

        _auditEntries.ShouldHaveSingleItem().AdditionalData["Address.City"].ShouldBeNull();
    }

    [Fact]
    public async Task LogSubmittedAsync_Should_Not_Collide_When_Two_Nested_Fields_Share_A_Member_Name()
    {
        // #406: Address.City and Work.City both resolved FieldName to "City", so the second field's
        // write silently overwrote the first's under one shared key.
        var model = new TestModel
        {
            Address = new TestAddress { City = "Brussels" },
            Work = new TestAddress { City = "Antwerp" },
        };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Address!.City)
            .AddField(x => x.Work!.City)
            .WithSecurity(s => s.EnableAuditLogging())
            .Build();

        await CreateEnforcer().LogSubmittedAsync(config, model, null);

        var entry = _auditEntries.ShouldHaveSingleItem();
        entry.AdditionalData["Address.City"].ShouldBe("Brussels");
        entry.AdditionalData["Work.City"].ShouldBe("Antwerp");
    }

    [Fact]
    public async Task LogSubmittedAsync_Should_Redact_A_Nested_Field_Under_Its_Full_Path_Key()
    {
        // Redaction still matches on FieldName (last member, "City") by design, but the redacted
        // "[REDACTED]" write must land under the field's full-path key, same as the value write.
        var model = new TestModel { Address = new TestAddress { City = "Brussels" } };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Address!.City)
            .WithSecurity(s => s.EnableAuditLogging(audit => audit.ExcludedFields.Add("City")))
            .Build();

        await CreateEnforcer().LogSubmittedAsync(config, model, null);

        _auditEntries.ShouldHaveSingleItem().AdditionalData["Address.City"].ShouldBe("[REDACTED]");
    }

    [Fact]
    public async Task A_Form_Without_Security_Should_Touch_No_Security_Service()
    {
        var config = FormBuilder<TestModel>.Create().AddField(x => x.Name).Build();
        var enforcer = CreateEnforcer();

        await enforcer.InitializeAsync(config, null);
        (await enforcer.EnforceAsync(config, new TestModel(), null)).ShouldBeTrue();
        await enforcer.LogSubmittedAsync(config, new TestModel(), null);

        enforcer.Error.ShouldBeNull();
        A.CallTo(_rateLimitService).MustNotHaveHappened();
        A.CallTo(_csrfTokenService).MustNotHaveHappened();
        A.CallTo(_auditLogService).MustNotHaveHappened();
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Throw_When_No_Encryption_Service_Is_Registered()
    {
        var enforcer = new FormSecurityEnforcer<TestModel>(new ServiceCollection().BuildServiceProvider());

        Should.Throw<InvalidOperationException>(() =>
            enforcer.EncryptConfiguredFields(new TestModel(), BuildConfig(s => s.EncryptField(x => x.Ssn)).Security));
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Encrypt_Only_Configured_Fields_Without_Mutating_The_Model()
    {
        var encryptionService = A.Fake<IEncryptionService>();
        A.CallTo(() => encryptionService.Encrypt(A<string?>._)).ReturnsLazily((string? v) => $"enc({v})");
        var enforcer = new FormSecurityEnforcer<TestModel>(
            new ServiceCollection().AddSingleton(encryptionService).BuildServiceProvider());
        var model = new TestModel { Name = "John", Ssn = "123-45-6789" };

        var encrypted = enforcer.EncryptConfiguredFields(model, BuildConfig(s => s.EncryptField(x => x.Ssn)).Security);

        encrypted.ShouldHaveSingleItem();
        encrypted["Ssn"].ShouldBe("enc(123-45-6789)");
        model.Ssn.ShouldBe("123-45-6789");
    }

    private FormSecurityEnforcer<TestModel> CreateEnforcer() =>
        new(new ServiceCollection()
            .AddSingleton(_rateLimitService)
            .AddSingleton(_csrfTokenService)
            .AddSingleton(_auditLogService)
            .BuildServiceProvider());

    private static IFormConfiguration<TestModel> BuildConfig(Action<SecurityBuilder<TestModel>> security) =>
        FormBuilder<TestModel>.Create().AddField(x => x.Name).WithSecurity(security).Build();

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Ssn { get; set; }
        public TestAddress? Address { get; set; }
        public TestAddress? Work { get; set; }
    }

    public class TestAddress
    {
        public string City { get; set; } = string.Empty;
    }
}
