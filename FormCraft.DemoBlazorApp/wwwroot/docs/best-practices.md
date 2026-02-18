# Best Practices

Guidelines for building maintainable, performant, and user-friendly forms with FormCraft.

## Form Configuration

### Keep Configurations Immutable

Build configurations once during initialization, not on every render:

```csharp
// Good: Build once in OnInitialized
protected override void OnInitialized()
{
    _formConfig = FormBuilder<MyModel>
        .Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .Build();
}

// Bad: Building on every render
private IFormConfiguration<MyModel> Config => FormBuilder<MyModel>
    .Create()
    .AddField(x => x.Name, field => field.WithLabel("Name"))
    .Build(); // Creates new object on each access
```

### Use Meaningful Labels and Help Text

Always provide clear, user-friendly labels and help text:

```csharp
.AddField(x => x.SocialSecurityNumber, field => field
    .WithLabel("Social Security Number")
    .WithPlaceholder("XXX-XX-XXXX")
    .WithHelpText("Your 9-digit SSN. This is kept secure and encrypted.")
    .Required("Social Security Number is required for tax purposes"))
```

### Group Related Fields

Use field groups to organize complex forms:

```csharp
var config = FormBuilder<ApplicationModel>
    .Create()
    .AddFieldGroup(group => group
        .WithGroupName("Personal Information")
        .WithColumns(2)
        .ShowInCard()
        .AddField(x => x.FirstName, f => f.WithLabel("First Name").Required())
        .AddField(x => x.LastName, f => f.WithLabel("Last Name").Required())
        .AddField(x => x.DateOfBirth, f => f.WithLabel("Date of Birth")))
    .AddFieldGroup(group => group
        .WithGroupName("Contact Details")
        .WithColumns(1)
        .ShowInCard()
        .AddField(x => x.Email, f => f.WithLabel("Email").WithEmailValidation())
        .AddField(x => x.Phone, f => f.WithLabel("Phone")))
    .Build();
```

## Validation

### Provide Helpful Error Messages

Write error messages that help users fix the problem:

```csharp
// Good: Specific and helpful
.AddField(x => x.Password, field => field
    .Required("Password is required to secure your account")
    .WithMinLength(8, "Password must be at least 8 characters for security")
    .WithValidator(v => v.Any(char.IsUpper), "Include at least one uppercase letter")
    .WithValidator(v => v.Any(char.IsDigit), "Include at least one number"))

// Bad: Vague messages
.AddField(x => x.Password, field => field
    .Required("Required")
    .WithMinLength(8, "Too short"))
```

### Validate Early and Often

Use appropriate validation triggers:

```csharp
// Client-side validation for immediate feedback
.AddField(x => x.Email, field => field
    .WithLabel("Email")
    .WithEmailValidation("Please enter a valid email address"))

// Server-side async validation for expensive checks
.AddField(x => x.Username, field => field
    .WithLabel("Username")
    .WithMinLength(3, "Username must be at least 3 characters")
    .WithAsyncValidator(async username => {
        // Only hit the API after basic validation passes
        return await CheckUsernameAvailable(username);
    }, "This username is already taken"))
```

### Use FluentValidation for Complex Rules

For complex cross-field validation, use FluentValidation:

```csharp
public class OrderValidator : AbstractValidator<OrderModel>
{
    public OrderValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");

        RuleFor(x => x.ConfirmEmail)
            .Equal(x => x.Email)
            .WithMessage("Email addresses must match");

        RuleFor(x => x.Quantity)
            .Must((model, qty) => qty <= model.MaxAvailable)
            .WithMessage("Quantity exceeds available stock");
    }
}
```

## Field Dependencies

### Keep Dependencies Simple and Unidirectional

Avoid circular dependencies:

```csharp
// Good: Clear unidirectional flow
// Country -> State -> City

.AddField(x => x.Country, field => field.WithLabel("Country"))
.AddField(x => x.State, field => field
    .DependsOn(x => x.Country, (model, country) => {
        model.State = "";
        model.City = "";
    }))
.AddField(x => x.City, field => field
    .DependsOn(x => x.State, (model, state) => {
        model.City = "";
    }))

// Bad: Circular dependencies
// A -> B -> C -> A (will cause infinite loops)
```

### Reset Dependent Values

Always reset dependent fields when parent changes:

