# Security Features

FormCraft provides comprehensive security features to protect your forms and sensitive data. This guide covers field-level encryption, CSRF protection, rate limiting, and audit logging.

## Overview

FormCraft's security features help you:
- **Encrypt sensitive fields** - Protect PII and sensitive data at rest
- **Prevent CSRF attacks** - Built-in token validation
- **Limit form submissions** - Rate limiting to prevent spam
- **Track user actions** - Comprehensive audit logging

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

1. When a form is submitted, encrypted fields are automatically encrypted before processing
2. When displaying a form, encrypted values are decrypted for editing
3. The encryption uses AES-256 for strong security

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

1. A unique token is generated when the form loads
2. The token is included as a hidden field in the form
3. On submission, the token is validated
4. Invalid tokens result in form rejection

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

This allows 5 submissions per minute per IP address.

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

FormCraft logs these events:
- **FormLoaded** - When a form is displayed
- **FieldChanged** - When a field value changes
- **ValidationError** - When validation fails
- **FormSubmitted** - When a form is successfully submitted
- **RateLimitExceeded** - When rate limit is hit
- **CsrfValidationFailed** - When CSRF validation fails

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

## Using SecureFormCraftComponent

For forms with security features, use the `SecureFormCraftComponent`:

```razor
<SecureFormCraftComponent TModel="SecureFormModel" 
                         Model="@model" 
                         Configuration="@config"
                         OnValidSubmit="@HandleSecureSubmit"
                         ShowSubmitButton="true" />

@code {
    private async Task HandleSecureSubmit(SecureFormModel model)
    {
        // Model will have decrypted values here
        // Process the submission
    }
}
```

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