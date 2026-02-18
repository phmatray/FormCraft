# API Reference

Complete API documentation for FormCraft.

## Core Classes

### FormBuilder<TModel>

The main entry point for creating form configurations.

```csharp
var config = FormBuilder<MyModel>.Create()
    // Add fields and configuration
    .Build();
```

### FormCraftComponent<TModel>

The Blazor component that renders the form.

```razor
<FormCraftComponent TModel="MyModel" 
                   Model="@model" 
                   Configuration="@config"
                   OnValidSubmit="@HandleSubmit" />
```

#### Parameters
- `Model` - The data model instance (required)
- `Configuration` - Form configuration from FormBuilder (required)
- `OnValidSubmit` - Callback when form is successfully submitted
- `OnFieldChanged` - Callback when any field value changes
- `ShowSubmitButton` - Whether to show submit button (default: true)
- `SubmitButtonText` - Text for submit button (default: "Submit")
- `SubmittingText` - Text while submitting (default: "Submitting...")
- `IsSubmitting` - Whether form is in submitting state
- `SubmitButtonClass` - CSS class for submit button

## Field Configuration Methods

### Basic Field Addition

#### AddField()
Core method for adding fields with lambda configuration.

```csharp
.AddField(x => x.PropertyName, field => field
    .WithLabel("Display Label")
    .Required()
    .WithPlaceholder("Enter value..."))
```

### Extension Methods for Common Fields

#### AddRequiredTextField()
Adds a required text field with built-in validation.

```csharp
.AddRequiredTextField(x => x.Name, "Full Name", "Enter your name", minLength: 2, maxLength: 50)
```

**Parameters:**
- `expression` - Property selector
- `label` - Field label
- `placeholder` - Optional placeholder text
- `minLength` - Minimum character length (default: 1)
- `maxLength` - Maximum character length (default: 255)

#### AddEmailField()
Adds an email field with format validation.

```csharp
.AddEmailField(x => x.Email, "Email Address", "your.email@example.com")
```

#### AddPasswordField()
Adds a password field with optional strength requirements.

```csharp
.AddPasswordField(x => x.Password, "Password", minLength: 8, requireSpecialChars: true)
```

#### AddPhoneField()
Adds a phone number field with format validation.

```csharp
.AddPhoneField(x => x.Phone, "Phone Number", required: true)
```

### Numeric Fields

#### AddNumericField()
Adds a numeric input with range validation.

```csharp
.AddNumericField(x => x.Age, "Age", min: 18, max: 100, required: true)
```

#### AddDecimalField()
Adds a decimal field for currency or percentages.

```csharp
.AddDecimalField(x => x.Price, "Price", min: 0, max: 1000, placeholder: "0.00")
```

#### AddCurrencyField()
Specialized decimal field for currency.

```csharp
.AddCurrencyField(x => x.Amount, "Amount", currencySymbol: "$")
```

### Selection Fields

#### AddDropdownField()
Adds a dropdown selection field.

```csharp
.AddDropdownField(x => x.Country, "Country",
    ("US", "United States"),
    ("CA", "Canada"),
    ("UK", "United Kingdom"))
```

#### AddCheckboxField()
Adds a boolean checkbox field.

```csharp
.AddCheckboxField(x => x.AcceptTerms, "I accept the terms and conditions", helpText: "Required")
```

### Date/Time Fields

For date fields, use the standard `AddField` method:

```csharp
// Date field
.AddField(x => x.BirthDate, field => field
    .WithLabel("Birth Date")
    .Required()
    .WithHelpText("Must be 18 or older"))

// DateTime field
.AddField(x => x.AppointmentTime, field => field
    .WithLabel("Appointment")
    .Required())
```

### File Upload

#### AddFileUploadField()
Adds a file upload field.

```csharp
.AddFileUploadField(x => x.Resume, "Upload Resume",
    acceptedFileTypes: new[] { ".pdf", ".doc", ".docx" },
    maxFileSize: 5 * 1024 * 1024) // 5MB
```

#### AddMultipleFileUploadField()
Adds a multiple file upload field.

```csharp
.AddMultipleFileUploadField(x => x.Documents, "Upload Documents",
    maxFiles: 3,
    acceptedFileTypes: new[] { ".pdf", ".jpg", ".png" },
    maxFileSize: 10 * 1024 * 1024) // 10MB per file
```

