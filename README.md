![FormCraft banner](.github/banner.png)

# FormCraft

**Forms in a few lines of C#.** Describe each field once with a typed `FormBuilder<T>`; FormCraft renders
the controls, runs your validation on the server and announces required fields to screen readers.

<div align="center">

[![NuGet Version](https://img.shields.io/nuget/v/FormCraft.svg?style=flat-square)](https://www.nuget.org/packages/FormCraft/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FormCraft.svg?style=flat-square)](https://www.nuget.org/packages/FormCraft/)
[![FormCraft.ForMudBlazor](https://img.shields.io/nuget/v/FormCraft.ForMudBlazor.svg?style=flat-square&label=FormCraft.ForMudBlazor)](https://www.nuget.org/packages/FormCraft.ForMudBlazor/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/phmatray/FormCraft/continuous.yml?branch=dev&style=flat-square)](https://github.com/phmatray/FormCraft/actions)
[![License: MIT](https://img.shields.io/github/license/phmatray/FormCraft?style=flat-square)](LICENSE)

[Live demo](https://phmatray.github.io/FormCraft/) · [Documentation](https://phmatray.github.io/FormCraft/docs/getting-started) · [API reference](https://phmatray.github.io/FormCraft/docs/api-reference) · [Changelog](CHANGELOG.md)

</div>

<!-- portfolio-toc:start -->

## Table of Contents

- [At a glance](#at-a-glance)
- [Why FormCraft](#why-formcraft)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Attribute-based forms](#attribute-based-forms)
- [Examples](#examples)
- [Advanced features](#advanced-features)
- [Accessibility](#accessibility)
- [Documentation](#documentation)
- [Tech Stack](#tech-stack)
- [Contributing](#contributing)
- [License](#license)

<!-- portfolio-toc:end -->

## At a glance

This is the whole of a contact form: a required name, a validated email, a topic and a checkbox.

```razor
<FormCraftComponent
    TModel="Contact"
    Model="@_contact"
    Configuration="@_config"
    OnValidSubmit="@Send" />

@code {
    private readonly Contact _contact = new();

    private static readonly SelectOption<string>[] Topics =
        [new("general", "General question"), new("bug", "Bug report")];

    private readonly IFormConfiguration<Contact> _config =
        FormBuilder<Contact>.Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Name")
                .Required())
            .AddField(x => x.Email, f => f
                .WithLabel("Email")
                .Required()
                .WithEmailValidation())
            .AddField(x => x.Topic, f => f
                .WithLabel("Topic")
                .WithSelectOptions(Topics))
            .AddField(x => x.Consent, f => f
                .WithLabel("Reply by email"))
            .Build();

    private void Send(Contact contact) => Console.WriteLine(contact);
}
```

The same form written by hand as a MudBlazor `EditForm` runs to roughly three times the lines. The
[live demo](https://phmatray.github.io/FormCraft/) plays this one out, and shows both listings side by side.

## Why FormCraft

- **Typed end to end.** Fields are bound with expressions (`x => x.Email`), so a renamed property is a
  compile error, not a blank field.
- **Validation where it belongs.** Built-in rules, custom and async validators, and FluentValidation all run
  against your model. Forms render `novalidate`, so the browser never shows its own bubbles.
- **Accessible by default.** Required fields carry `aria-required` on every field type, and controls that
  remove themselves move keyboard focus somewhere sensible. See [Accessibility](#accessibility).
- **Fields that react.** `DependsOn()` recalculates, resets or reloads a field when another one changes.
- **More than text boxes.** Groups and columns, steppers and tabs, file uploads, lookups and LOV fields,
  master-detail collections, custom renderers, and field encryption with CSRF and rate limiting.
- **Your UI library.** Adapters for [MudBlazor](https://mudblazor.com/) and
  [Fluent UI Blazor](https://www.fluentui-blazor.net/) render the same configuration.

Around 1,600 unit tests cover the core and both adapters. For a feature-by-feature comparison with
`EditForm`, Blazored.FluentValidation and MudBlazor's own forms, see [COMPARISON.md](COMPARISON.md).

## Installation

Install **one** UI adapter; each one brings the core package with it.

```bash
dotnet add package FormCraft.ForMudBlazor
```

| Package | What it is |
|---|---|
| [`FormCraft`](https://www.nuget.org/packages/FormCraft/) | The UI-agnostic core: builders, configuration, validation, security |
| [`FormCraft.ForMudBlazor`](https://www.nuget.org/packages/FormCraft.ForMudBlazor/) | Renders forms with MudBlazor (≥ 9.9.0) |
| `FormCraft.ForFluentUI` | Renders forms with Fluent UI Blazor v5. New, and not yet published to NuGet |

Adapters are mutually exclusive: `AddFormCraftMudBlazor()` and `AddFormCraftFluentUI()` cannot be
registered in the same container, and the second call throws.

**Target frameworks:** `net8.0` and `net10.0`.

## Quick start

### 1. Register the services

```csharp
// Program.cs
builder.Services.AddMudServices();          // MudBlazor services
builder.Services.AddFormCraft();            // FormCraft core services
builder.Services.AddFormCraftMudBlazor();   // MudBlazor renderers for FormCraft
```

### 2. Create your model

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

### 3. Build the form and render it

```razor
@page "/register"
@using FormCraft
@using FormCraft.ForMudBlazor

<h3>User Registration</h3>

<FormCraftComponent
    TModel="UserRegistration"
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

## Attribute-based forms

Put the form on the model itself, then generate it in one call.

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

```csharp
var formConfig = FormBuilder<UserRegistration>.Create()
    .AddFieldsFromAttributes()
    .Build();
```

The attributes are `[TextField]`, `[EmailField]`, `[NumberField]`, `[DateField]`, `[SelectField]`,
`[CheckboxField]` and `[TextArea]`. They combine with the standard DataAnnotations validators
(`[Required]`, `[MinLength]`, `[MaxLength]`, `[Range]` and the rest).

With no attributes at all, `AddFieldsAuto()` builds a form from any POCO by reflection, with humanized
labels and a sensible field type per property type.

### Fluent API or attributes

The same two fields, both ways:

<table>
<tr>
<th>Fluent API</th>
<th>Attribute-based</th>
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

var config = FormBuilder<User>.Create()
    .AddFieldsFromAttributes()
    .Build();
```

</td>
</tr>
</table>

## Examples

Every example below has a working counterpart in the [live demo](https://phmatray.github.io/FormCraft/),
with its source beside the form.

### Dynamic field dependencies

`DependsOn(watchedField, callback)` runs the callback whenever the watched field changes, so you can reset
or recalculate dependent values:

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

The callback can also be async: `DependsOn(x => x.Country, async (model, country) => ...)` re-renders the
form once the work settles, which is how cascading country → state → city lookups are built.

### Custom validation

```csharp
.AddField(x => x.Username, field => field
    .WithValidator(
        username => !forbiddenUsernames.Contains(username.ToLower()),
        "This username is not available")
    .WithAsyncValidator(
        async username => await UserService.IsUsernameAvailableAsync(username),
        "Username is already taken"))
```

When a validator needs other model values or DI services, implement `IFieldValidator<TModel, TValue>`. Its
`ValidateAsync(model, value, services)` receives the full model and the `IServiceProvider`:

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

FluentValidation plugs in too: register your `IValidator<TModel>` in DI and call
`.WithFluentValidation(x => x.Email)` on the field.
The default validation messages are localisable. English and French ship, and adding
`FormCraft/Resources/ValidationMessages.<culture>.resx` adds a language.

### Layouts

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

### More field types

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

`DateOnly`, `TimeOnly` and nullable value types (`int?`, `decimal?`, `DateTime?` …) are supported too:
a cleared field writes `null` back instead of `0` or `MinValue`.

## Advanced features

### Conditional fields

```csharp
.AddField(x => x.CompanyName, field => field
    .WithLabel("Company Name")
    .VisibleWhen(model => model.UserType == UserType.Business))

.AddField(x => x.TaxId, field => field
    .WithLabel("Tax ID")
    .VisibleWhen(model => model.Country == "US")
    .DisabledWhen(model => model.IsLocked))
```

For conditional *requiredness*, use a model-aware validator that only fails when the condition applies:

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

### Field groups

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

### Input appearance (MudBlazor)

`Variant` and `ShrinkLabel` are configurable per field, with a form-level default. The field-level value
wins; unconfigured fields render `Variant.Outlined` with `ShrinkLabel="true"`.

```razor
<FormCraftComponent
    TModel="UserModel"
    Model="@model"
    Configuration="@formConfig"
    DefaultVariant="Variant.Text"
    DefaultShrinkLabel="false" />
```

```csharp
// Per-field override: a Filled search box among otherwise Text inputs
var formConfig = FormBuilder<UserModel>
    .Create()
    .AddField(x => x.Query, field => field
        .WithLabel("Search")
        .WithVariant(Variant.Filled)
        .WithShrinkLabel(true))
    .Build();
```

> [!IMPORTANT]
> `ShrinkLabel="false"` only shows on an empty field with no placeholder and no start adornment:
> MudBlazor pins the label whenever a field has a value, a placeholder or a start adornment. FormCraft
> logs one warning (`FormCraft.ForMudBlazor.ShrinkLabel`) naming every field where the setting cannot
> take effect. Rendering is unaffected.

### Security

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

`FormCraftComponent` enforces these settings itself. It issues a CSRF token and validates it before
`OnValidSubmit` fires. It checks the rate limit before validation, keyed by the `SecurityContextId`
parameter (set it to a user, session or IP). It writes `FormSubmitted` / `FormRejected` audit entries with
encrypted and excluded fields redacted. A blocked submission shows an alert and never reaches your handler.

Encryption stays an application concern. Call `encryptionService.EncryptConfiguredFields(model, config.Security)`
before persisting. The default `IEncryptionService` is AES-256 with a random IV per operation; configure a
32-byte key for values that must survive a restart. On WebAssembly a browser-compatible fallback is registered
instead, and it is obfuscation, not encryption. The
[security guide](https://phmatray.github.io/FormCraft/docs/security) has the details.

### Custom field renderers

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

// Use in your form configuration (TRenderer is checked against the field's value type)
.AddField(x => x.Color, field => field
    .WithLabel("Product Color")
    .WithCustomRenderer<ColorPickerRenderer>()
    .WithHelpText("Select the primary color"))

// Register it in DI when it takes constructor dependencies
services.AddScoped<ColorPickerRenderer>();
```

## Accessibility

- **Required fields are announced.** `.Required()` adds `aria-required="true"` on every field type that can
  carry it (text, numeric, date, select, multi-select, autocomplete, lookup, LOV, boolean). It does *not*
  add the HTML5 `required` attribute, because validation runs on the server. Use `.WithNativeRequired()`
  when you want MudBlazor's native asterisk and attribute.
- **Uploads say so too.** A required file upload is marked by a visible asterisk and by a description on
  its **Browse** button, the control a keyboard user actually reaches.
- **Focus goes somewhere sensible.** Controls that remove or disable themselves (clearing an upload,
  deleting or moving a collection row, adding one) move focus deliberately instead of dropping it to the
  top of the page.

The MudBlazor adapter needs MudBlazor 9.9.0 or later for the `aria-required` behaviour.

## Documentation

- [Getting started](https://phmatray.github.io/FormCraft/docs/getting-started)
- [API reference](https://phmatray.github.io/FormCraft/docs/api-reference)
- [Examples](https://phmatray.github.io/FormCraft/docs/examples)
- [Customization](https://phmatray.github.io/FormCraft/docs/customization)
- [FluentValidation](https://phmatray.github.io/FormCraft/docs/fluent-validation)
- [Security](https://phmatray.github.io/FormCraft/docs/security)
- [Troubleshooting](https://phmatray.github.io/FormCraft/docs/troubleshooting)

What changed in each release is in the [changelog](CHANGELOG.md) and on the
[releases page](https://github.com/phmatray/FormCraft/releases).

<!-- portfolio-techstack:start -->

## Tech Stack

- **.NET 10 · .NET 8**
- Microsoft.AspNetCore.Components.WebAssembly
- Microsoft.AspNetCore.Components.WebAssembly.DevServer
- MudBlazor
- Microsoft.FluentUI.AspNetCore.Components
- FluentValidation
- Markdig
- bunit
- FakeItEasy
- Shouldly

<!-- portfolio-techstack:end -->

## Contributing

Contributions are welcome. [CONTRIBUTING.md](CONTRIBUTING.md) has the details.

```bash
git clone https://github.com/phmatray/FormCraft.git
cd FormCraft
dotnet build -c Release
dotnet test -c Release
./pack-local.sh   # local NuGet packages in ./nupkg (pack-local.ps1 on Windows)
```

Pull requests target `dev` and are squash-merged, so **the PR title becomes the commit** and must be a
[Conventional Commit](https://www.conventionalcommits.org/): `<type>(<scope>): <subject> (#<issue>)`.
A CI check enforces it, because that title drives the next version and its changelog entry.

### Releasing

Nothing is versioned or tagged by hand. [release-please](https://github.com/googleapis/release-please) keeps
a release PR open against `dev` with the next version and changelog. Merging it tags `vX.Y.Z`, creates the
GitHub Release and publishes the packages to NuGet.org through Trusted Publishing, all in the same workflow
run. `CHANGELOG.md` is generated, so never edit it by hand. MinVer derives the package version from the tag.

## License

FormCraft is licensed under the [MIT License](LICENSE): free to use, modify and ship, including commercially.
Issues and ideas go to [GitHub Issues](https://github.com/phmatray/FormCraft/issues).
