using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

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
    private const int MaxSubmissions = 3;
    private const string ResetTime = "1 minute";

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "security-demo",
        Title = "Security Features Demo",
        Description = "Comprehensive demonstration of FormCraft's enterprise-grade security features including field-level encryption, CSRF protection, rate limiting, and comprehensive audit logging. Learn how to protect sensitive user data and implement secure form handling in production applications.",
        Icon = Icons.Material.Filled.Security,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Lock, Color = Color.Primary, Text = "Field-level encryption for sensitive data (SSN, Credit Cards)" },
            new() { Icon = Icons.Material.Filled.Security, Color = Color.Secondary, Text = "CSRF token protection against cross-site attacks" },
            new() { Icon = Icons.Material.Filled.Speed, Color = Color.Warning, Text = "Rate limiting to prevent abuse and brute force attacks" },
            new() { Icon = Icons.Material.Filled.History, Color = Color.Info, Text = "Comprehensive audit logging for compliance" },
            new() { Icon = Icons.Material.Filled.Shield, Color = Color.Success, Text = "Configurable field exclusion for sensitive logging" },
            new() { Icon = Icons.Material.Filled.VerifiedUser, Color = Color.Tertiary, Text = "Secure data storage and transmission patterns" }
        ],
        ApiGuidelines =
        [
            new() { Feature = ".WithSecurity()", Usage = "Configure all security features for a form", Example = ".WithSecurity(security => security.EnableCsrfProtection())" },
            new() { Feature = ".EncryptField()", Usage = "Enable encryption for sensitive fields", Example = ".EncryptField(x => x.SSN).EncryptField(x => x.CreditCard)" },
            new() { Feature = ".EnableCsrfProtection()", Usage = "Enable CSRF token validation", Example = ".EnableCsrfProtection()" },
            new() { Feature = ".WithRateLimit()", Usage = "Set submission rate limits", Example = ".WithRateLimit(5, TimeSpan.FromMinutes(1))" },
            new() { Feature = ".EnableAuditLogging()", Usage = "Configure comprehensive audit logging", Example = ".EnableAuditLogging(audit => { audit.LogFieldChanges = true; })" },
            new() { Feature = "audit.ExcludedFields", Usage = "Exclude sensitive fields from audit logs", Example = "audit.ExcludedFields.Add(\"CreditCard\")" }
        ],
        CodeExamples =
        [
            new() { Title = "Security Configuration", Language = "csharp", CodeProvider = GetSecurityCodeStatic }
        ],
        WhenToUse = "Use security features when handling sensitive personal information, payment data, or any regulated data (PCI-DSS, HIPAA, GDPR). Field encryption is essential for storing sensitive data like SSNs, credit cards, and API keys. CSRF protection should be enabled on all forms that modify server state. Rate limiting prevents brute force attacks and abuse. Audit logging is critical for compliance, troubleshooting, and security monitoring in production systems.",
        CommonPitfalls =
        [
            "Don't log encrypted fields in plain text - exclude them from audit logs",
            "Remember rate limits are per-session - implement server-side rate limiting too",
            "Encryption keys must be securely managed - never hardcode them",
            "CSRF tokens need to be validated on the server side",
            "Audit logs can become large - implement retention policies"
        ],
        RelatedDemoIds = ["fluent", "validation", "async-value-provider"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<GuidelineItem> _securityGuidelines => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    private string SecurityCodeExample => GetSecurityCodeStatic();

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
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

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
            if (_submissionCount > MaxSubmissions)
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

    // Static code method for documentation
    private static string GetSecurityCodeStatic()
    {
        return @"var config = FormBuilder<SecureUserModel>.Create()
    .AddRequiredTextField(x => x.Name, ""Full Name"")
    .AddEmailField(x => x.Email)
    .AddField(x => x.SSN, field => field
        .WithLabel(""Social Security Number"")
        .Required(""SSN is required"")
        .WithPlaceholder(""XXX-XX-XXXX"")
        .WithValidator(value =>
                System.Text.RegularExpressions.Regex.IsMatch(value, @""^\d{3}-\d{2}-\d{4}$""),
            ""SSN must be in format XXX-XX-XXXX""))
    .AddField(x => x.CreditCard, field => field
        .WithLabel(""Credit Card Number"")
        .Required(""Credit card is required"")
        .WithPlaceholder(""XXXX-XXXX-XXXX-XXXX"")
        .WithValidator(value =>
                System.Text.RegularExpressions.Regex.IsMatch(value, @""^\d{4}-\d{4}-\d{4}-\d{4}$""),
            ""Credit card must be in format XXXX-XXXX-XXXX-XXXX""))
    .AddField(x => x.BirthDate, field => field
        .WithLabel(""Date of Birth"")
        .Required(""Date of birth is required""))
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
            audit.ExcludedFields.Add(""CreditCard"");
        }))
    .Build();";
    }
}