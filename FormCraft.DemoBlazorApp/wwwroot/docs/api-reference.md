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
.AddCheckboxField(x => x.AcceptTerms, "I accept the terms and conditions", required: true)
```

#### AddRadioGroupField()
Adds a radio button group.

```csharp
.AddRadioGroupField(x => x.Gender, "Gender",
    ("M", "Male"),
    ("F", "Female"),
    ("O", "Other"))
```

### Date/Time Fields

#### AddDateField()
Adds a date picker field.

```csharp
.AddDateField(x => x.BirthDate, "Birth Date", min: DateTime.Now.AddYears(-100), max: DateTime.Now)
```

#### AddDateTimeField()
Adds a date and time picker.

```csharp
.AddDateTimeField(x => x.AppointmentTime, "Appointment", required: true)
```

### File Upload

#### AddFileUploadField()
Adds a file upload field.

```csharp
.AddFileUploadField(x => x.Resume, "Upload Resume", accept: ".pdf,.doc,.docx", maxSize: 5242880)
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

#### IsReadOnly() / IsDisabled()
Controls field interactivity.

```csharp
.IsReadOnly(true)
.IsDisabled(model => model.IsLocked)
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

#### WithDefaultValue()
Sets a default value for the field.

```csharp
.AddField(x => x.Status, field => field
    .WithDefaultValue("active"))
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

Validate based on other field values:

```csharp
.AddField(x => x.AlternateEmail, field => field
    .WithValidator((value, model) => 
        string.IsNullOrEmpty(model.Email) || !string.IsNullOrEmpty(value),
        "Alternate email is required when primary email is provided"))
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