## Field Configuration Options

### Validation

#### Required()
Makes a field required.

```csharp
.AddField(x => x.Name, field => field
    .Required("Name is required"))
```

#### WithMinLength() / WithMaxLength()
Sets length constraints for string fields.

```csharp
.AddField(x => x.Description, field => field
    .WithMinLength(10, "Must be at least 10 characters")
    .WithMaxLength(500, "Cannot exceed 500 characters"))
```

#### WithRange()
Sets numeric range constraints.

```csharp
.AddField(x => x.Age, field => field
    .WithRange(18, 65, "Age must be between 18 and 65"))
```

#### WithValidator()
Adds custom validation logic.

```csharp
.AddField(x => x.Username, field => field
    .WithValidator(value => !value.Contains(" "), "Username cannot contain spaces"))
```

#### WithAsyncValidator()
Adds asynchronous validation.

```csharp
.AddField(x => x.Email, field => field
    .WithAsyncValidator(async value => await CheckEmailAvailability(value), "Email already exists"))
```

#### WithEmailValidation()
Adds email format validation.

```csharp
.AddField(x => x.Email, field => field
    .WithEmailValidation("Please enter a valid email address"))
```

#### WithFluentValidation()
Integrates FluentValidation validators registered in DI.

```csharp
.AddField(x => x.Email, field => field
    .WithFluentValidation(x => x.Email))
```

#### WithFluentValidator()
Uses a specific FluentValidation validator instance.

```csharp
var validator = new CustomerValidator();
.AddField(x => x.Name, field => field
    .WithFluentValidator(validator, x => x.Name))
```

### Appearance

#### WithLabel()
Sets the field label.

```csharp
.WithLabel("Display Name")
```

#### WithPlaceholder()
Sets placeholder text.

```csharp
.WithPlaceholder("Enter your name...")
```

#### WithHelpText()
Adds help text below the field.

```csharp
.WithHelpText("This will be displayed on your profile")
```

#### ReadOnly() / Disabled()
Controls field interactivity.

```csharp
.ReadOnly(true)
.Disabled(true)
```

#### DisabledWhen()
Conditionally disables field based on model state.

```csharp
.DisabledWhen(model => model.IsLocked)
```

#### WithCssClass()
Adds custom CSS classes.

```csharp
.WithCssClass("custom-field-style")
```

### Field Options

#### WithOptions()
Adds options for select fields.

```csharp
.AddField(x => x.Status, field => field
    .WithOptions(
        ("active", "Active"),
        ("inactive", "Inactive"),
        ("pending", "Pending")))
```

#### AsTextArea()
Configures a text field as a multi-line textarea.

```csharp
.AddField(x => x.Comments, field => field
    .AsTextArea(lines: 5, maxLength: 1000))
```

#### AsMultiSelect()
Enables multiple selection for select fields.

```csharp
.AddField(x => x.Skills, field => field
    .AsMultiSelect(
        ("csharp", "C#"),
        ("javascript", "JavaScript"),
        ("python", "Python")))
```

### Behavior

#### VisibleWhen()
Shows field only when condition is met.

```csharp
.AddField(x => x.City, field => field
    .VisibleWhen(model => !string.IsNullOrEmpty(model.Country)))
```

#### DependsOn()
Creates field dependencies with actions.

```csharp
.AddField(x => x.State, field => field
    .DependsOn(x => x.Country, (model, country) => {
        if (country != "US") {
            model.State = null;
        }
    }))
```

#### Setting Default Values
Default values should be set on your model directly:

```csharp
// In your model class
public class MyModel
{
    public string Status { get; set; } = "active";  // Default value
}

// Or initialize in your component
protected override void OnInitialized()
{
    model = new MyModel { Status = "active" };
}
```

## Form Configuration Options

### Layout

#### WithLayout()
Sets the form layout style.

```csharp
FormBuilder<MyModel>.Create()
    .WithLayout(FormLayout.Horizontal)
    // ... fields
    .Build();
```

Available layouts:
- `FormLayout.Vertical` (default)
- `FormLayout.Horizontal`
- `FormLayout.Grid`

### Field Groups

#### AddFieldGroup()
Groups related fields together.

```csharp
.AddFieldGroup(group => group
    .WithGroupName("Personal Information")
    .WithColumns(2)
    .ShowInCard()
    .AddField(x => x.FirstName, field => field.WithLabel("First Name"))
    .AddField(x => x.LastName, field => field.WithLabel("Last Name")))
```

