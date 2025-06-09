# Examples

Common patterns and examples for Dynamic Form Blazor.

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

var config = FormBuilder<RegistrationModel>
    .Create()
    .AddRequiredTextField(x => x.Username, "Username", minLength: 3, maxLength: 20)
        .WithValidator(value => !value.Contains(" "), "Username cannot contain spaces")
    .AddEmailField(x => x.Email)
    .AddPasswordField(x => x.Password, "Password", 8, true)
    .AddField(x => x.ConfirmPassword, field => field
        .WithLabel("Confirm Password")
        .Required("Please confirm your password")
        .WithValidator((value, model) => value == model.Password, "Passwords must match"))
    .AddDateField(x => x.DateOfBirth, "Date of Birth")
        .WithValidator(value => value < DateTime.Now.AddYears(-13), "Must be at least 13 years old")
    .AddCheckboxField(x => x.AcceptTerms, "I accept the terms and conditions")
        .Required("You must accept the terms")
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
        .AsTextArea(rows: 4))
    .AddField(x => x.ImprovementSuggestions, field => field
        .WithLabel("Suggestions for Improvement")
        .AsTextArea(rows: 3)
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

// Custom async validator
public class UsernameAvailabilityValidator : IFieldValidator<AccountModel, string>
{
    private readonly IUserService _userService;
    
    public UsernameAvailabilityValidator(IUserService userService)
    {
        _userService = userService;
    }
    
    public async Task<ValidationResult> ValidateAsync(AccountModel model, string value, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(value))
            return ValidationResult.Success();
            
        var isAvailable = await _userService.IsUsernameAvailableAsync(value);
        
        return isAvailable 
            ? ValidationResult.Success() 
            : ValidationResult.Error("Username is already taken");
    }
}

// Usage
var config = FormBuilder<AccountModel>
    .Create()
    .AddField(x => x.Username, field => field
        .WithLabel("Username")
        .Required()
        .WithAsyncValidator<UsernameAvailabilityValidator>())
    .Build();
```