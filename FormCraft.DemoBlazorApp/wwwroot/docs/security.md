# Security Features

FormCraft provides security building blocks to protect your forms and sensitive data. This guide covers field-level encryption, CSRF protection, rate limiting, and audit logging.

## Overview

FormCraft's security features help you:
- **Encrypt sensitive fields** - Protect PII and sensitive data at rest
- **Prevent CSRF attacks** - Token generation and validation services
- **Limit form submissions** - Rate limiting to prevent spam
- **Track user actions** - Comprehensive audit logging

> **How enforcement works (v3.1+)**: `WithSecurity()` records the security settings on
> the form configuration (accessible via `config.Security`), and `AddFormCraft()`
> registers the supporting services (`IEncryptionService`, `ICsrfTokenService`,
> `IRateLimitService`, `IAuditLogService`). The standard `FormCraftComponent` enforces
> CSRF validation, rate limiting, and audit logging automatically: violating
> submissions are blocked with a visible error before they ever reach your
> `OnValidSubmit` handler. Field **encryption** remains an explicit application
> concern — use `EncryptConfiguredFields()` / `GetEncryptedFieldValues()` when
> persisting, as shown below.

## Quick Start

```csharp
var config = FormBuilder<UserModel>.Create()
    .AddField(x => x.Name, field => field
        .WithLabel("Name")
        .Required())
    .AddField(x => x.SSN, field => field
        .WithLabel("Social Security Number")
        .Required())
    .WithSecurity(security => security
        .EncryptField(x => x.SSN)
        .EnableCsrfProtection()
        .WithRateLimit(5, TimeSpan.FromMinutes(1))
        .EnableAuditLogging())
    .Build();
```

## Field-Level Encryption

Protect sensitive data by encrypting specific fields before storage.

### Basic Encryption

```csharp
.WithSecurity(security => security
    .EncryptField(x => x.SSN)
    .EncryptField(x => x.CreditCardNumber)
    .EncryptField(x => x.BankAccount))
```

### Configuration

Configure encryption in `appsettings.json`:

```json
{
  "FormCraft": {
    "Encryption": {
      "Key": "your-32-character-encryption-key",
      "IV": "16-character-iv"
    }
  }
}
```

> **Important**: Never commit encryption keys to source control. Use Azure Key Vault, AWS Secrets Manager, or environment variables in production.

### How It Works

1. `EncryptField()` marks fields as sensitive in `config.Security.EncryptedFields`
2. You encrypt/decrypt the marked fields with `IEncryptionService` when persisting or loading data:

```csharp
@inject IEncryptionService EncryptionService

private async Task HandleSubmit(SecureFormModel model)
{
    // Encrypt marked fields before storing
    var encryptedSsn = EncryptionService.Encrypt(model.SSN);
    await SaveAsync(model with { SSN = encryptedSsn });
}
```

> **Choosing an implementation**: `AddFormCraft()` registers `BlazorEncryptionService`
> by default, which uses a simple XOR cipher so it can run in the browser
> (WebAssembly). It is suitable for demos only. For production data, register the
> server-side AES-256 implementation or your own:
>
> ```csharp
> services.AddScoped<IEncryptionService, DefaultEncryptionService>(); // AES, server-side
> ```

### Custom Encryption Service

Implement your own encryption service:

```csharp
public class CustomEncryptionService : IEncryptionService
{
    public string? Encrypt(string? value)
    {
        // Your encryption logic
    }
    
    public string? Decrypt(string? encryptedValue)
    {
        // Your decryption logic
    }
}

// Register in DI
services.AddScoped<IEncryptionService, CustomEncryptionService>();
```

## CSRF Protection

Prevent Cross-Site Request Forgery attacks with built-in token validation.

### Enable CSRF Protection

```csharp
.WithSecurity(security => security
    .EnableCsrfProtection())
```

### Custom Token Field Name

```csharp
.WithSecurity(security => security
    .EnableCsrfProtection("_csrf_token"))
```

### How It Works

`EnableCsrfProtection()` sets `IsCsrfProtectionEnabled` and `CsrfTokenFieldName` on
`config.Security`. `FormCraftComponent` then generates a token via
`ICsrfTokenService` when the form initializes and validates it before invoking
`OnValidSubmit`. When validation fails, the submission is blocked and an error
alert is shown above the submit button — no app-side code required. If CSRF
protection is enabled but no `ICsrfTokenService` is registered, the component shows
a clear misconfiguration error instead of crashing.

