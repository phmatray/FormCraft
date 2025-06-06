# DynamicFormBlazor

A .NET 9.0 Blazor Server application demonstrating dynamic form generation with type-safe field definitions and form validation using FluentValidation.

## Features

- **Dynamic Form Generation**: Create forms dynamically at runtime using field definitions
- **Type-Safe Field Rendering**: Generic field definitions ensure compile-time type checking
- **Multiple Field Types Support**:
  - Text fields (string)
  - Numeric fields (int, double, decimal)
  - Boolean fields (checkbox)
  - Date fields (DateTime)
  - Select/dropdown fields with options
- **Form Validation**: Comprehensive validation using FluentValidation
- **Material Design UI**: Beautiful UI components using MudBlazor

## Project Structure

```
DynamicFormBlazor/
├── Components/
│   ├── DynamicForm.razor          # Core dynamic form component
│   ├── Layout/
│   │   └── MainLayout.razor       # Application layout
│   └── Pages/
│       ├── Home.razor             # Demo page for dynamic forms
│       └── Validation.razor       # Form validation showcase
├── Models/
│   ├── FieldDefinition.cs         # Field definition classes
│   └── Person.cs                  # Model for validation demo
├── Validators/
│   ├── PersonValidator.cs         # FluentValidation rules
│   └── FluentValidationValidator.cs # Validation adapter
└── Program.cs                     # Application entry point
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022, VS Code, or any compatible IDE

### Running the Application

1. Clone the repository:
```bash
git clone https://github.com/yourusername/DynamicFormBlazor.git
cd DynamicFormBlazor
```

2. Restore dependencies and run:
```bash
cd DynamicFormBlazor
dotnet run
```

3. Open your browser and navigate to:
   - HTTPS: https://localhost:7200
   - HTTP: http://localhost:5228

## Usage Examples

### Creating a Dynamic Form

```csharp
@page "/"
@using DynamicFormBlazor.Models

<DynamicForm Fields="@fields" OnSubmit="@HandleFormSubmit" />

@code {
    private List<FieldDefinition> fields = new()
    {
        new FieldDefinition<string> 
        { 
            Key = "firstName", 
            Label = "First Name", 
            DefaultValue = "" 
        },
        new FieldDefinition<int> 
        { 
            Key = "age", 
            Label = "Age", 
            DefaultValue = 0 
        },
        new SelectFieldDefinition<string>
        {
            Key = "color",
            Label = "Favorite Color",
            Options = new List<FieldOption<string>>
            {
                new() { Value = "red", Label = "Red" },
                new() { Value = "blue", Label = "Blue" },
                new() { Value = "green", Label = "Green" }
            }
        }
    };

    private void HandleFormSubmit(Dictionary<string, object> formData)
    {
        // Process form data
        foreach (var kvp in formData)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
    }
}
```

### Adding Custom Field Types

To add a new field type, extend the `FieldDefinition<T>` class:

```csharp
public class CustomFieldDefinition<T> : FieldDefinition<T>
{
    public override RenderFragment GenerateRenderFragment(Dictionary<string, object> model)
    {
        // Return a RenderFragment that renders your custom field
        return builder =>
        {
            // Your custom rendering logic here
        };
    }
}
```

### Form Validation

The project includes a comprehensive validation example using FluentValidation:

1. Create a model class
2. Define validation rules using FluentValidation
3. Apply validation to your form fields

See the `/validation` page for a complete example.

## Key Components

### FieldDefinition

The base class for all field definitions:
- `Key`: Unique identifier for the field
- `Label`: Display label
- `DefaultValue`: Initial value
- `GenerateRenderFragment()`: Abstract method for rendering

### DynamicForm Component

The main form component that:
- Accepts a list of `FieldDefinition` objects
- Manages form state using a `Dictionary<string, object>`
- Handles form submission via `EventCallback`

### Field Types

- `FieldDefinition<T>`: Generic implementation for common types
- `SelectFieldDefinition<T>`: Specialized field for dropdowns

## Dependencies

- **MudBlazor** (v8.7.0): Material Design component library
- **FluentValidation** (v12.0.0): Validation library

## Dynamic Form API Improvements

This project showcases both the current dynamic form implementation and concepts for a significantly improved API. Visit `/improved` to see the architectural improvements that have been designed:

### Current Implementation (Working)
- Dictionary-based form model
- Basic field type support
- Manual RenderFragment generation
- Simple validation examples

### Improved API Design (Conceptual)
We've designed a comprehensive improvement to the dynamic form API with the following features:

#### ✅ **Type-Safe Form Builder**
```csharp
var form = FormBuilder<ContactModel>
    .Create()
    .AddField(x => x.FirstName)
        .WithLabel("First Name")
        .Required("First name is required")
        .WithMinLength(2, "Must be at least 2 characters")
    .Build();
```

#### ✅ **Comprehensive Validation System**
- Built-in validators (Required, Email, Range, MinLength, MaxLength)
- Custom synchronous and asynchronous validators
- Fluent validation integration
- Real-time field-level validation

#### ✅ **Advanced Field Types**
- Text areas with character limits
- File upload with size/type restrictions
- Multi-select dropdowns
- Slider inputs for numeric ranges
- Date/time pickers

#### ✅ **Field Dependencies & Conditional Logic**
```csharp
.AddField(x => x.City)
    .VisibleWhen(m => !string.IsNullOrEmpty(m.Country))
    .DependsOn(x => x.Country, (model, country) => {
        if (string.IsNullOrEmpty(country)) model.City = null;
    })
```

#### ✅ **Form State Management**
- Dirty state tracking
- Change detection
- Form reset functionality
- Validation state management

#### ✅ **Extensible Architecture**
- Plugin system for custom field renderers
- Service-based dependency injection
- Configurable field layouts
- Theme and styling support

### Implementation Status

The architectural foundation has been designed and demonstrates:
- **Core Interfaces**: IFieldConfiguration, IFieldRenderer, IFieldValidator
- **Builder Pattern**: FormBuilder and FieldBuilder classes
- **Extension Methods**: Fluent API for common scenarios
- **Service Integration**: Dependency injection setup

While the complete implementation requires additional work to fully integrate with MudBlazor's component system, the foundation provides a clear path toward a modern, type-safe dynamic form solution.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the [MIT License](LICENSE).