# Getting Started

Welcome to Dynamic Form Blazor! This powerful library allows you to create dynamic, type-safe forms in Blazor applications with minimal code.

## Overview

Dynamic Form Blazor provides a fluent API for building forms that are:
- **Type-safe** - Full IntelliSense support and compile-time checking
- **Flexible** - Supports complex validation rules and field dependencies
- **Extensible** - Easy to customize with your own field types and validators
- **Responsive** - Works great on desktop and mobile devices

## Quick Start

### 1. Installation

Add the package to your Blazor project:

```bash
dotnet add package FormCraft
```

### 2. Basic Usage

Create a model for your form:

```csharp
public class ContactModel
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
}
```

### 3. Build Your Form

Use the fluent API to configure your form:

```csharp
var formConfiguration = FormBuilder<ContactModel>
    .Create()
    .AddRequiredTextField(x => x.FirstName, "First Name")
    .AddRequiredTextField(x => x.LastName, "Last Name")
    .AddEmailField(x => x.Email)
    .AddNumericField(x => x.Age, "Age", 18, 100)
    .Build();
```

### 4. Display the Form

Add the form component to your Razor page:

```html
<FormCraftComponent TModel="ContactModel" 
                   Model="@model" 
                   Configuration="@formConfiguration"
                   OnValidSubmit="@HandleSubmit"
                   ShowSubmitButton="true" />
```

## Next Steps

- Check out the [API Reference](/docs/api-reference) for detailed documentation
- Browse [Examples](/docs/examples) to see common patterns
- Learn about [Customization](/docs/customization) options

## Key Features

### Fluent Builder Pattern
Build forms with an intuitive, chainable API that reduces boilerplate code.

### Built-in Validation
Includes common validators like required fields, email validation, min/max length, and numeric ranges.

### Field Dependencies
Create dynamic forms where fields can show/hide or change based on other field values.

### Custom Templates
Override the default rendering for any field with your own Blazor components.

### Layout Options
Choose from multiple layout styles including horizontal, vertical, and grid layouts.