### Custom CSRF Service

For advanced scenarios, implement a custom CSRF service:

```csharp
public class CustomCsrfTokenService : ICsrfTokenService
{
    public Task<string> GenerateTokenAsync() { }
    public Task<bool> ValidateTokenAsync(string token) { }
}
```

## Rate Limiting

Prevent spam and abuse by limiting form submissions.

### Basic Rate Limiting

```csharp
.WithSecurity(security => security
    .WithRateLimit(5, TimeSpan.FromMinutes(1)))
```

This configures a limit of 5 submissions per minute per identifier.
`FormCraftComponent` enforces it automatically via `IRateLimitService` *before*
validation runs: blocked submissions show a friendly "try again later" message and
never reach `OnValidSubmit`; allowed submissions record an attempt.

The identifier defaults to the model type name, which is shared across all users.
Pass a per-user value via the `SecurityContextId` parameter so limits apply per
user/session:

```razor
<FormCraftComponent TModel="SecureFormModel"
                    Model="@model"
                    Configuration="@config"
                    SecurityContextId="@userId"
                    OnValidSubmit="@HandleSubmit" />
```

### Custom Identifier

Rate limit by user ID instead of IP:

```csharp
.WithSecurity(security => security
    .WithRateLimit(10, TimeSpan.FromHours(1), "UserId"))
```

### Rate Limit Configuration

- **MaxAttempts**: Maximum number of submissions allowed
- **TimeWindow**: Time period for the limit
- **IdentifierType**: What to use for tracking (IP, UserId, SessionId)

### Custom Rate Limiting Service

For distributed applications, implement a Redis-based service:

```csharp
public class RedisRateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    
    public async Task<RateLimitResult> CheckRateLimitAsync(
        string identifier, 
        int maxAttempts, 
        TimeSpan timeWindow)
    {
        // Redis implementation
    }
}
```

## Audit Logging

Track all form interactions for compliance and security monitoring.

### Enable Audit Logging

```csharp
.WithSecurity(security => security
    .EnableAuditLogging())
```

### Configure What to Log

```csharp
.WithSecurity(security => security
    .EnableAuditLogging(audit =>
    {
        audit.LogFieldChanges = true;
        audit.LogValidationErrors = true;
        audit.LogSubmissions = true;
        audit.ExcludedFields.Add("Password");
        audit.ExcludedFields.Add("CreditCard");
    }))
```

### Audit Events

`EnableAuditLogging()` stores the audit configuration (what to log, excluded fields)
on `config.Security.AuditLog`. `FormCraftComponent` automatically writes
submission entries via `IAuditLogService.LogAsync()`:

- **FormSubmitted** - When a valid form submission reaches `OnValidSubmit`
- **FormRejected** - When a submission is blocked (with a `Reason` of
  `RateLimitExceeded` or `CsrfValidationFailed` in `AdditionalData`)

Both entries include the current field values in `AdditionalData`; fields listed in
`ExcludedFields` *and* fields marked with `EncryptField()` are written as
`[REDACTED]`. You can still write additional entries (e.g. `FormLoaded`,
`FieldChanged` via the `OnFieldChanged` callback, `ValidationError`) from your own
handlers using the same service.

### Audit Log Entry Structure

```csharp
public class AuditLogEntry
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
    public string FormId { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Dictionary<string, object?> AdditionalData { get; set; }
}
```

### Custom Audit Service

Store audit logs in a database:

```csharp
public class DatabaseAuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    
    public async Task LogAsync(AuditLogEntry entry)
    {
        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync();
    }
}
```

## Complete Security Example

Here's a comprehensive example using all security features:

```csharp
public class SecureFormModel
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string SSN { get; set; } = "";
    public string CreditCard { get; set; } = "";
    public decimal Amount { get; set; }
}

// Form configuration
var config = FormBuilder<SecureFormModel>.Create()
    .AddRequiredTextField(x => x.Name, "Full Name")
    .AddEmailField(x => x.Email)
    .AddField(x => x.SSN, field => field
        .WithLabel("Social Security Number")
        .Required()
        .WithPlaceholder("XXX-XX-XXXX"))
    .AddField(x => x.CreditCard, field => field
        .WithLabel("Credit Card")
        .Required())
    .AddCurrencyField(x => x.Amount, "Payment Amount")
    .WithSecurity(security => security
        // Encrypt sensitive fields
        .EncryptField(x => x.SSN)
        .EncryptField(x => x.CreditCard)
        // Enable CSRF protection
        .EnableCsrfProtection()
        // Rate limit to 3 submissions per 5 minutes
        .WithRateLimit(3, TimeSpan.FromMinutes(5))
        // Enable audit logging
        .EnableAuditLogging(audit =>
        {
            // Don't log credit card changes
            audit.ExcludedFields.Add("CreditCard");
        }))
    .Build();
```