## Custom Field Renderers

### Creating a Custom Renderer

Implement `IFieldRenderer`:

```csharp
public class ColorPickerRenderer : CustomFieldRendererBase<string>
{
    protected override RenderFragment RenderField(IFieldRenderContext<string> context)
    {
        return builder =>
        {
            builder.OpenComponent<MudColorPicker>(0);
            builder.AddAttribute(1, "Label", context.Field.Label);
            builder.AddAttribute(2, "Value", context.CurrentValue);
            builder.AddAttribute(3, "ValueChanged", context.OnValueChanged);
            builder.CloseComponent();
        };
    }
}
```

### Using Custom Renderers

```csharp
.AddField(x => x.FavoriteColor, field => field
    .WithLabel("Favorite Color")
    .WithCustomRenderer(new ColorPickerRenderer()))
```

## Advanced Features

### Form Templates

Create reusable form configurations:

```csharp
public static class FormTemplates
{
    public static IFormConfiguration<ContactModel> CreateContactForm()
    {
        return FormBuilder<ContactModel>.Create()
            .AddRequiredTextField(x => x.Name, "Name")
            .AddEmailField(x => x.Email)
            .AddPhoneField(x => x.Phone)
            .Build();
    }
}
```

### Conditional Validation

For validation that depends on other field values, use FluentValidation:

```csharp
// Create a FluentValidation validator
public class MyModelValidator : AbstractValidator<MyModel>
{
    public MyModelValidator()
    {
        RuleFor(x => x.AlternateEmail)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Alternate email is required when primary email is provided");
    }
}

// Register in DI
services.AddScoped<IValidator<MyModel>, MyModelValidator>();

// Use with FormCraft
.AddField(x => x.AlternateEmail, field => field
    .WithLabel("Alternate Email")
    .WithFluentValidation(x => x.AlternateEmail))
```

### Dynamic Field Generation

Generate fields based on runtime data:

```csharp
var builder = FormBuilder<MyModel>.Create();

foreach (var fieldDef in dynamicFields)
{
    builder.AddField(
        fieldDef.PropertyExpression,
        field => field
            .WithLabel(fieldDef.Label)
            .Required(fieldDef.IsRequired));
}

var config = builder.Build();
```

## Events and Callbacks

### OnFieldChanged

Handle individual field changes:

```csharp
<FormCraftComponent TModel="MyModel" 
                   Model="@model" 
                   Configuration="@config"
                   OnFieldChanged="@HandleFieldChange" />

@code {
    private Task HandleFieldChange((string fieldName, object? value) args)
    {
        Console.WriteLine($"Field {args.fieldName} changed to {args.value}");
        return Task.CompletedTask;
    }
}
```

### OnValidSubmit

Handle successful form submission:

```csharp
private async Task HandleValidSubmit(MyModel model)
{
    // Save to database, call API, etc.
    await SaveData(model);
}
```

## Validation System

### Built-in Validators

FormCraft includes these validators:
- `RequiredValidator` - Ensures field has a value
- `EmailValidator` - Validates email format
- `MinLengthValidator` - Minimum string length
- `MaxLengthValidator` - Maximum string length
- `RangeValidator` - Numeric range validation
- `RegexValidator` - Pattern matching
- `CustomValidator` - Custom validation logic
- `AsyncValidator` - Asynchronous validation

### Creating Custom Validators

Implement `IFieldValidator<TModel, TValue>`:

```csharp
public class UniqueUsernameValidator : IFieldValidator<UserModel, string>
{
    private readonly IUserService _userService;
    
    public UniqueUsernameValidator(IUserService userService)
    {
        _userService = userService;
    }
    
    public async Task<ValidationResult> ValidateAsync(
        UserModel model, 
        string value, 
        IServiceProvider services)
    {
        if (string.IsNullOrEmpty(value))
            return ValidationResult.Success();
            
        var exists = await _userService.UsernameExistsAsync(value);
        
        return exists 
            ? ValidationResult.Error("Username is already taken")
            : ValidationResult.Success();
    }
}
```

### Validation Messages

Multiple validators can add separate messages:

```csharp
.AddField(x => x.Email, field => field
    .Required("Email is required")
    .WithEmailValidation("Please enter a valid email address")
    .WithAsyncValidator(async value => 
        await CheckEmailNotInUse(value), 
        "This email is already registered"))
```

