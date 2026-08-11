![FormCraft banner](.github/banner.png)

# FormCraft 🎨

<!-- portfolio-toc:start -->

## Table of Contents

- [🌐 Live Demo](#-live-demo)
- [🎉 What's New in v3.1.0](#-whats-new-in-v310)
- [🎉 What's New in v3.0.0](#-whats-new-in-v300)
- [🚀 Why FormCraft?](#-why-formcraft)
- [📊 How FormCraft Compares](#-how-formcraft-compares)
- [📦 Installation](#-installation)
- [🎯 Quick Start](#-quick-start)
- [🏷️ Attribute-Based Forms (NEW!)](#-attribute-based-forms-new)
- [🎨 Examples](#-examples)
- [🛠️ Advanced Features](#-advanced-features)
- [📊 Performance](#-performance)
- [🧪 Testing](#-testing)
- [Tech Stack](#tech-stack)
- [🤝 Contributing](#-contributing)
- [📖 Documentation](#-documentation)
- [🗺️ Roadmap](#-roadmap)
- [💬 Community](#-community)
- [📄 License](#-license)
- [🙏 Acknowledgments](#-acknowledgments)

<!-- portfolio-toc:end -->


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

## 🎉 Unreleased

- **Configurable MudBlazor ShrinkLabel** — `.WithShrinkLabel(false)` per field and a `DefaultShrinkLabel` parameter on `FormCraftComponent`, completing the `Variant` work from v3.1.0: `Variant.Text` can now let its label float instead of pinning it above a borderless input. Field-level wins over form-level; the default stays `true`, so **no existing form changes appearance** (#177)
  - Caveat, inherited from MudBlazor: `ShrinkLabel="false"` is only visible on an **empty field with no placeholder and no start adornment**. MudBlazor ORs `ShrinkLabel` with those conditions, so a field with a placeholder keeps its label pinned whatever you pass. To get a floating label on a `Variant.Text` field, leave its placeholder unset.
- **⚠️ Breaking — `LovBuilder.WithDisplay(Expression<Func<TItem, string>>)` removed.** It was redundant: it only called `.Compile()` and assigned the same `Func<TItem, string>` the remaining overload takes directly. Having both made every bare lambda ambiguous (`CS0121`) — so `.WithDisplay(c => c.Name)`, the form shown throughout our own documentation, did not compile. It does now (#180)

  ```csharp
  .WithDisplay(c => c.Name)                   // simple property
  .WithDisplay(c => $"{c.Code} - {c.Name}")   // complex formatting
  ```

  **Migration:** delete any `(Expression<Func<T, string>>)` or `(Func<T, string>)` cast you added to work around the ambiguity and pass the lambda directly. If you genuinely hold an `Expression<Func<T, string>>`, pass `expr.Compile()`.

## 🎉 What's New in v3.1.0

v3.1.0 implements every issue that was open after v3.0 — all features, no breaking changes. [Full changelog →](https://github.com/phmatray/FormCraft/releases/tag/v3.1.0)

- **Zero-config forms** — `AddFieldsAuto()` generates a complete form from any POCO by reflection: humanized labels, sensible field types per property type, DataAnnotations honored when present, none required (#124)
- **Security enforcement** — `WithSecurity()` is now enforced automatically by `FormCraftComponent`: rate limiting (with `SecurityContextId` parameter), CSRF validation, and `FormSubmitted`/`FormRejected` audit entries with redaction; plus `EncryptConfiguredFields()` for one-call persistence encryption (#147)
- **Configurable MudBlazor Variant** — `.WithVariant(Variant.Filled)` per field and a `DefaultVariant` parameter on `FormCraftComponent`, honored by every input component (#146)
- **Async field dependencies** — `DependsOn(x => x.Country, async (model, country) => ...)` is a first-class overload; cascades re-render automatically when the async work settles (#93)
- **Nullable value types round-trip** — `int?`/`decimal?`/`DateTime?`/`DateOnly?`/`TimeOnly?` fields display empty when null and write `null` back when cleared, instead of being coerced to 0/MinValue (#150)
- **Native nested validation for collections** — collection item edits raise `Items[0].ProductName` field identifiers on the EditContext, so `ValidationSummary`/`IsModified` work for child rows (#91)
- **Single render pipeline** — the legacy type-switch is gone; every field flows through `FieldRendererService`, and `AsMultiSelect` fields (previously skipped silently) now render a real multi-selection select (#148)
- **Master-detail & auto-form demos** — new `/master-detail` (invoice + LOV customer + line items + computed totals) and `/auto-form` pages (#130)
- **Polish** — single-file uploads no longer emit a stray `multiple` attribute (#149), Related Demos show real titles (#152), `WithAutocomplete()` + correct password autocomplete tokens (#153), validator mutations through the object-typed wrapper now take effect via `AddValidator` (#151)

## 🎉 What's New in v3.0.0

v3.0.0 is a major quality release: after a full audit of every subsystem, 60+ bugs were fixed, several long-broken features now actually work, and the whole demo site was verified end-to-end in a real browser. [Full changelog →](https://github.com/phmatray/FormCraft/releases/tag/v3.0.0)

### ✅ Now working as documented
- **Field dependencies** — `DependsOn(x => x.Country, ...)` callbacks now fire when the *watched* field changes, dependent fields refresh in the UI, and async callbacks drive cascading loads (country → state → city)
- **Async validation blocks submission** — `OnValidSubmit` waits for async validators; errors clear as soon as the user corrects a value; hidden fields are no longer validated
- **Custom rendering** — `WithCustomTemplate()` renders, `WithCustomRenderer(instance)` is honored, and LOV/lookup/autocomplete/select renderers are no longer shadowed by the generic text/numeric ones
- **More field types** — `DateOnly`, `TimeOnly`, `float`, `long`, `short`, and `byte` fields render correctly
- **Form templates** — `FormTemplates.ContactForm/RegistrationForm/LoginForm/AddressForm<T>()` generate real convention-based forms
- **New API** — `FormCraftComponent.ValidateAsync()` for explicit validation (e.g. in dialogs)

### 🔒 Security hardening
- Default `IEncryptionService` is now **AES-256** (`DefaultEncryptionService`) with a random IV per operation; decryption failures throw `FormCraftDecryptionException` instead of returning ciphertext
- Thread-safe rate limiting, CSRF tokens that survive prerendering, and audit logs that honor `ExcludedFields` redaction

### ⚠️ Breaking changes (migration notes)
| Change | What to do |
|---|---|
| `FieldDependencies` is keyed by the **watched** field's name | Only affects code inspecting the configuration dictionary directly |
| `DependsOn` callbacks now fire on watched-field changes | Remove any workarounds written for the old inverted behavior |
| `FormBuilder` throws if mutated after `Build()` | Create a new builder instead of reusing one |
| `WithFluentValidation` fails when no `IValidator<TModel>` is registered | Register your validator in DI (it silently passed before) |
| Validator exceptions surface as "Validation could not be completed: …" | Don't rely on crashes producing the configured error message |
| `AsFileUpload`/`AsMultipleFileUpload` no longer force a renderer | No action — the proper upload components are picked by field type |
| Default encryption switched from XOR to AES-256 | Configure a 32-byte key (Base64 or UTF-8); without one an ephemeral per-process key is used |
| `FieldGroupBuilder.WithColumns` validates its range (1–6) | Fix any out-of-range values (0 used to crash rendering) |
| `FormCraft.ForMudBlazor` now versions with the core package | Reference `3.0.0` for both packages |

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
- 🧪 **Fully Tested** - 880+ unit tests ensuring reliability

## 📊 How FormCraft Compares

FormCraft stands out among Blazor form solutions with its **type-safe fluent API**, **automatic field rendering**, and **built-in field dependency management**. See how it compares to Blazor EditForm, Blazored.FluentValidation, and MudBlazor Forms:

| Capability | EditForm | MudBlazor Forms | FormCraft |
|------------|:--------:|:---------------:|:---------:|
| Fluent API configuration | - | - | Yes |
| Automatic field rendering | - | - | Yes |
| Built-in field dependencies | Manual | Manual | Yes |
| Conditional visibility | Manual | Manual | Built-in |
| Field-level encryption | - | - | Yes |
| Attribute-based generation | - | - | Yes |

> **[View the full comparison](COMPARISON.md)** — includes detailed feature matrix, code examples, and guidance on when to use each solution.

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

**Supported frameworks:** .NET 8, .NET 9, and .NET 10.

## 🎯 Quick Start

### 1. Register Services

```csharp
// Program.cs
builder.Services.AddMudServices();          // MudBlazor services
builder.Services.AddFormCraft();            // FormCraft core services
builder.Services.AddFormCraftMudBlazor();   // MudBlazor renderers for FormCraft
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
                .Required("You must accept the terms"))
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

Create forms where fields react to each other. `DependsOn(watchedField, callback)` runs the
callback whenever the watched field changes, letting you reset or recalculate dependent values:

```csharp
var formConfig = FormBuilder<OrderForm>.Create()
    .AddDropdownField(x => x.ProductType, "Product Type",
        ("standard", "Standard"),
        ("premium", "Premium"))
    .AddField(x => x.ProductModel, field => field
        .WithLabel("Model")
        .WithOptions(
            ("basic", "Basic Model"),
            ("pro", "Pro Model"))
        // Reset the model whenever Product Type changes
        .DependsOn(x => x.ProductType, (model, productType) =>
            model.ProductModel = string.Empty))
    .AddNumericField(x => x.Quantity, "Quantity", min: 1)
    .AddField(x => x.TotalPrice, field => field
        .WithLabel("Total Price")
        .ReadOnly()
        // Recalculate the total whenever Quantity changes
        .DependsOn(x => x.Quantity, (model, quantity) =>
            model.TotalPrice = quantity * GetUnitPrice(model.ProductModel)))
    .Build();
```

### Custom Validation

Add complex validation logic with ease:

```csharp
.AddField(x => x.Username, field => field
    .WithValidator(
        username => !forbiddenUsernames.Contains(username.ToLower()),
        "This username is not available")
    .WithAsyncValidator(
        async username => await UserService.IsUsernameAvailableAsync(username),
        "Username is already taken"))
```

If a validator needs access to other model values or DI services, implement
`IFieldValidator<TModel, TValue>` — its `ValidateAsync(model, value, services)`
method receives the full model and the `IServiceProvider`:

```csharp
public class UniqueUsernameValidator : IFieldValidator<User, string>
{
    public string? ErrorMessage { get; set; } = "Username is already taken";

    public async Task<ValidationResult> ValidateAsync(
        User model, string value, IServiceProvider services)
    {
        var userService = services.GetRequiredService<IUserService>();
        return await userService.IsUsernameAvailableAsync(value)
            ? ValidationResult.Success()
            : ValidationResult.Failure("Username is already taken");
    }
}

// Usage
.AddField(x => x.Username, field => field
    .WithValidator(new UniqueUsernameValidator()))
```

### Multiple Layouts

Choose the layout that fits your design:

```csharp
// Vertical Layout (default)
.WithLayout(FormLayout.Vertical)

// Horizontal Layout
.WithLayout(FormLayout.Horizontal)

// Grid Layout
.WithLayout(FormLayout.Grid)

// Inline Layout
.WithLayout(FormLayout.Inline)
```

Column counts are configured per field group rather than at the form level:

```csharp
.AddFieldGroup(group => group
    .WithGroupName("Address")
    .WithColumns(2)  // Two-column layout for this group
    .AddField(x => x.City)
    .AddField(x => x.PostalCode))
```

### Advanced Field Types

```csharp
// Password field with strength requirements
.AddPasswordField(x => x.Password, "Password", minLength: 8, requireSpecialChars: true)

// Password confirmation via a model-aware validator
.AddField(x => x.ConfirmPassword, field => field
    .WithLabel("Confirm Password")
    .WithInputType("password")
    .Required("Please confirm your password")
    .WithValidator(new PasswordsMatchValidator()))

// Date picker with validation (DateTime properties render as date pickers automatically)
.AddField(x => x.BirthDate, field => field
    .WithLabel("Date of Birth")
    .WithValidator(date => date <= DateTime.Today.AddYears(-18), "Must be 18 or older")
    .WithHelpText("Must be 18 or older"))

// Multi-line text with character limit
.AddField(x => x.Description, field => field
    .WithLabel("Description")
    .AsTextArea(lines: 5, maxLength: 500)
    .WithMaxLength(500, "Maximum 500 characters")
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

The password confirmation validator compares against the rest of the model:

```csharp
public class PasswordsMatchValidator : IFieldValidator<RegistrationModel, string>
{
    public string? ErrorMessage { get; set; } = "Passwords do not match";

    public Task<ValidationResult> ValidateAsync(
        RegistrationModel model, string value, IServiceProvider services)
        => Task.FromResult(value == model.Password
            ? ValidationResult.Success()
            : ValidationResult.Failure("Passwords do not match"));
}
```

## 🛠️ Advanced Features

### Conditional Fields

Show/hide or disable fields based on conditions:

```csharp
.AddField(x => x.CompanyName, field => field
    .WithLabel("Company Name")
    .VisibleWhen(model => model.UserType == UserType.Business))

.AddField(x => x.TaxId, field => field
    .WithLabel("Tax ID")
    .VisibleWhen(model => model.Country == "US")
    .DisabledWhen(model => model.IsLocked))
```

For conditional *requiredness*, use a model-aware validator that only fails
when the condition applies:

```csharp
.AddField(x => x.TaxId, field => field
    .WithLabel("Tax ID")
    .WithValidator(new RequiredWhenUsValidator()))

public class RequiredWhenUsValidator : IFieldValidator<BusinessModel, string>
{
    public string? ErrorMessage { get; set; } = "Tax ID is required for US companies";

    public Task<ValidationResult> ValidateAsync(
        BusinessModel model, string value, IServiceProvider services)
        => Task.FromResult(model.Country == "US" && string.IsNullOrWhiteSpace(value)
            ? ValidationResult.Failure("Tax ID is required for US companies")
            : ValidationResult.Success());
}
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

### Input Appearance — Variant and ShrinkLabel

Both MudBlazor presentation properties are configurable per field, with a form-level default. Field-level always wins; unconfigured fields render `Variant.Outlined` with `ShrinkLabel="true"`.

```csharp
// Form-level defaults for every field
<FormCraftComponent TModel="UserModel"
                    Model="@model"
                    Configuration="@formConfig"
                    DefaultVariant="Variant.Text"
                    DefaultShrinkLabel="false" />
```

```csharp
// Per-field override — a Filled search box among otherwise Text inputs
var formConfig = FormBuilder<UserModel>
    .Create()
    .AddField(x => x.Query, field => field
        .WithLabel("Search")
        .WithVariant(Variant.Filled)
        .WithShrinkLabel(true))
    .Build();
```

> [!TIP]
> `Variant.Text` usually wants `ShrinkLabel="false"`. That variant draws no border, so there is nothing for a permanently shrunk label to sit in — letting it float up from inside the input on focus is what makes `Text` look right. The two properties stay independent so you can still pin a label on a `Text` input if that is what you want.

> [!IMPORTANT]
> **`ShrinkLabel="false"` only shows up on an empty field with no placeholder and no start adornment.** MudBlazor decides the shrunk state by OR-ing `ShrinkLabel` with "has a value", "has a placeholder" and "has a start adornment" — so on a field configured with `.WithPlaceholder(...)` or `.WithAdornment(...)`, the label stays pinned no matter what you pass here. This is MudBlazor's rule, not FormCraft's. If you want the label to float on a `Variant.Text` field, leave its placeholder unset and let the label do that job.

`Variant` is honored by text, numeric, date/time pickers, select, multi-select, autocomplete, lookup, LOV, color picker and collection-item fields. `ShrinkLabel` reaches all of the same components, subject to the rule above — note that LOV fields always supply their own `"Click to select..."` placeholder, so their label is always pinned.

### Security Features (v2.0.0+)

Configure security settings for your forms:

```csharp
var formConfig = FormBuilder<SecureForm>.Create()
    .AddField(x => x.SSN, field => field
        .WithLabel("Social Security Number")
        .WithPlaceholder("XXX-XX-XXXX"))
    .AddField(x => x.CreditCard, field => field
        .WithLabel("Credit Card")
        .WithPlaceholder("XXXX XXXX XXXX XXXX"))
    .WithSecurity(security => security
        .EncryptField(x => x.SSN)           // Mark sensitive fields for encryption
        .EncryptField(x => x.CreditCard)
        .EnableCsrfProtection()             // Configure anti-forgery tokens
        .WithRateLimit(5, TimeSpan.FromMinutes(1))  // Max 5 submissions per minute
        .EnableAuditLogging())              // Configure audit logging
    .Build();
```

> **How enforcement works (v3.1+)**: `WithSecurity()` stores the security settings on the
> form configuration, `AddFormCraft()` registers the supporting services
> (`IEncryptionService`, `ICsrfTokenService`, `IRateLimitService`, `IAuditLogService`),
> and `FormCraftComponent` enforces them automatically: a CSRF token is generated on
> initialization and validated before `OnValidSubmit` fires, rate limits are checked
> (and attempts recorded) before validation, and audit entries (`FormSubmitted` /
> `FormRejected`) are written with excluded and encrypted fields redacted. Blocked
> submissions show an error alert above the submit button and never reach your handler.
> Set the `SecurityContextId` parameter to a per-user value (user id, session id, IP)
> so rate limits aren't shared across users; it defaults to the model type name.
> Encryption remains an application concern: call
> `encryptionService.EncryptConfiguredFields(model, config.Security)` (or the
> component's `GetEncryptedFieldValues()`) to obtain the encrypted values of the
> marked fields in one call before persisting.
> Since v3.0.0 the default registration is AES-256 (`DefaultEncryptionService`) with a
> random IV per operation — configure a 32-byte key for values that must survive a
> process restart (an ephemeral per-process key is generated otherwise). On WebAssembly
> a browser-compatible fallback (`BlazorEncryptionService`, XOR-based obfuscation) is
> registered instead — treat it as obfuscation, not encryption. See the
> [security documentation](https://phmatray.github.io/FormCraft/docs/security) for details.

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

// Use in your form configuration (type arguments: model, value, renderer)
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

FormCraft is extensively tested with over 880 unit tests covering:

- ✅ All field types and renderers
- ✅ Validation scenarios
- ✅ Field dependencies
- ✅ Edge cases and error handling
- ✅ Integration scenarios

<!-- portfolio-techstack:start -->

## Tech Stack

- **.NET 10 · .NET 9 · .NET 8**
- Microsoft.AspNetCore.Components.WebAssembly
- Microsoft.AspNetCore.Components.WebAssembly.DevServer
- MudBlazor
- FluentValidation
- Markdig
- bunit
- FakeItEasy
- Shouldly

<!-- portfolio-techstack:end -->

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
- [x] Wizard/stepper forms
- [x] Form templates library (`FormTemplates`)
- [x] DateOnly/TimeOnly field support
- [x] List-of-Values (LOV) modal selection fields
- [x] Automatic CSRF/rate-limit enforcement in `FormCraftComponent` (v3.1)
- [x] Zero-config form generation — `AddFieldsAuto()` (v3.1)
- [x] Async field dependencies and nullable value-type round-trip (v3.1)

### 🚧 In Progress
- [ ] Import/Export forms as JSON
- [ ] Rich text editor field

### 📋 Planned
- [ ] Drag-and-drop form builder UI
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