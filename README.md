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

## 🎉 What's New in v2.5.0

FormCraft v2.5.0 introduces powerful attribute-based form generation and more:

### 🏷️ Attribute-Based Form Generation (NEW!)
- **Zero-configuration forms** - Generate complete forms from model attributes
- **Rich attribute library** - TextField, EmailField, NumberField, DateField, SelectField, CheckboxField, TextArea
- **Automatic validation** - Integrates with DataAnnotations attributes
- **One-line setup** - Just call `.AddFieldsFromAttributes()`

### 🔒 Security Features (v2.0.0)
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
- **550+ unit tests** ensuring reliability

## 🚀 Why FormCraft?

FormCraft revolutionizes form building in Blazor applications by providing a **fluent, type-safe API** that makes complex forms simple. Say goodbye to repetitive form markup and hello to elegant, maintainable code.

### ✨ Key Features

- 🔒 **Type-Safe** - Full IntelliSense support with compile-time validation
- 🎯 **Fluent API** - Intuitive method chaining for readable form configuration
- 🏷️ **Attribute-Based Forms** - Generate forms from model attributes with zero configuration
- 🎨 **MudBlazor Integration** - Beautiful Material Design components out of the box
- 🔄 **Dynamic Forms** - Create forms that adapt based on user input
- ✅ **Advanced Validation** - Built-in, custom, and async validators
- 🔗 **Field Dependencies** - Link fields together with reactive updates
- 📐 **Flexible Layouts** - Multiple layout options to fit your design
- 🚀 **High Performance** - Optimized rendering with minimal overhead
- 🧪 **Fully Tested** - 550+ unit tests ensuring reliability

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
            .AddDropdownField(x => x.Country, "Country",
                ("us", "United States"),
                ("uk", "United Kingdom"),
                ("ca", "Canada"),
                ("au", "Australia"))
            .AddField(x => x.AcceptTerms, field => field
                .WithLabel("I accept the terms and conditions")
                .WithValidator(value => value, "You must accept the terms"))
            .Build();
    }

    private async Task HandleSubmit(UserRegistration model)
    {
        // Handle form submission
        await UserService.RegisterAsync(model);
    }
}
```

## 🏷️ Attribute-Based Forms (NEW!)

Define your forms directly on your model with attributes - no configuration code needed!

### Define Your Model with Attributes

```csharp
public class UserRegistration
{
    [TextField("First Name", "Enter your first name")]
    [Required(ErrorMessage = "First name is required")]
    [MinLength(2)]
    public string FirstName { get; set; } = string.Empty;
    
    [TextField("Last Name", "Enter your last name")]
    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;
    
    [EmailField("Email Address")]
    [Required]
    public string Email { get; set; } = string.Empty;
    
    [NumberField("Age", "Your age")]
    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int Age { get; set; }
    
    [DateField("Date of Birth")]
    public DateTime BirthDate { get; set; }
    
    [SelectField("Country", "United States", "Canada", "United Kingdom", "Australia")]
    public string Country { get; set; } = string.Empty;
    
    [TextArea("Bio", "Tell us about yourself")]
    [MaxLength(500)]
    public string Bio { get; set; } = string.Empty;
    
    [CheckboxField("Newsletter", "Subscribe to our newsletter")]
    public bool SubscribeToNewsletter { get; set; }
}
```

### Generate the Form with One Line

```csharp
var formConfig = FormBuilder<UserRegistration>.Create()
    .AddFieldsFromAttributes()  // That's it! 🎉
    .Build();
```

### Available Attribute Types

- `[TextField]` - Standard text input
- `[EmailField]` - Email input with validation
- `[NumberField]` - Numeric input with min/max support
- `[DateField]` - Date picker with constraints
- `[SelectField]` - Dropdown with predefined options
- `[CheckboxField]` - Boolean checkbox
- `[TextArea]` - Multiline text input

All attributes work seamlessly with standard DataAnnotations validators like `[Required]`, `[MinLength]`, `[MaxLength]`, `[Range]`, and more!

### Comparison: Fluent API vs Attributes

<table>
<tr>
<th>Fluent API</th>
<th>Attribute-Based</th>
</tr>
<tr>
<td>

```csharp
var config = FormBuilder<User>.Create()
    .AddField(x => x.Name, field => field
        .WithLabel("Full Name")
        .WithPlaceholder("Enter name")
        .Required("Name is required")
        .WithMinLength(2))
    .AddField(x => x.Email, field => field
        .WithLabel("Email")
        .WithInputType("email")
        .Required())
    .Build();