Messages are displayed with proper spacing using the `d-block` CSS class.

## FormBuilder Methods Reference

### Form Configuration

| Method | Description | Example |
|--------|-------------|---------|
| `Create()` | Static factory to create new builder | `FormBuilder<T>.Create()` |
| `Build()` | Creates immutable configuration | `.Build()` |
| `WithLayout()` | Sets form layout | `.WithLayout(FormLayout.Grid)` |
| `WithCssClass()` | Sets form CSS class | `.WithCssClass("my-form")` |
| `ShowValidationSummary()` | Shows/hides validation summary | `.ShowValidationSummary(true)` |
| `ShowRequiredIndicator()` | Shows/hides required indicator | `.ShowRequiredIndicator(true, "*")` |

### Field Addition Methods

| Method | Description | Example |
|--------|-------------|---------|
| `AddField()` | Adds field with configuration | `.AddField(x => x.Name, f => f.WithLabel("Name"))` |
| `AddFieldGroup()` | Adds a group of fields | `.AddFieldGroup(g => g.WithGroupName("Info"))` |

## FieldBuilder Methods Reference

### Core Configuration

| Method | Description |
|--------|-------------|
| `WithLabel(string)` | Sets field display label |
| `WithPlaceholder(string)` | Sets input placeholder text |
| `WithHelpText(string)` | Sets helper text below field |
| `WithCssClass(string)` | Adds CSS class to field |
| `WithInputType(string)` | Sets HTML input type (text, email, password, tel) |
| `WithOrder(int)` | Sets field display order |

### Validation Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `Required()` | `Required(string? message = null)` | Makes field required |
| `WithMinLength()` | `WithMinLength(int min, string? message = null)` | Minimum string length |
| `WithMaxLength()` | `WithMaxLength(int max, string? message = null)` | Maximum string length |
| `WithRange()` | `WithRange(T min, T max, string? message = null)` | Numeric range validation |
| `WithEmailValidation()` | `WithEmailValidation(string? message = null)` | Email format validation |
| `WithValidator()` | `WithValidator(Func<TValue, bool> predicate, string message)` | Custom sync validator |
| `WithAsyncValidator()` | `WithAsyncValidator(Func<TValue, Task<bool>> func, string message)` | Custom async validator |
| `WithFluentValidation()` | `WithFluentValidation(Expression<Func<TModel, TValue>> prop)` | Uses FluentValidation from DI |
| `WithFluentValidator()` | `WithFluentValidator(IValidator<TModel> validator, ...)` | Uses specific validator instance |

### State & Behavior

| Method | Signature | Description |
|--------|-----------|-------------|
| `ReadOnly()` | `ReadOnly(bool isReadOnly = true)` | Makes field read-only |
| `Disabled()` | `Disabled(bool isDisabled = true)` | Disables field |
| `DisabledWhen()` | `DisabledWhen(Func<TModel, bool> condition)` | Conditionally disabled |
| `VisibleWhen()` | `VisibleWhen(Func<TModel, bool> condition)` | Conditionally visible |
| `DependsOn()` | `DependsOn<TDep>(Expression<...>, Action<TModel, TDep>)` | Creates dependency |

### Field Type Configuration

| Method | Description |
|--------|-------------|
| `AsTextArea(int lines)` | Configures as multi-line textarea |
| `AsPassword(bool enableVisibilityToggle)` | Configures as password field |
| `AsSlider(T min, T max)` | Configures as slider input |
| `AsFileUpload(...)` | Configures as file upload |
| `AsMultipleFileUpload(...)` | Configures as multiple file upload |
| `WithOptions(...)` | Adds dropdown options |
| `WithSelectOptions(...)` | Adds select options from SelectOption list |
| `AsMultiSelect(...)` | Configures as multi-select |

### Attributes

| Method | Signature | Description |
|--------|-----------|-------------|
| `WithAttribute()` | `WithAttribute(string key, object value)` | Sets single attribute |
| `WithAttributes()` | `WithAttributes(Dictionary<string, object>)` | Sets multiple attributes |
| `WithCustomRenderer()` | `WithCustomRenderer(ICustomFieldRenderer<TValue>)` | Uses custom renderer |
| `WithCustomTemplate()` | `WithCustomTemplate(RenderFragment<...>)` | Uses custom template |

