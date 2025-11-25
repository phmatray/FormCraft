using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class SecurityDemo
{
    private SecureUserModel _model = new();
    private IFormConfiguration<SecureUserModel> _formConfig = null!;
    private SecureUserModel? _lastSubmission;
    private readonly List<AuditLogEntry> _auditLogs = new();
    private bool _isSubmitting;
    private bool _isRateLimited;
    private string _encryptionDemo = "";
    private string _encryptedResult = "";
    private string _decryptedResult = "";
    private int _submissionCount;
    private const int _maxSubmissions = 3;
    private const string _resetTime = "1 minute";

    private readonly List<GuidelineItem> _securityGuidelines = new()
    {
        new()
        {
            Feature = "Security Configuration",
            Usage = "Configure security features",
            Example = ".WithSecurity(security => security.EnableCsrfProtection())"
        },
        new()
        {
            Feature = "Field Encryption",
            Usage = "Enable field-level encryption",
            Example = ".EncryptField(x => x.SSN)"
        },
        new()
        {
            Feature = "CSRF Protection",
            Usage = "Enable CSRF token validation",
            Example = ".EnableCsrfProtection()"
        },
        new()
        {
            Feature = "Rate Limiting",
            Usage = "Configure rate limiting",
            Example = ".WithRateLimit(5, TimeSpan.FromMinutes(1))"
        },
        new()
        {
            Feature = "Audit Logging",
            Usage = "Enable comprehensive audit logging",
            Example = ".EnableAuditLogging(audit => audit.LogFieldChanges = true)"
        }
    };

    private const string _securityCodeExample = @"var config = FormBuilder<SecureUserModel>.Create()
    .AddRequiredTextField(x => x.Name, ""Full Name"")
    .AddEmailField(x => x.Email)
    .AddField(x => x.SSN, field => field
        .WithLabel(""Social Security Number"")
        .Required()
        .WithPlaceholder(""XXX-XX-XXXX""))
    .AddField(x => x.CreditCard, field => field
        .WithLabel(""Credit Card Number"")
        .Required()
        .WithPlaceholder(""XXXX-XXXX-XXXX-XXXX""))
    .AddDateField(x => x.BirthDate, ""Date of Birth"")
    .WithSecurity(security => security
        .EncryptField(x => x.SSN)
        .EncryptField(x => x.CreditCard)
        .EnableCsrfProtection()
        .WithRateLimit(3, TimeSpan.FromMinutes(1))
        .EnableAuditLogging(audit =>
        {
            audit.LogFieldChanges = true;
            audit.LogSubmissions = true;
            audit.ExcludedFields.Add(""CreditCard"");
        }))
    .Build();";

    public class SecureUserModel
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string SSN { get; set; } = "";
        public string CreditCard { get; set; } = "";
        public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-25);
    }

    protected override void OnInitialized()
    {
        _formConfig = FormBuilder<SecureUserModel>.Create()
            .AddRequiredTextField(x => x.Name, "Full Name", minLength: 2)
            .AddEmailField(x => x.Email)
            .AddField(x => x.SSN, field => field
                .WithLabel("Social Security Number")
                .Required("SSN is required")
                .WithPlaceholder("XXX-XX-XXXX")
                .WithValidator(value =>
                        System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{3}-\d{2}-\d{4}$"),
                    "SSN must be in format XXX-XX-XXXX"))
            .AddField(x => x.CreditCard, field => field
                .WithLabel("Credit Card Number")
                .Required("Credit card is required")
                .WithPlaceholder("XXXX-XXXX-XXXX-XXXX")
                .WithValidator(value =>
                        System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{4}-\d{4}-\d{4}-\d{4}$"),
                    "Credit card must be in format XXXX-XXXX-XXXX-XXXX"))
            .AddField(x => x.BirthDate, field => field
                .WithLabel("Date of Birth")
                .Required("Date of birth is required"))
            .WithSecurity(security => security
                .EncryptField(x => x.SSN)
                .EncryptField(x => x.CreditCard)
                .EnableCsrfProtection()
                .WithRateLimit(3, TimeSpan.FromMinutes(1))
                .EnableAuditLogging(audit =>
                {
                    audit.LogFieldChanges = true;
                    audit.LogSubmissions = true;
                    audit.LogValidationErrors = true;
                    // Don't log credit card changes for security
                    audit.ExcludedFields.Add("CreditCard");
                }))
            .Build();

        // Simulate audit logging
        _auditLogs.Add(new AuditLogEntry
        {
            EventType = AuditEventTypes.FormLoaded,
            FormId = nameof(SecureUserModel),
            Timestamp = DateTime.Now
        });
    }

    private async Task HandleSecureSubmit(SecureUserModel model)
    {
        _isSubmitting = true;
        StateHasChanged();

        try
        {
            // Simulate rate limiting check
            _submissionCount++;
            if (_submissionCount > _maxSubmissions)
            {
                _isRateLimited = true;
                _auditLogs.Add(new AuditLogEntry
                {
                    EventType = AuditEventTypes.RateLimitExceeded,
                    FormId = nameof(SecureUserModel),
                    Timestamp = DateTime.Now
                });
                return;
            }

            // Simulate encryption of sensitive fields
            var encryptedModel = new SecureUserModel
            {
                Name = model.Name,
                Email = model.Email,
                SSN = EncryptionService.Encrypt(model.SSN) ?? model.SSN,
                CreditCard = EncryptionService.Encrypt(model.CreditCard) ?? model.CreditCard,
                BirthDate = model.BirthDate
            };

            _lastSubmission = encryptedModel;

            // Simulate audit log
            _auditLogs.Add(new AuditLogEntry
            {
                EventType = AuditEventTypes.FormSubmitted,
                FormId = nameof(SecureUserModel),
                Timestamp = DateTime.Now,
                AdditionalData = new Dictionary<string, object?>
                {
                    ["SubmissionCount"] = _submissionCount
                }
            });

            // Reset form
            _model = new SecureUserModel();

            // Simulate processing delay
            await Task.Delay(1000);
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private Task HandleFieldChange((string fieldName, object? value) args)
    {
        // Simulate audit log for field changes
        if (args.fieldName != "CreditCard") // Excluded field
        {
            _auditLogs.Add(new AuditLogEntry
            {
                EventType = AuditEventTypes.FieldChanged,
                FormId = nameof(SecureUserModel),
                FieldName = args.fieldName,
                NewValue = args.value?.ToString(),
                Timestamp = DateTime.Now
            });
        }

        return Task.CompletedTask;
    }

    private void EncryptDemo()
    {
        if (!string.IsNullOrEmpty(_encryptionDemo))
        {
            _encryptedResult = EncryptionService.Encrypt(_encryptionDemo) ?? "";
            _decryptedResult = EncryptionService.Decrypt(_encryptedResult) ?? "";
        }
    }

    private void ResetForm()
    {
        _model = new SecureUserModel();
        _lastSubmission = null;
        _submissionCount = 0;
        _isRateLimited = false;
        _auditLogs.Clear();
        _auditLogs.Add(new AuditLogEntry
        {
            EventType = AuditEventTypes.FormLoaded,
            FormId = nameof(SecureUserModel),
            Timestamp = DateTime.Now
        });
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        if (_lastSubmission == null) return new();

        return new List<FormSuccessDisplay.DataDisplayItem>
        {
            new() { Label = "Name", Value = _lastSubmission.Name },
            new() { Label = "Email", Value = _lastSubmission.Email },
            new() { Label = "SSN", Value = _lastSubmission.SSN.Length > 10 ? "***-**-" + _lastSubmission.SSN[^4..] : "Encrypted" },
            new() { Label = "Credit Card", Value = _lastSubmission.CreditCard.Length > 10 ? "****-****-****-" + _lastSubmission.CreditCard[^4..] : "Encrypted" },
            new() { Label = "Birth Date", Value = _lastSubmission.BirthDate.ToShortDateString() }
        };
    }
}