```

</td>
<td>

```csharp
public class User
{
    [TextField("Full Name", "Enter name")]
    [Required(ErrorMessage = "Name is required")]
    [MinLength(2)]
    public string Name { get; set; }
    
    [EmailField("Email")]
    [Required]
    public string Email { get; set; }
}

// One line to generate!
var config = FormBuilder<User>.Create()
    .AddFieldsFromAttributes()
    .Build();
```

</td>
</tr>
</table>

## 🎨 Examples

### Dynamic Field Dependencies

Create forms where fields react to each other using `DependsOn` and `VisibleWhen`:

```csharp
var formConfig = FormBuilder<OrderForm>.Create()
    .AddDropdownField(x => x.ProductType, "Product Type",
        ("electronics", "Electronics"),
        ("clothing", "Clothing"),
        ("furniture", "Furniture"))
    .AddField(x => x.ProductModel, field => field
        .WithLabel("Model")
        .DependsOn(x => x.ProductType, (model, productType) => {
            // Clear model when product type changes
            model.ProductModel = "";
        })
        .VisibleWhen(model => !string.IsNullOrEmpty(model.ProductType)))
    .AddNumericField(x => x.Quantity, "Quantity", min: 1)
    .AddField(x => x.TotalPrice, field => field
        .WithLabel("Total Price")
        .ReadOnly())  // Use computed property in model for calculated values
    .Build();
```

### Custom Validation

Add complex validation logic with ease:

```csharp
.AddField(x => x.Username, field => field
    .WithLabel("Username")
    .Required()
    // Sync validation with custom function
    .WithValidator(
        username => !forbiddenUsernames.Contains(username?.ToLower() ?? ""),
        "This username is not available")
    // Async validation (returns bool, error message is second parameter)
    .WithAsyncValidator(
        async username => await userService.IsUsernameAvailableAsync(username),
        "Username is already taken"))
```

### Multiple Layouts

Choose the layout that fits your design:

```csharp
// Vertical Layout (default)
.WithLayout(FormLayout.Vertical)

// Horizontal Layout
.WithLayout(FormLayout.Horizontal)

// Grid Layout (use field groups for columns)
.WithLayout(FormLayout.Grid)

// Inline Layout
.WithLayout(FormLayout.Inline)

// For multi-column layouts, use Field Groups:
.AddFieldGroup(group => group
    .WithColumns(2)  // Two-column grid
    .AddField(x => x.FirstName)
    .AddField(x => x.LastName))
```

### Advanced Field Types

```csharp
// Password field with validation
.AddPasswordField(x => x.Password, "Password", minLength: 8, requireSpecialChars: true)

// Password confirmation (use FluentValidation for cross-field validation)
.AddField(x => x.ConfirmPassword, field => field
    .WithLabel("Confirm Password")
    .WithInputType("password")
    .Required())

// Date field with label
.AddField(x => x.BirthDate, field => field
    .WithLabel("Date of Birth")
    .WithHelpText("Must be 18 or older"))

// Multi-line text with character limit
.AddTextArea(x => x.Description, "Description", rows: 5,
    fieldConfig: field => field
        .WithMaxLength(500)
        .WithHelpText("Maximum 500 characters"))

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

Show/hide or disable fields based on conditions:

```csharp
// Show field only when condition is met
.AddField(x => x.CompanyName, field => field
    .WithLabel("Company Name")
    .VisibleWhen(model => model.UserType == UserType.Business))

// Disable field based on condition
.AddField(x => x.TaxId, field => field
    .WithLabel("Tax ID")
    .DisabledWhen(model => model.Country != "US"))

// Combine visibility with required validation
.AddField(x => x.StateCode, field => field
    .WithLabel("State")
    .Required("State is required for US addresses")
    .VisibleWhen(model => model.Country == "US"))
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
        .WithRateLimit(5, TimeSpan.FromMinutes(1), "IP")  // Max 5 submissions per minute
        .EnableAuditLogging(config => {     // Log form interactions
            config.LogFieldChanges = true;
            config.LogSubmissions = true;
            config.ExcludedFields.Add("CreditCard");  // Don't log sensitive values
        }))
    .AddField(x => x.SSN, field => field
        .WithLabel("Social Security Number")
        .WithPlaceholder("000-00-0000")
        .Required())
    .AddField(x => x.CreditCard, field => field
        .WithLabel("Credit Card")
        .WithPlaceholder("0000 0000 0000 0000")
        .Required())
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
    .WithCustomRenderer<ColorPickerRenderer>()  // Renderer type only
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