### MudBlazor-Specific Extensions

| Method | Description |
|--------|-------------|
| `WithAdornment(icon, position, color)` | Adds icon adornment to field |
| `WithVariant(Variant)` | Sets MudBlazor variant style |
| `WithMargin(Margin)` | Sets MudBlazor margin |

## FieldGroupBuilder Methods Reference

| Method | Description |
|--------|-------------|
| `AddField()` | Adds field to the group |
| `WithGroupName(string)` | Sets group display name |
| `WithColumns(int)` | Sets number of columns |
| `WithCssClass(string)` | Sets group CSS class |
| `ShowInCard()` | Renders group in a card |
| `WithOrder(int)` | Sets group display order |
| `WithHeaderRightContent(RenderFragment)` | Adds content to right of header |

## FluentFormBuilderExtensions Reference

Convenience methods for common field patterns:

| Method | Parameters |
|--------|------------|
| `AddRequiredTextField()` | `expression, label, placeholder?, minLength?, maxLength?` |
| `AddEmailField()` | `expression, label?, placeholder?` |
| `AddPasswordField()` | `expression, label?, minLength?, requireSpecialChars?` |
| `AddPhoneField()` | `expression, label?, required?` |
| `AddNumericField()` | `expression, label, min?, max?, required?` |
| `AddDecimalField()` | `expression, label, min?, max?, placeholder?` |
| `AddCurrencyField()` | `expression, label, currencySymbol?` |
| `AddPercentageField()` | `expression, label, min?, max?` |
| `AddDropdownField()` | `expression, label, options...` |
| `AddCheckboxField()` | `expression, label, helpText?` |
| `AddTextArea()` | `expression, label, lines?, maxLength?` |
| `AddFileUploadField()` | `expression, label, acceptedTypes?, maxSize?` |
| `AddMultipleFileUploadField()` | `expression, label, maxFiles?, acceptedTypes?, maxSize?` |

## Security Features

### SecurityBuilder Methods

```csharp
.WithSecurity(security => security
    .EncryptField(x => x.SSN)
    .EnableCsrfProtection()
    .WithRateLimit(maxRequests: 5, window: TimeSpan.FromMinutes(1))
    .EnableAuditLogging())
```

| Method | Description |
|--------|-------------|
| `EncryptField()` | Enables encryption for sensitive field |
| `EnableCsrfProtection()` | Enables CSRF token validation |
| `WithRateLimit()` | Sets rate limiting for form submission |
| `EnableAuditLogging()` | Enables audit logging for form actions |

## Service Registration

### Basic Setup

```csharp
// In Program.cs
builder.Services.AddFormCraft();
builder.Services.AddMudServices();
```

### With Custom Options

```csharp
builder.Services.AddFormCraft(options =>
{
    options.DefaultLayout = FormLayout.Vertical;
    options.ShowRequiredIndicator = true;
});
```

## Type Support

FormCraft automatically renders appropriate inputs for these types:

| .NET Type | Rendered Component |
|-----------|-------------------|
| `string` | MudTextField |
| `int`, `long` | MudNumericField |
| `decimal`, `double`, `float` | MudNumericField |
| `bool` | MudCheckBox |
| `DateTime`, `DateOnly` | MudDatePicker |
| `TimeSpan`, `TimeOnly` | MudTimePicker |
| `Enum` | MudSelect |
| `IBrowserFile` | MudFileUpload |
| `IReadOnlyList<IBrowserFile>` | MudFileUpload (multiple) |

## Input Type Mapping

When using `WithInputType()`, these HTML types map to MudBlazor InputType:

| HTML Type | MudBlazor InputType |
|-----------|---------------------|
| `"text"` | InputType.Text |
| `"email"` | InputType.Email |
| `"password"` | InputType.Password |
| `"tel"` | InputType.Telephone |
| `"url"` | InputType.Url |
| `"number"` | InputType.Number |

## Common Attribute Keys

When using `WithAttribute()`, these keys have special meaning:

| Key | Type | Description |
|-----|------|-------------|
| `"Lines"` | int | Number of lines for textarea |
| `"MaxLength"` | int | Maximum input length |
| `"Counter"` | int | Shows character counter |
| `"Immediate"` | bool | Updates on each keystroke |
| `"Clearable"` | bool | Shows clear button |