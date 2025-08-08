# FormCraft 🎨

<div align="center">

[![NuGet Version](https://img.shields.io/nuget/v/FormCraft.svg?style=flat-square)](https://www.nuget.org/packages/FormCraft/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FormCraft.svg?style=flat-square)](https://www.nuget.org/packages/FormCraft/)
[![MudBlazor Version](https://img.shields.io/nuget/v/FormCraft.ForMudBlazor.svg?style=flat-square&label=FormCraft.ForMudBlazor)](https://www.nuget.org/packages/FormCraft.ForMudBlazor/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/phmatray/FormCraft/continuous.yml?branch=main&style=flat-square)](https://github.com/phmatray/FormCraft/actions)
[![License](https://img.shields.io/github/license/phmatray/FormCraft?style=flat-square)](https://github.com/phmatray/FormCraft/blob/main/LICENSE)
[![Stars](https://img.shields.io/github/stars/phmatray/FormCraft?style=flat-square)](https://github.com/phmatray/FormCraft/stargazers)

**Build type-safe, dynamic forms in Blazor with ease** ✨

[Get Started](#-quick-start) • [Live Demo](https://phmatray.github.io/FormCraft/) • [Documentation](https://phmatray.github.io/FormCraft/docs/getting-started) • [Examples](#-examples) • [Contributing](CONTRIBUTING.md)

</div>

---

## 🌐 Live Demo

Experience FormCraft in action! Visit our [interactive demo](https://phmatray.github.io/FormCraft/) to see:

- 🎯 Various form layouts and configurations
- 🔄 Dynamic field dependencies
- ✨ Custom field renderers
- 📤 File upload capabilities
- 🎨 Real-time form generation

## 🎉 What's New in v2.0.0

FormCraft v2.0.0 brings exciting new features and improvements:

### 🔒 Security Features
- **Field-level encryption** for sensitive data protection
- **CSRF protection** with built-in anti-forgery tokens
- **Rate limiting** to prevent form spam
- **Audit logging** to track all form interactions

### 📦 Modular Architecture
- **Separate UI framework packages** - Use only what you need
- **FormCraft.ForMudBlazor** - MudBlazor implementation package
- **Improved extensibility** - Easier to add custom UI frameworks

### 🚀 Other Improvements
- **Enhanced performance** with optimized rendering
- **Better type safety** with improved generic constraints
- **Comprehensive documentation** with live examples
- **500+ unit tests** ensuring reliability

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

### FormCraft Core
```bash
dotnet add package FormCraft
```

### FormCraft for MudBlazor
```bash
dotnet add package FormCraft.ForMudBlazor
```

> **Note**: FormCraft.ForMudBlazor includes FormCraft as a dependency, so you only need to install the MudBlazor package if you're using MudBlazor components.

## 🎯 Quick Start

### 1. Register Services

```csharp
// Program.cs
builder.Services.AddFormCraft();
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
@using FormCraft.ForMudBlazor

<h3>User Registration</h3>

<FormCraftComponent TModel="UserRegistration" 
                   Model="@model" 
                   Configuration="@formConfig"
                   OnValidSubmit="@HandleSubmit"
                   ShowSubmitButton="true" />

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

Alternatively, you can configure fields directly on your model using attributes:

```csharp
public class ContactModel
{
    [Required]
    [TextField("First Name", "Enter your first name")]
    [MinLength(2)]
    public string FirstName { get; set; } = string.Empty;
}

var config = FormBuilder<ContactModel>.Create()
    .AddFieldsFromAttributes()
    .Build();
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

// File upload
.AddFileUploadField(x => x.Resume, "Upload Resume",
    acceptedFileTypes: new[] { ".pdf", ".doc", ".docx" },
    maxFileSize: 5 * 1024 * 1024) // 5MB
    
// Multiple file upload
.AddMultipleFileUploadField(x => x.Documents, "Upload Documents",
    maxFiles: 3,
    acceptedFileTypes: new[] { ".pdf", ".jpg", ".png" },
    maxFileSize: 10 * 1024 * 1024) // 10MB per file
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

### Security Features (v2.0.0+)

Protect your forms with built-in security features:

```csharp
var formConfig = FormBuilder<SecureForm>.Create()
    .WithSecurity(security => security
        .EncryptField(x => x.SSN)           // Encrypt sensitive fields
        .EncryptField(x => x.CreditCard)
        .EnableCsrfProtection()             // Enable anti-forgery tokens
        .WithRateLimit(5, TimeSpan.FromMinutes(1))  // Max 5 submissions per minute
        .EnableAuditLogging())              // Log all form interactions
    .AddField(x => x.SSN, field => field
        .WithLabel("Social Security Number")
        .WithMask("000-00-0000"))
    .AddField(x => x.CreditCard, field => field
        .WithLabel("Credit Card")
        .WithMask("0000 0000 0000 0000"))
    .Build();
```

### Custom Field Renderers

Create specialized input controls for specific field types:

```csharp
// Create a custom renderer
public class ColorPickerRenderer : CustomFieldRendererBase<string>
{
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            var value = GetValue(context) ?? "#000000";
            
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", "color");
            builder.AddAttribute(2, "value", value);
            builder.AddAttribute(3, "onchange", EventCallback.Factory.CreateBinder<string>(
                this, async (newValue) => await SetValue(context, newValue), value));
            builder.CloseElement();
        };
    }
}

// Use in your form configuration
.AddField(x => x.Color, field => field
    .WithLabel("Product Color")
    .WithCustomRenderer<ProductModel, string, ColorPickerRenderer>()
    .WithHelpText("Select the primary color"))

// Register custom renderers (optional for DI)
services.AddScoped<ColorPickerRenderer>();
services.AddScoped<RatingRenderer>();
```

Built-in example renderers:
- **ColorPickerRenderer** - Visual color selection with hex input
- **RatingRenderer** - Star-based rating control using MudBlazor

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
git clone https://github.com/phmatray/FormCraft.git

# Build the project
dotnet build

# Run tests
dotnet test

# Create a local NuGet package
./pack-local.sh  # or pack-local.ps1 on Windows
```

## 📖 Documentation

📚 **[Complete Documentation](https://phmatray.github.io/FormCraft/docs/getting-started)** - Interactive docs with live examples

- [Getting Started Guide](https://phmatray.github.io/FormCraft/docs/getting-started)
- [API Reference](https://phmatray.github.io/FormCraft/docs/api-reference)
- [Examples](https://phmatray.github.io/FormCraft/docs/examples)
- [Customization](https://phmatray.github.io/FormCraft/docs/customization)
- [Troubleshooting](https://phmatray.github.io/FormCraft/docs/troubleshooting)

## 🗺️ Roadmap

### ✅ Completed
- [x] File upload field type
- [x] Security features (encryption, CSRF, rate limiting, audit logging)
- [x] Modular UI framework architecture

### 🚧 In Progress
- [ ] Import/Export forms as JSON
- [ ] Rich text editor field

### 📋 Planned
- [x] Wizard/stepper forms
- [ ] Drag-and-drop form builder UI
- [ ] Form templates library
- [ ] Localization support
- [ ] More layout options
- [ ] Integration with popular CSS frameworks
- [ ] Form state persistence

## 💬 Community

- **Discussions**: [GitHub Discussions](https://github.com/phmatray/FormCraft/discussions)
- **Issues**: [GitHub Issues](https://github.com/phmatray/FormCraft/issues)
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