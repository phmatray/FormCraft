# Getting Started

Welcome to FormCraft! This powerful library allows you to create dynamic, type-safe forms in Blazor applications with minimal code.

## Overview

FormCraft provides a fluent API for building forms that are:
- **Type-safe** - Full IntelliSense support and compile-time checking
- **Flexible** - Supports complex validation rules and field dependencies
- **Extensible** - Easy to customize with your own field types and validators
- **Beautiful** - Powered by MudBlazor components
- **Responsive** - Works great on desktop and mobile devices

## Installation

### 1. Install the Package

Add FormCraft to your Blazor project:

```bash
dotnet add package FormCraft
dotnet add package FormCraft.ForMudBlazor
```

### 2. Configure Services

In your `Program.cs`, add the required services:

```csharp
// Add MudBlazor services
builder.Services.AddMudServices();

// Add FormCraft services
builder.Services.AddFormCraft();
builder.Services.AddFormCraftMudBlazor();
```

### 3. Add Required References

In your `_Imports.razor` file, add:

```razor
@using FormCraft
@using FormCraft.ForMudBlazor
@using MudBlazor
```

In your `index.html` (WebAssembly) or `_Host.cshtml` (Server), add MudBlazor references:

```html
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

## Quick Start Example

### 1. Create Your Model

```csharp
public class ContactModel
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public string Country { get; set; } = "";
    public string Message { get; set; } = "";
}
```

### 2. Build Your Form Configuration

```csharp
@code {
    private ContactModel model = new();
    private IFormConfiguration<ContactModel> formConfig;

    protected override void OnInitialized()
    {
        formConfig = FormBuilder<ContactModel>
            .Create()
            .AddRequiredTextField(x => x.FirstName, "First Name", minLength: 2)
            .AddRequiredTextField(x => x.LastName, "Last Name", minLength: 2)
            .AddEmailField(x => x.Email)
            .AddNumericField(x => x.Age, "Age", min: 18, max: 100)
            .AddDropdownField(x => x.Country, "Country",
                ("US", "United States"),
                ("CA", "Canada"),
                ("UK", "United Kingdom"))
            .AddField(x => x.Message, field => field
                .WithLabel("Message")
                .AsTextArea(lines: 5)
                .Required("Please enter a message"))
            .Build();
    }

    private async Task HandleSubmit(ContactModel submittedModel)
    {
        // Handle form submission
        await Task.CompletedTask;
    }
}
```

### 3. Display the Form

```razor
@page "/contact"

<MudContainer MaxWidth="MaxWidth.Small">
    <MudPaper Class="pa-4">
        <MudText Typo="Typo.h4" Class="mb-4">Contact Form</MudText>
        
        <FormCraftComponent TModel="ContactModel" 
                           Model="@model" 
                           Configuration="@formConfig"
                           OnValidSubmit="@HandleSubmit"
                           ShowSubmitButton="true"
                           SubmitButtonText="Send Message" />
    </MudPaper>
</MudContainer>
```

## Understanding the API

### AddField with Lambda Configuration

The core API uses a lambda-based configuration pattern:

```csharp
.AddField(x => x.PropertyName, field => field
    .WithLabel("Display Label")
    .Required("Custom error message")
    .WithPlaceholder("Placeholder text")
    .WithHelpText("Help text for users"))
```

### Extension Methods for Common Fields

FormCraft provides convenience methods for common field types:

- `AddRequiredTextField()` - Text input with required validation
- `AddEmailField()` - Email input with format validation
- `AddNumericField()` - Number input with range validation
- `AddDropdownField()` - Select dropdown with options
- `AddPasswordField()` - Password input with strength validation
- `AddPhoneField()` - Phone number input with format validation
- `AddDateField()` - Date picker
- `AddCheckboxField()` - Boolean checkbox
- `AddFileUploadField()` - File upload control

## Form Features

### Validation

FormCraft uses FluentValidation internally. Browser HTML5 validation is disabled in favor of custom validation messages:

```csharp
.AddField(x => x.Password, field => field
    .WithLabel("Password")
    .Required("Password is required")
    .WithMinLength(8, "Password must be at least 8 characters")
    .WithValidator(value => value.Any(char.IsDigit), "Password must contain a number"))
```

### Field Dependencies

Create reactive forms where fields depend on each other:

```csharp
.AddField(x => x.State, field => field
    .WithLabel("State")
    .VisibleWhen(model => model.Country == "US")
    .DependsOn(x => x.Country, (model, country) => {
        if (country != "US") model.State = "";
    }))
```

### Custom Field Renderers

Create custom field types for specialized inputs:

```csharp
.AddField(x => x.Color, field => field
    .WithLabel("Favorite Color")
    .WithCustomRenderer(new ColorPickerRenderer()))
```

## Next Steps

- **[API Reference](/docs/api-reference)** - Detailed API documentation
- **[Examples](/docs/examples)** - Common form patterns and use cases
- **[Customization](/docs/customization)** - Advanced customization options
- **[Troubleshooting](/docs/troubleshooting)** - Common issues and solutions

## Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/phmatray/FormCraft/issues)
- **Discussions**: [Ask questions and share ideas](https://github.com/phmatray/FormCraft/discussions)
- **Documentation**: [Full documentation](https://phmatray.github.io/FormCraft/)