## Rendering a Secure Form

Render the form with the standard `FormCraftComponent` — CSRF validation, rate
limiting, and audit logging are enforced automatically. Your submit handler only
needs to handle persistence, encrypting marked fields in one call:

```razor
<FormCraftComponent TModel="SecureFormModel" 
                    Model="@model" 
                    Configuration="@config"
                    SecurityContextId="@userId"
                    OnValidSubmit="@HandleSecureSubmit"
                    ShowSubmitButton="true" />

@code {
    [Inject] private IEncryptionService EncryptionService { get; set; } = default!;

    private async Task HandleSecureSubmit(SecureFormModel model)
    {
        // Rate limiting and CSRF have already been enforced by the component;
        // a FormSubmitted audit entry has been written (with redacted fields).

        // Encrypt the fields marked with EncryptField() before persisting.
        // The model itself is never modified.
        var encrypted = EncryptionService.EncryptConfiguredFields(model, config.Security);
        var ssnToStore = encrypted["SSN"];
        var cardToStore = encrypted["CreditCard"];

        // Process the submission
    }
}
```

If you hold a reference to the component (`@ref`), the equivalent
`component.GetEncryptedFieldValues()` convenience method uses the registered
`IEncryptionService` and the form's own configuration.

## Best Practices

### Encryption
1. **Key Management**: Use a proper key management system in production
2. **Key Rotation**: Implement key rotation policies
3. **Selective Encryption**: Only encrypt truly sensitive fields to maintain performance
4. **Backup Keys**: Ensure encryption keys are backed up securely

### CSRF Protection
1. **Always Enable**: Enable CSRF protection for all public-facing forms
2. **Token Lifetime**: Consider token expiration for long forms
3. **SameSite Cookies**: Use SameSite cookie attributes as additional protection

### Rate Limiting
1. **Appropriate Limits**: Set limits based on legitimate use cases
2. **User Feedback**: Provide clear feedback when rate limits are hit
3. **Monitoring**: Monitor rate limit hits for potential attacks
4. **Bypass for Authenticated**: Consider different limits for authenticated users

### Audit Logging
1. **Retention Policy**: Implement log retention policies
2. **PII Handling**: Be careful with PII in logs
3. **Log Analysis**: Regularly analyze logs for security events
4. **Performance**: Consider async logging for better performance

## Compliance Considerations

FormCraft's security features help with:
- **GDPR**: Encryption and audit trails for data protection
- **PCI DSS**: Encryption of credit card data
- **HIPAA**: Audit logging and encryption for healthcare data
- **SOC 2**: Comprehensive audit trails

## Performance Impact

Security features have minimal performance impact:
- **Encryption**: ~1-2ms per field
- **CSRF**: Negligible
- **Rate Limiting**: <1ms lookup time
- **Audit Logging**: Async, non-blocking

## Troubleshooting

### Encryption Issues
- **Error**: "Invalid padding" - Check encryption keys match
- **Error**: "Key size invalid" - Ensure 32-character key
- **Performance**: Only encrypt necessary fields

### CSRF Issues
- **Token Invalid**: Check token hasn't expired
- **Missing Token**: Ensure JavaScript is enabled
- **Multiple Tabs**: Each tab needs its own token

### Rate Limiting Issues
- **False Positives**: Check identifier configuration
- **Not Working**: Verify service registration
- **Shared IPs**: Consider using user IDs instead

### Audit Logging Issues
- **Missing Logs**: Check service registration
- **Performance**: Use async logging service
- **Storage**: Implement log rotation

## Next Steps

- Review the [API Reference](/docs/api-reference#security) for detailed security API documentation
- See [Examples](/docs/examples#secure-forms) for more security examples
- Check [Customization](/docs/customization#security) for advanced security customization