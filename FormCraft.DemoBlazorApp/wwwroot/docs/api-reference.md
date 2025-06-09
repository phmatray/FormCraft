# API Reference

Complete reference for the Dynamic Form Blazor API.

## FormBuilder<TModel>

The main entry point for building dynamic forms.

### Create()
Creates a new form builder instance.

```csharp
var builder = FormBuilder<MyModel>.Create();
```

### Layout Methods

#### WithLayout(FormLayout layout)
Sets the overall form layout style.

```csharp
.WithLayout(FormLayout.Horizontal) // Two columns
.WithLayout(FormLayout.Grid)       // Three columns
.WithLayout(FormLayout.Vertical)   // Single column (default)
```

## Field Builder Methods

### Text Fields

#### AddRequiredTextField()
Adds a required text field with built-in validation.

```csharp
.AddRequiredTextField(x => x.Name, "Full Name", "Enter your name", minLength: 2)
```

**Parameters:**
- `expression` - Property selector
- `label` - Field label
- `placeholder` - Optional placeholder text
- `minLength` - Minimum character length (default: 1)
- `maxLength` - Maximum character length (default: 255)

#### AddEmailField()
Adds an email field with email format validation.

```csharp
.AddEmailField(x => x.Email, "Email Address")
```

#### AddPasswordField()
Adds a password field with strength requirements.

```csharp
.AddPasswordField(x => x.Password, "Password", minLength: 8, requireSpecialChars: true)
```

### Numeric Fields

#### AddNumericField()
Adds a numeric input with range validation.

```csharp
.AddNumericField(x => x.Age, "Age", min: 18, max: 100)
```

### Selection Fields

#### AddDropdownField()
Adds a dropdown selection field.

```csharp
.AddDropdownField(x => x.Country, "Country",
    ("US", "United States"),
    ("CA", "Canada"),
    ("UK", "United Kingdom")
)
```

#### AddCheckboxField()
Adds a checkbox field.

```csharp
.AddCheckboxField(x => x.AcceptTerms, "I accept the terms and conditions")
```

## Field Configuration Methods

### Validation

#### Required()
Makes a field required.

```csharp
.AddField(x => x.Name)
    .Required("Name is required")
```

#### WithMinLength() / WithMaxLength()
Sets length constraints.

```csharp
.AddField(x => x.Description)
    .WithMinLength(10, "Must be at least 10 characters")
    .WithMaxLength(500, "Cannot exceed 500 characters")
```

#### WithValidator()
Adds custom validation logic.

```csharp
.AddField(x => x.Username)
    .WithValidator(value => !value.Contains(" "), "Username cannot contain spaces")
```

### Appearance

#### WithLabel()
Sets the field label.

```csharp
.WithLabel("Custom Label")
```

#### WithPlaceholder()
Sets placeholder text.

```csharp
.WithPlaceholder("Enter value here...")
```

#### WithHelpText()
Adds help text below the field.

```csharp
.WithHelpText("This information helps us contact you")
```

#### WithCssClass()
Adds custom CSS classes.

```csharp
.WithCssClass("my-custom-field")
```

### Behavior

#### VisibleWhen()
Shows field only when condition is met.

```csharp
.AddField(x => x.City)
    .VisibleWhen(model => !string.IsNullOrEmpty(model.Country))
```

#### DependsOn()
Creates field dependencies with actions.

```csharp
.AddField(x => x.State)
    .DependsOn(x => x.Country, (model, country) => {
        if (country != "US") {
            model.State = null;
        }
    })
```

## Component Properties

### FormCraftComponent<TModel>

#### Required Parameters
- `Model` - The data model instance
- `Configuration` - Form configuration from FormBuilder

#### Optional Parameters
- `OnValidSubmit` - Callback when form is successfully submitted
- `OnFieldChanged` - Callback when any field value changes
- `ShowSubmitButton` - Whether to show the submit button (default: true)
- `SubmitButtonText` - Text for submit button (default: "Submit")
- `IsSubmitting` - Whether form is in submitting state

#### Example
```html
<FormCraftComponent TModel="ContactModel" 
                   Model="@model" 
                   Configuration="@formConfiguration"
                   OnValidSubmit="@HandleSubmit"
                   OnFieldChanged="@OnFieldValueChanged"
                   ShowSubmitButton="true"
                   SubmitButtonText="Save Contact"
                   SubmittingText="Saving..."
                   IsSubmitting="@isSubmitting" />
```

## Validation System

### Built-in Validators

- **RequiredValidator** - Ensures field has a value
- **EmailValidator** - Validates email format
- **LengthValidator** - Validates string length
- **RangeValidator** - Validates numeric ranges
- **RegexValidator** - Custom regex patterns

### Custom Validators

Implement `IFieldValidator<TModel, TValue>`:

```csharp
public class CustomValidator : IFieldValidator<MyModel, string>
{
    public async Task<ValidationResult> ValidateAsync(MyModel model, string value, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 3)
        {
            return ValidationResult.Error("Value must be at least 3 characters");
        }
        
        return ValidationResult.Success();
    }
}
```