```csharp
.AddField(x => x.ProductCategory, field => field
    .WithLabel("Category")
    .WithSelectOptions(categories))
.AddField(x => x.Product, field => field
    .WithLabel("Product")
    .DependsOn(x => x.ProductCategory, (model, category) =>
    {
        // Always reset when parent changes
        model.Product = "";
        model.Quantity = 0;
        model.Price = 0;
    })
    .VisibleWhen(m => !string.IsNullOrEmpty(m.ProductCategory)))
```

## Performance

### Avoid Expensive Operations in Callbacks

Keep dependency callbacks fast:

```csharp
// Good: Fast synchronous operation
.DependsOn(x => x.Quantity, (model, qty) => {
    model.Total = qty * model.UnitPrice;
})

// Better for expensive operations: Use async and show loading state
.DependsOn(x => x.ZipCode, async (model, zip) => {
    if (zip?.Length == 5)
    {
        _isLoadingCity = true;
        StateHasChanged();

        model.City = await _locationService.GetCityAsync(zip);

        _isLoadingCity = false;
        StateHasChanged();
    }
})
```

### Debounce Expensive Validators

For API-based validation, implement debouncing:

```csharp
private CancellationTokenSource? _validationCts;

.AddField(x => x.Username, field => field
    .WithAsyncValidator(async username => {
        // Cancel previous validation
        _validationCts?.Cancel();
        _validationCts = new CancellationTokenSource();

        try
        {
            // Wait before validating (debounce)
            await Task.Delay(300, _validationCts.Token);
            return await _api.CheckUsernameAsync(username);
        }
        catch (OperationCanceledException)
        {
            return true; // Assume valid if cancelled
        }
    }, "Username is already taken"))
```

### Reuse Form Configurations

For frequently used form patterns, create reusable templates:

```csharp
public static class FormTemplates
{
    public static IFormConfiguration<T> CreateAddressForm<T>(
        Expression<Func<T, string>> streetExpr,
        Expression<Func<T, string>> cityExpr,
        Expression<Func<T, string>> stateExpr,
        Expression<Func<T, string>> zipExpr) where T : new()
    {
        return FormBuilder<T>
            .Create()
            .AddField(streetExpr, f => f.WithLabel("Street Address").Required())
            .AddField(cityExpr, f => f.WithLabel("City").Required())
            .AddField(stateExpr, f => f.WithLabel("State").Required())
            .AddField(zipExpr, f => f.WithLabel("ZIP Code").Required())
            .Build();
    }
}
```

## User Experience

### Show Loading States

Always indicate when async operations are in progress:

```razor
<FormCraftComponent TModel="MyModel"
                   Model="@model"
                   Configuration="@config"
                   OnValidSubmit="@HandleSubmit"
                   IsSubmitting="@_isSubmitting"
                   SubmittingText="Saving..." />
```

### Provide Visual Feedback

Use help text and placeholders to guide users:

```csharp
.AddField(x => x.Phone, field => field
    .WithLabel("Phone Number")
    .WithPlaceholder("(555) 123-4567")
    .WithHelpText("We'll only call you about your order"))

.AddField(x => x.Website, field => field
    .WithLabel("Website")
    .WithPlaceholder("https://example.com")
    .WithHelpText("Include https://"))
```

### Handle Empty States

Provide meaningful defaults and empty state handling:

```csharp
// Set sensible defaults in your model
public class OrderModel
{
    public int Quantity { get; set; } = 1;
    public string ShippingMethod { get; set; } = "Standard";
    public DateTime DeliveryDate { get; set; } = DateTime.Today.AddDays(7);
}
```

### Progressive Disclosure

Show fields progressively based on user input:

```csharp
.AddField(x => x.HasCompanyName, field => field
    .WithLabel("Are you ordering for a company?"))
.AddField(x => x.CompanyName, field => field
    .WithLabel("Company Name")
    .VisibleWhen(m => m.HasCompanyName)
    .Required("Company name is required when ordering for a company"))
.AddField(x => x.TaxId, field => field
    .WithLabel("Tax ID")
    .VisibleWhen(m => m.HasCompanyName)
    .WithHelpText("For tax-exempt purchases"))
```

## Security

### Never Trust Client-Side Validation

Always validate on the server:

```csharp
private async Task HandleSubmit(MyModel model)
{
    // Re-validate on server even though client validated
    var validationResult = await _validator.ValidateAsync(model);
    if (!validationResult.IsValid)
    {
        // Handle validation errors
        return;
    }

    await SaveData(model);
}
```

### Sanitize Input

Sanitize user input before storing or displaying:

```csharp
.AddField(x => x.Comment, field => field
    .WithLabel("Comment")
    .AsTextArea(lines: 4)
    .WithMaxLength(1000)) // Limit input length

// In your handler
private async Task HandleSubmit(MyModel model)
{
    // Sanitize before saving
    model.Comment = _sanitizer.Sanitize(model.Comment);
    await SaveData(model);
}
```

### Protect Sensitive Fields

Use appropriate input types and consider encryption:

```csharp
.AddField(x => x.Password, field => field
    .WithLabel("Password")
    .WithInputType("password")
    .Required("Password is required"))

.AddField(x => x.CreditCard, field => field
    .WithLabel("Credit Card Number")
    .WithInputType("password") // Hide on screen
    .WithPlaceholder("XXXX-XXXX-XXXX-XXXX"))
```

## Code Organization

### Separate Configuration from Component

Keep form configurations in separate files for large forms:

```csharp
// CustomerFormConfig.cs
public static class CustomerFormConfig
{
    public static IFormConfiguration<CustomerModel> Create()
    {
        return FormBuilder<CustomerModel>
            .Create()
            .AddField(x => x.Name, ConfigureNameField)
            .AddField(x => x.Email, ConfigureEmailField)
            .Build();
    }

    private static void ConfigureNameField(FieldBuilder<CustomerModel, string> field)
    {
        field.WithLabel("Customer Name")
             .Required("Name is required")
             .WithMinLength(2);
    }

    private static void ConfigureEmailField(FieldBuilder<CustomerModel, string> field)
    {
        field.WithLabel("Email Address")
             .Required("Email is required")
             .WithEmailValidation();
    }
}

// In component
protected override void OnInitialized()
{
    _config = CustomerFormConfig.Create();
}
```

### Use Extension Methods for Common Patterns

Create extension methods for frequently used configurations:

```csharp
public static class FieldBuilderExtensions
{
    public static FieldBuilder<TModel, string> AsPhoneNumber<TModel>(
        this FieldBuilder<TModel, string> builder) where TModel : new()
    {
        return builder
            .WithInputType("tel")
            .WithPlaceholder("(555) 123-4567")
            .WithValidator(
                v => string.IsNullOrEmpty(v) || Regex.IsMatch(v, @"^\(\d{3}\) \d{3}-\d{4}$"),
                "Please enter a valid phone number");
    }

    public static FieldBuilder<TModel, decimal> AsCurrency<TModel>(
        this FieldBuilder<TModel, decimal> builder,
        string currencySymbol = "$") where TModel : new()
    {
        return builder
            .WithHelpText($"Enter amount in {currencySymbol}")
            .WithRange(0, decimal.MaxValue, "Amount must be positive");
    }
}

// Usage
.AddField(x => x.Phone, field => field.WithLabel("Phone").AsPhoneNumber())
.AddField(x => x.Amount, field => field.WithLabel("Amount").AsCurrency())
```

## Testing

### Test Configuration Logic

Write unit tests for your form configurations:

```csharp
[Fact]
public void CustomerForm_Should_Have_Required_Name_Field()
{
    var config = CustomerFormConfig.Create();

    var nameField = config.Fields.First(f => f.FieldName == "Name");

    nameField.IsRequired.ShouldBeTrue();
    nameField.Validators.ShouldContain(v => v.ErrorMessage.Contains("required"));
}

[Fact]
public void CustomerForm_Should_Have_Email_Validation()
{
    var config = CustomerFormConfig.Create();

    var emailField = config.Fields.First(f => f.FieldName == "Email");

    emailField.Validators.ShouldNotBeEmpty();
}
```

### Test Visibility Conditions

```csharp
[Fact]
public void CompanyName_Should_Be_Visible_When_HasCompanyName_Is_True()
{
    var config = OrderFormConfig.Create();
    var companyField = config.Fields.First(f => f.FieldName == "CompanyName");

    var model = new OrderModel { HasCompanyName = true };

    companyField.VisibilityCondition(model).ShouldBeTrue();
}
```
