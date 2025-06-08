# FormCraft 🎨

<div align="center">

[![NuGet Version](https://img.shields.io/nuget/v/FormCraft.svg?style=flat-square)](https://www.nuget.org/packages/FormCraft/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FormCraft.svg?style=flat-square)](https://www.nuget.org/packages/FormCraft/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/phmatray/DynamicFormBlazor/ci.yml?branch=main&style=flat-square)](https://github.com/phmatray/DynamicFormBlazor/actions)
[![License](https://img.shields.io/github/license/phmatray/DynamicFormBlazor?style=flat-square)](https://github.com/phmatray/DynamicFormBlazor/blob/main/LICENSE)
[![Stars](https://img.shields.io/github/stars/phmatray/DynamicFormBlazor?style=flat-square)](https://github.com/phmatray/DynamicFormBlazor/stargazers)

**Build type-safe, dynamic forms in Blazor with ease** ✨

[Get Started](#-quick-start) • [Documentation](#-documentation) • [Examples](#-examples) • [Contributing](CONTRIBUTING.md)

</div>

---

## 🚀 Why FormCraft?

FormCraft revolutionizes form building in Blazor applications by providing a **fluent, type-safe API** that makes complex forms simple. Say goodbye to repetitive form markup and hello to elegant, maintainable code.

### ✨ Key Features

- 🔒 **Type-Safe** - Full IntelliSense support with compile-time validation
- 🎯 **Fluent API** - Intuitive method chaining for readable form configuration
- 🎨 **MudBlazor Integration** - Beautiful Material Design components out of the box
- 🔄 **Dynamic Forms** - Create forms that adapt based on user input
- ✅ **Advanced Validation** - Built-in, custom, and async validators
- 🔗 **Field Dependencies** - Link fields together with reactive updates
- 📐 **Flexible Layouts** - Multiple layout options to fit your design
- 🚀 **High Performance** - Optimized rendering with minimal overhead
- 🧪 **Fully Tested** - 400+ unit tests ensuring reliability

## 📦 Installation

```bash
dotnet add package FormCraft
```

## 🎯 Quick Start

### 1. Register Services

```csharp
// Program.cs
builder.Services.AddDynamicForms();
```

### 2. Create Your Model

```csharp
public class UserRegistration
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public string Country { get; set; }
    public bool AcceptTerms { get; set; }
}
```

### 3. Build Your Form

```csharp
@page "/register"
@using FormCraft

<h3>User Registration</h3>

<DynamicFormComponent TModel="UserRegistration" 
                     Model="@model" 
                     Configuration="@formConfig"
                     OnValidSubmit="@HandleSubmit" />

@code {
    private UserRegistration model = new();
    private IFormConfiguration<UserRegistration> formConfig;

    protected override void OnInitialized()
    {
        formConfig = FormBuilder<UserRegistration>.Create()
            .AddRequiredTextField(x => x.FirstName, "First Name")
            .AddRequiredTextField(x => x.LastName, "Last Name")
            .AddEmailField(x => x.Email)
            .AddNumericField(x => x.Age, "Age", min: 18, max: 120)
            .AddSelectField(x => x.Country, "Country", GetCountries())
            .AddCheckboxField(x => x.AcceptTerms, "I accept the terms and conditions")
                .IsRequired("You must accept the terms")
            .Build();
    }

    private async Task HandleSubmit(UserRegistration model)
    {
        // Handle form submission
        await UserService.RegisterAsync(model);
    }

    private List<SelectOption<string>> GetCountries() => new()
    {
        new("us", "United States"),
        new("uk", "United Kingdom"),
        new("ca", "Canada"),
        new("au", "Australia")
    };
}
```

## 🎨 Examples

### Dynamic Field Dependencies

Create forms where fields react to each other:

```csharp
var formConfig = FormBuilder<OrderForm>.Create()
    .AddSelectField(x => x.ProductType, "Product Type", productOptions)
    .AddSelectField(x => x.ProductModel, "Model", 
        dependsOn: x => x.ProductType,
        optionsProvider: (productType) => GetModelsForType(productType))
    .AddNumericField(x => x.Quantity, "Quantity", min: 1)
    .AddField(x => x.TotalPrice, "Total Price")
        .IsReadOnly()
        .DependsOn(x => x.ProductModel, x => x.Quantity)
        .WithValueProvider((model, _) => CalculatePrice(model))
    .Build();
```

### Custom Validation

Add complex validation logic with ease:

```csharp
.AddField(x => x.Username)
    .WithValidator(new CustomValidator<User, string>(
        username => !forbiddenUsernames.Contains(username.ToLower()),
        "This username is not available"))
    .WithAsyncValidator(async (username, services) =>
    {
        var userService = services.GetRequiredService<IUserService>();
        return await userService.IsUsernameAvailableAsync(username);
    }, "Username is already taken")
```

### Multiple Layouts

Choose the layout that fits your design:

```csharp
// Vertical Layout (default)
.WithLayout(FormLayout.Vertical)

// Horizontal Layout
.WithLayout(FormLayout.Horizontal)

// Grid Layout
.WithLayout(FormLayout.Grid, columns: 2)

// Inline Layout
.WithLayout(FormLayout.Inline)
```

### Advanced Field Types

```csharp
// Password field with confirmation
.AddPasswordField(x => x.Password, "Password")
    .WithHelpText("Must be at least 8 characters")
.AddPasswordField(x => x.ConfirmPassword, "Confirm Password")
    .MustMatch(x => x.Password, "Passwords do not match")

// Date picker with constraints
.AddDateField(x => x.BirthDate, "Date of Birth")
    .WithMaxDate(DateTime.Today.AddYears(-18))
    .WithHelpText("Must be 18 or older")

// Multi-line text with character limit
.AddTextAreaField(x => x.Description, "Description", rows: 5)
    .WithMaxLength(500)
    .WithHelpText("Maximum 500 characters")

// File upload (coming soon)
.AddFileField(x => x.ProfilePicture, "Profile Picture")
    .AcceptOnly(".jpg", ".png")
    .WithMaxSize(5 * 1024 * 1024) // 5MB
```

## 🛠️ Advanced Features

### Conditional Fields

Show/hide fields based on conditions:

```csharp
.AddField(x => x.CompanyName)
    .VisibleWhen(model => model.UserType == UserType.Business)
    
.AddField(x => x.TaxId)
    .RequiredWhen(model => model.Country == "US")
```

### Field Groups

Organize related fields into groups with customizable layouts:

```csharp
var formConfig = FormBuilder<UserModel>
    .Create()
    .AddFieldGroup(group => group
        .WithGroupName("Personal Information")
        .WithColumns(2)  // Two-column layout
        .ShowInCard(2)   // Show in card with elevation 2
        .AddField(x => x.FirstName, field => field
            .WithLabel("First Name")
            .Required())
        .AddField(x => x.LastName, field => field
            .WithLabel("Last Name")
            .Required())
        .AddField(x => x.DateOfBirth))
    .AddFieldGroup(group => group
        .WithGroupName("Contact Information")
        .WithColumns(3)  // Three-column layout
        .ShowInCard()    // Default elevation 1
        .AddField(x => x.Email)
        .AddField(x => x.Phone)
        .AddField(x => x.Address))
    .Build();
```

### Custom Field Renderers

Create your own field types:

```csharp
public class ColorPickerRenderer : IFieldRenderer
{
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => fieldType == typeof(string) && field.AdditionalAttributes.ContainsKey("color-picker");

    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        // Your custom rendering logic
    }
}

// Register your renderer
services.AddDynamicForms(options =>
{
    options.AddRenderer<ColorPickerRenderer>();
});
```

## 📊 Performance

FormCraft is designed for optimal performance:

- ⚡ Minimal re-renders using field-level change detection
- 🎯 Targeted validation execution
- 🔄 Efficient dependency tracking
- 📦 Small bundle size (~50KB gzipped)

## 🧪 Testing

FormCraft is extensively tested with over 400 unit tests covering:

- ✅ All field types and renderers
- ✅ Validation scenarios
- ✅ Field dependencies
- ✅ Edge cases and error handling
- ✅ Integration scenarios

## 🤝 Contributing

We love contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Quick Start for Contributors

```bash
# Clone the repository
git clone https://github.com/phmatray/DynamicFormBlazor.git

# Build the project
dotnet build

# Run tests
dotnet test

# Create a local NuGet package
./pack-local.sh  # or pack-local.ps1 on Windows
```

## 📖 Documentation

- [Getting Started Guide](docs/getting-started.md)
- [API Reference](docs/api-reference.md)
- [Examples](docs/examples.md)
- [Custom Validators](docs/custom-validators.md)
- [Field Renderers](docs/field-renderers.md)
- [Best Practices](docs/best-practices.md)

## 🗺️ Roadmap

- [ ] File upload field type
- [ ] Rich text editor field
- [ ] Drag-and-drop form builder UI
- [ ] Form templates library
- [ ] Localization support
- [ ] More layout options
- [ ] Integration with popular CSS frameworks
- [ ] Form state persistence
- [ ] Wizard/stepper forms

## 💬 Community

- **Discussions**: [GitHub Discussions](https://github.com/phmatray/DynamicFormBlazor/discussions)
- **Issues**: [GitHub Issues](https://github.com/phmatray/DynamicFormBlazor/issues)
- **Twitter**: [@phmatray](https://twitter.com/phmatray)

## 📄 License

FormCraft is licensed under the [MIT License](LICENSE).

## 🙏 Acknowledgments

- [MudBlazor](https://mudblazor.com/) for the amazing component library
- [FluentValidation](https://fluentvalidation.net/) for validation inspiration
- The Blazor community for feedback and support

---

<div align="center">

**If you find FormCraft useful, please consider giving it a ⭐ on GitHub!**

Made with ❤️ by [phmatray](https://github.com/phmatray)

</div>