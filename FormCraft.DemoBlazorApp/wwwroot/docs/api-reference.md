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
Adds a boolean checkbox field with optional help text.

```csharp
.AddCheckboxField(x => x.AcceptTerms, "I accept the terms and conditions",
    helpText: "You must accept to continue")
```

To make the checkbox required, use `AddField` with `Required()`:

```csharp
.AddField(x => x.AcceptTerms, field => field
    .WithLabel("I accept the terms and conditions")
    .Required("You must accept the terms"))
```

For a single-choice group, use `AddDropdownField()` or `WithOptions()`:

```csharp
.AddField(x => x.Gender, field => field
    .WithLabel("Gender")
    .WithOptions(
        ("M", "Male"),
        ("F", "Female"),
        ("O", "Other")))
```

### Date/Time Fields

`DateTime` properties are rendered as date pickers automatically — just add them with `AddField`:

```csharp
.AddField(x => x.BirthDate, field => field
    .WithLabel("Birth Date")
    .Required("Birth date is required")
    .WithValidator(date => date <= DateTime.Today, "Date cannot be in the future"))
```

For attribute-based forms, `[DateField]` supports `MinDate`/`MaxDate` constraints.

### File Upload

#### AddFileUploadField()
Adds a file upload field.

```csharp
.AddFileUploadField(x => x.Resume, "Upload Resume",
    acceptedFileTypes: new[] { ".pdf", ".doc", ".docx" },
    maxFileSize: 5 * 1024 * 1024) // 5MB
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

#### ReadOnly() / Disabled() / DisabledWhen()
Controls field interactivity.

```csharp
.ReadOnly()                                  // Always read-only
.Disabled()                                  // Always disabled
.DisabledWhen(model => model.IsLocked)       // Conditionally disabled
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

#### Default Values
Set default values directly on the model — the form binds to the model instance you provide:

```csharp
public class MyModel
{
    public string Status { get; set; } = "active";
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
- `FormLayout.Inline`
- `FormLayout.Grid`

`WithLayout()` takes only the layout value — column counts are configured per
field group via `WithColumns()` (see Field Groups below).

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

Derive from `CustomFieldRendererBase<TValue>`:

```csharp
public class ColorPickerRenderer : CustomFieldRendererBase<string>
{
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            builder.OpenComponent<MudColorPicker>(0);
            builder.AddAttribute(1, "Value", GetValue(context) ?? "#000000");
            builder.CloseComponent();
        };
    }
}
```

Use the inherited `GetValue(context)` and `SetValue(context, value)` helpers to
read and write the field value.

### Using Custom Renderers

```csharp
// Type arguments: model, value, renderer
.AddField(x => x.FavoriteColor, field => field
    .WithLabel("Favorite Color")
    .WithCustomRenderer<MyModel, string, ColorPickerRenderer>())
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

To validate based on other field values, implement `IFieldValidator<TModel, TValue>` —
its `ValidateAsync` method receives the full model:

```csharp
public class AlternateEmailValidator : IFieldValidator<MyModel, string>
{
    public string? ErrorMessage { get; set; } =
        "Alternate email is required when primary email is provided";

    public Task<ValidationResult> ValidateAsync(
        MyModel model, string value, IServiceProvider services)
        => Task.FromResult(
            string.IsNullOrEmpty(model.Email) || !string.IsNullOrEmpty(value)
                ? ValidationResult.Success()
                : ValidationResult.Failure(ErrorMessage!));
}

// Usage
.AddField(x => x.AlternateEmail, field => field
    .WithValidator(new AlternateEmailValidator()))
```

### Dynamic Field Generation

Generate fields based on runtime data:

```csharp
var builder = FormBuilder<MyModel>.Create();

foreach (var fieldDef in dynamicFields)
{
    builder.AddField(
        fieldDef.PropertyExpression,
        field =>
        {
            field.WithLabel(fieldDef.Label);
            if (fieldDef.IsRequired)
                field.Required($"{fieldDef.Label} is required");
        });
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
- `RequiredValidator` - Ensures field has a value (added by `Required()`)
- `CustomValidator` - Custom validation logic (added by `WithValidator(func, message)`)
- `AsyncValidator` - Asynchronous validation (added by `WithAsyncValidator(func, message)`)
- `CollectionFieldValidator` - Validates collection field items
- `FluentValidationAdapter` - Bridges FluentValidation rules (added by `WithFluentValidation()`)

Helpers such as `WithEmailValidation()`, `WithMinLength()`, `WithMaxLength()`, and
`WithRange()` are built on top of `CustomValidator`.

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
    
    public string? ErrorMessage { get; set; } = "Username is already taken";
    
    public async Task<ValidationResult> ValidateAsync(
        UserModel model, 
        string value, 
        IServiceProvider services)
    {
        if (string.IsNullOrEmpty(value))
            return ValidationResult.Success();
            
        var exists = await _userService.UsernameExistsAsync(value);
        
        return exists 
            ? ValidationResult.Failure("Username is already taken")
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