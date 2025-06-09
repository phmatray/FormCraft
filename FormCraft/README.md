# FormCraft

A powerful, type-safe dynamic form library for Blazor applications with fluent API design.

🌐 **[Live Demo](https://phmatray.github.io/FormCraft/)** | 📚 **[Documentation](https://phmatray.github.io/FormCraft/docs/getting-started)**

## Features

- **Type-Safe**: Strongly typed form builders with compile-time validation
- **Fluent API**: Intuitive, readable form configuration
- **Dynamic Validation**: Built-in and custom validators with async support
- **Field Dependencies**: Fields can react to changes in other fields
- **Multiple Layouts**: Vertical, horizontal, grid, and inline layouts
- **Extensible Rendering**: Custom field renderers for any data type
- **MudBlazor Integration**: Beautiful UI components out of the box

## Quick Start

1. Install the NuGet package:
```bash
dotnet add package FormCraft
```

2. Register services in `Program.cs`:
```csharp
builder.Services.AddDynamicForms();
```

3. Create a form:
```csharp
var configuration = FormBuilder<MyModel>.Create()
    .AddRequiredTextField(x => x.Name, "Full Name")
    .AddEmailField(x => x.Email)
    .AddNumericField(x => x.Age, "Age", min: 18, max: 100)
    .Build();
```

4. Render the form:
```razor
<DynamicFormComponent TModel="MyModel" 
                     Model="@myModel" 
                     Configuration="@configuration"
                     OnValidSubmit="@HandleSubmit" />
```

## Documentation

For detailed documentation and examples, visit our [interactive documentation site](https://phmatray.github.io/FormCraft/docs/getting-started).

## License

This project is licensed under the MIT License.