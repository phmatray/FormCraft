# Examples

Common patterns and examples for FormCraft.

## Contact Form

A complete contact form with validation and field dependencies.

```csharp
public class ContactModel
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Country { get; set; } = "";
    public string City { get; set; } = "";
    public bool SubscribeToNewsletter { get; set; }
}

// Form configuration
var config = FormBuilder<ContactModel>
    .Create()
    .WithLayout(FormLayout.Horizontal)
    .AddRequiredTextField(x => x.FirstName, "First Name", minLength: 2)
    .AddRequiredTextField(x => x.LastName, "Last Name", minLength: 2)
    .AddEmailField(x => x.Email)
    .AddField(x => x.Phone, field => field
        .WithLabel("Phone")
        .WithPlaceholder("(555) 123-4567"))
    .AddDropdownField(x => x.Country, "Country",
        ("US", "United States"),
        ("CA", "Canada"),
        ("UK", "United Kingdom"))
    .AddField(x => x.City, field => field
        .WithLabel("City")
        .VisibleWhen(m => !string.IsNullOrEmpty(m.Country))
        .DependsOn(x => x.Country, (model, country) => {
            if (string.IsNullOrEmpty(country)) {
                model.City = "";
            }
        }))
    .AddCheckboxField(x => x.SubscribeToNewsletter, "Subscribe to newsletter")
    .Build();
```

## Registration Form

User registration with password confirmation.

```csharp
public class RegistrationModel
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public bool AcceptTerms { get; set; }
}

// Passwords-match validation needs the full model, so implement IFieldValidator
public class PasswordsMatchValidator : IFieldValidator<RegistrationModel, string>
{
    public string? ErrorMessage { get; set; } = "Passwords must match";

    public Task<ValidationResult> ValidateAsync(
        RegistrationModel model, string value, IServiceProvider services)
        => Task.FromResult(value == model.Password
            ? ValidationResult.Success()
            : ValidationResult.Failure("Passwords must match"));
}

var config = FormBuilder<RegistrationModel>
    .Create()
    .AddField(x => x.Username, field => field
        .WithLabel("Username")
        .Required("Username is required")
        .WithMinLength(3)
        .WithMaxLength(20)
        .WithValidator(value => !value.Contains(" "), "Username cannot contain spaces"))
    .AddEmailField(x => x.Email)
    .AddPasswordField(x => x.Password, "Password", minLength: 8, requireSpecialChars: true)
    .AddField(x => x.ConfirmPassword, field => field
        .WithLabel("Confirm Password")
        .WithInputType("password")
        .Required("Please confirm your password")
        .WithValidator(new PasswordsMatchValidator()))
    .AddField(x => x.DateOfBirth, field => field
        .WithLabel("Date of Birth")
        .WithValidator(value => value < DateTime.Now.AddYears(-13),
            "Must be at least 13 years old"))
    .AddField(x => x.AcceptTerms, field => field
        .WithLabel("I accept the terms and conditions")
        .Required("You must accept the terms"))
    .Build();
```

## Survey Form

Multi-section survey with conditional questions.

```csharp
public class SurveyModel
{
    public string Name { get; set; } = "";
    public int Satisfaction { get; set; }
    public bool WouldRecommend { get; set; }
    public string Feedback { get; set; } = "";
    public string ImprovementSuggestions { get; set; } = "";
}

var config = FormBuilder<SurveyModel>
    .Create()
    .AddRequiredTextField(x => x.Name, "Your Name")
    .AddNumericField(x => x.Satisfaction, "Satisfaction (1-10)", 1, 10)
    .AddCheckboxField(x => x.WouldRecommend, "Would you recommend us to others?")
    .AddField(x => x.Feedback, field => field
        .WithLabel("Additional Feedback")
        .WithPlaceholder("Tell us about your experience...")
        .AsTextArea(lines: 4))
    .AddField(x => x.ImprovementSuggestions, field => field
        .WithLabel("Suggestions for Improvement")
        .AsTextArea(lines: 3)
        .VisibleWhen(m => m.Satisfaction < 8))
    .Build();
```

## Custom Validation Example

Complex validation with async checks.

```csharp
public class AccountModel
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
}

// Custom async validator - resolve services from the IServiceProvider argument
public class UsernameAvailabilityValidator : IFieldValidator<AccountModel, string>
{
    public string? ErrorMessage { get; set; } = "Username is already taken";
    
    public async Task<ValidationResult> ValidateAsync(AccountModel model, string value, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(value))
            return ValidationResult.Success();
            
        var userService = services.GetRequiredService<IUserService>();
        var isAvailable = await userService.IsUsernameAvailableAsync(value);
        
        return isAvailable 
            ? ValidationResult.Success() 
            : ValidationResult.Failure("Username is already taken");
    }
}

// Usage
var config = FormBuilder<AccountModel>
    .Create()
    .AddField(x => x.Username, field => field
        .WithLabel("Username")
        .Required()
        .WithValidator(new UsernameAvailabilityValidator()))
    .Build();
```

For simple async checks that do not need DI, `WithAsyncValidator` takes a delegate:

```csharp
.AddField(x => x.Email, field => field
    .WithLabel("Email")
    .WithAsyncValidator(async email => await CheckEmailAvailabilityAsync(email),
        "Email is already registered"))
```