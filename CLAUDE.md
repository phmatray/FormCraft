# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

### Building the Project
```bash
# Restore dependencies and build
dotnet restore
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Build with warnings as errors (for CI/CD validation)
dotnet build /p:TreatWarningsAsErrors=true

# Create local NuGet package
./pack-local.sh  # macOS/Linux - Creates packages in ./nupkg/
./pack-local.ps1 # Windows
```

### Running Tests
```bash
# Run all tests (600+ unit tests across 2 test projects)
dotnet test

# Run specific test project
dotnet test FormCraft.UnitTests/FormCraft.UnitTests.csproj
dotnet test FormCraft.ForMudBlazor.UnitTests/FormCraft.ForMudBlazor.UnitTests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class or method
dotnet test --filter "FullyQualifiedName~FormBuilderTests"
dotnet test --filter "DisplayName~Should_Build_Valid_Configuration"

# Run tests by category
dotnet test --filter "Category=Builder"
dotnet test --filter "Category=Renderer"
dotnet test --filter "Category=Security"
```

### Running the Demo Application
```bash
cd FormCraft.DemoBlazorApp
dotnet run
# Navigate to https://localhost:5001 (or http://localhost:5000)
```

### NUKE Build System
The project uses NUKE for sophisticated build automation:
```bash
# Run full build pipeline (macOS/Linux)
./build.sh

# Run full build pipeline (Windows)
./build.ps1

# Available NUKE targets:
# - Clean: Cleans build outputs
# - Restore: Restores NuGet packages
# - Compile: Builds the solution
# - Test: Runs all unit tests
# - Pack: Creates NuGet packages
# - Changelog: Generates changelog using git-cliff
```

## High-Level Architecture

### Solution Structure
```
FormCraft/                      # Core library (framework-agnostic)
├── Builders/                   # Fluent API builders
│   ├── FormBuilder.cs         # Main entry point
│   ├── FieldBuilder.cs        # Individual field configuration
│   └── FieldGroupBuilder.cs   # Field grouping and layout
├── Configuration/              # Configuration models
├── Rendering/                  # Rendering pipeline
│   ├── IFieldRenderer.cs      # Renderer contract
│   └── FieldRendererService.cs # Renderer registry
├── Validation/                 # Validation system
│   └── IFieldValidator.cs     # Validator contract
├── Security/                   # Security features (v2.0.0+)
│   ├── IEncryptionService.cs  # Field encryption
│   └── ICsrfTokenService.cs   # CSRF protection
└── Extensions/                 # Extension methods

FormCraft.ForMudBlazor/         # MudBlazor UI implementation
├── Renderers/                  # MudBlazor-specific renderers
└── Services/                   # UI framework services

FormCraft.DemoBlazorApp/        # Interactive demo application
FormCraft.UnitTests/            # Core library test suite (560+ tests)
FormCraft.ForMudBlazor.UnitTests/ # MudBlazor integration tests (47 tests)
build/                          # NUKE build automation
```

### Target Frameworks
- **net9.0** and **net10.0** - Multi-targeting for .NET 9 and .NET 10

### Core Design Patterns

#### 1. Fluent Builder Pattern (Primary Architecture)
The entire API is built around method chaining with immutable configuration:
```csharp
FormBuilder<TModel>.Create()
    .AddField(x => x.Property, field => field.ConfigureField())
    .AddFieldGroup(group => group.ConfigureGroup())
    .WithLayout(FormLayout.Grid)
    .WithSecurity(security => security.ConfigureSecurity())
    .Build() // Returns immutable IFormConfiguration<TModel>
```

**Key Builder Classes:**
- `FormBuilder<TModel>` - Root builder, entry point via `.Create()`
- `FieldBuilder<TModel, TValue>` - Configures individual fields
- `FieldGroupBuilder<TModel>` - Groups fields with layout options
- `SecurityBuilder<TModel>` - Security features configuration (encryption, CSRF, rate limiting)

#### 2. Strategy Pattern (Field Rendering)
Pluggable rendering system with type-based renderer selection:
```csharp
public interface IFieldRenderer
{
    bool CanRender(Type fieldType, IFieldConfiguration<object, object> field);
    RenderFragment Render<TModel>(IFieldRenderContext<TModel> context);
}
```

**Renderer Registration:**
- Default renderers registered in DI container
- Custom renderers via `.WithCustomRenderer()`
- Priority-based selection when multiple renderers match

#### 3. Command Pattern (Validation)
Async validation with command pattern:
```csharp
public interface IFieldValidator<TModel, TValue>
{
    Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services);
}
```

**Built-in Validators:**
- `RequiredValidator<TModel, TValue>`
- `CustomValidator<TModel, TValue>`
- `AsyncValidator<TModel, TValue>`
- FluentValidation integration via `DynamicFormValidator`

#### 4. Observer Pattern (Field Dependencies)
Reactive field updates based on dependencies using `DependsOn` with callbacks:
```csharp
// Conditional visibility based on another field
.AddField(x => x.State, field => field
    .DependsOn(x => x.Country, (model, country) => {
        // Reset state when country changes
        if (string.IsNullOrEmpty(country))
            model.State = "";
    })
    .VisibleWhen(model => !string.IsNullOrEmpty(model.Country)))

// Cascading updates - clear dependent field when parent changes
.AddField(x => x.City, field => field
    .DependsOn(x => x.State, (model, state) => {
        model.City = ""; // Clear city when state changes
    })
    .VisibleWhen(model => !string.IsNullOrEmpty(model.State)))
```

**Dependency Types:**
- `DependsOn<TDependsOn>(expression, callback)` - Execute action when dependency changes
- `VisibleWhen(predicate)` - Conditional visibility based on model state
- `DisabledWhen(predicate)` - Conditional disabling based on model state

#### 5. Adapter Pattern (UI Framework Integration)
Framework-agnostic core with UI-specific adapters:
```csharp
public interface IUIFrameworkAdapter
{
    RenderFragment RenderField<TModel>(IFieldRenderContext<TModel> context);
    RenderFragment RenderForm<TModel>(IFormConfiguration<TModel> config);
}
```

### Key Abstractions and Extension Points

#### Configuration Abstractions
- `IFormConfiguration<TModel>` - Complete immutable form configuration
- `IFieldConfiguration<TModel, TValue>` - Individual field settings
- `IFieldGroupConfiguration<TModel>` - Group layout and settings
- `IFormSecurity` - Security configuration

#### Rendering Pipeline
1. `IFieldRendererService` - Central rendering coordinator
2. `IFieldRenderContext<TModel>` - Rendering context with model and callbacks
3. `ICustomFieldRenderer<TValue>` - Base for custom renderers
4. `CustomFieldRendererBase<T>` - Simplified custom renderer base class

#### Validation System
- `IFieldValidator<TModel, TValue>` - Core validation contract
- `ValidationResult` - Validation outcome (IsValid, ErrorMessage)
- `DynamicFormValidator` - FluentValidation integration component
- Validators can be sync or async

#### Security Features (v2.0.0+)
```csharp
.WithSecurity(security => security
    // Field-level encryption for sensitive data
    .EncryptField(x => x.SSN)
    .EncryptField(x => x.CreditCard)
    // CSRF protection with custom token field name (default: "__RequestVerificationToken")
    .EnableCsrfProtection()
    // Rate limiting by IP (default) or custom identifier
    .WithRateLimit(maxAttempts: 5, timeWindow: TimeSpan.FromMinutes(1), identifierType: "IP")
    // Audit logging with configurable options
    .EnableAuditLogging(config => {
        config.LogFieldChanges = true;
        config.LogValidationErrors = true;
        config.LogSubmissions = true;
        config.ExcludedFields.Add("Password");  // Exclude sensitive fields from logs
    }))
```

### Important Conventions

#### Fluent API Design Rules
- All builder methods return `this` for chaining
- Configuration is immutable after `.Build()`
- Method naming: `Add*` (add items), `With*` (configure), `Enable*` (features)
- No side effects in builder methods

#### Type Safety and Expression Trees
- Heavy use of generics for compile-time safety
- Expression trees for property binding: `x => x.Property`
- Strong typing throughout: `FieldBuilder<TModel, TValue>`
- No magic strings for property names

#### Validation Behavior
- `Required()` adds validation but NOT HTML5 required attribute
- Browser validation disabled via `novalidate` attribute
- All validation through FluentValidation
- Validation messages from server, not browser
- MudBlazor components don't include `Required` attribute

#### Testing Patterns
```csharp
// Arrange-Act-Assert pattern with Shouldly assertions
[Fact]
public void MethodName_Should_ExpectedBehavior_When_Condition()
{
    // Arrange
    var builder = FormBuilder<TestModel>.Create();

    // Act
    var result = builder.AddField(x => x.Name);

    // Assert
    result.ShouldBeSameAs(builder);
}
```

#### MudBlazor Component Testing (bUnit)
```csharp
// Use MudBlazorTestBase for component tests
public class MyTests : MudBlazorTestBase  // Inherits from BunitContext
{
    [Fact]
    public void Component_Should_Render_Field()
    {
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        component.FindComponent<MudTextField<string>>().ShouldNotBeNull();
    }
}
```

### Quick API Reference

#### FieldBuilder<TModel, TValue> Methods
| Method | Description |
|--------|-------------|
| `WithLabel(string)` | Sets the display label |
| `WithPlaceholder(string)` | Sets placeholder text |
| `WithHelpText(string)` | Adds help text below field |
| `WithCssClass(string)` | Adds CSS class to field |
| `WithInputType(string)` | Sets HTML5 input type (email, tel, password, date, number) |
| `Required(string?)` | Marks field as required with optional error message |
| `Disabled(bool)` | Disables field interaction |
| `ReadOnly(bool)` | Makes field non-editable |
| `VisibleWhen(Func<TModel, bool>)` | Conditional visibility |
| `DisabledWhen(Func<TModel, bool>)` | Conditional disabling |
| `WithAttribute(string, object)` | Adds custom HTML attribute |
| `WithAttributes(Dictionary<string, object>)` | Adds multiple attributes |
| `WithValidator(IFieldValidator)` | Adds custom validator instance |
| `WithValidator(Func<TValue, bool>, string)` | Adds sync validation function |
| `WithAsyncValidator(Func<TValue, Task<bool>>, string)` | Adds async validation function |
| `DependsOn<TDep>(Expression, Action<TModel, TDep>)` | Creates field dependency with callback |
| `WithCustomTemplate(RenderFragment)` | Custom Blazor template |
| `WithCustomRenderer(IFieldRenderer)` | Custom renderer instance |
| `WithOrder(int)` | Sets field display order |

#### FieldBuilder Extension Methods
| Method | Description |
|--------|-------------|
| `AsTextArea(int lines, int? maxLength)` | Multi-line text area |
| `WithOptions(params (TValue, string)[])` | Dropdown options as tuples |
| `WithSelectOptions(IEnumerable<SelectOption>)` | Dropdown options as SelectOption collection |
| `AsMultiSelect(params (TValue, string)[])` | Multiple selection |
| `AsSlider(TValue min, max, step, showValue)` | Slider control for numeric types |
| `WithEmailValidation(string?)` | Email format validation |
| `WithMinLength(int, string?)` | Minimum length validation |
| `WithMaxLength(int, string?)` | Maximum length validation |
| `WithRange(TValue min, max, string?)` | Range validation for comparable types |
| `AsFileUpload(string[]?, long?, bool, bool)` | Single file upload |
| `AsMultipleFileUpload(int, string[]?, long?, bool, bool)` | Multiple file upload |
| `WithCustomRenderer<TRenderer>()` | Custom renderer by type |

#### FluentFormBuilderExtensions (Convenience Methods)
| Method | Description |
|--------|-------------|
| `AddRequiredTextField(expr, label, placeholder?, minLength, maxLength)` | Required text with validation |
| `AddEmailField(expr, label?, placeholder?)` | Email with format validation |
| `AddNumericField(expr, label, min, max, required)` | Integer with range |
| `AddDecimalField(expr, label, min, max, required, placeholder?)` | Decimal with range |
| `AddCurrencyField(expr, label, symbol, required)` | Currency field |
| `AddPercentageField(expr, label, required)` | Percentage (0-100) |
| `AddDropdownField(expr, label, params options)` | Dropdown with options |
| `AddPhoneField(expr, label?, required)` | Phone with validation |
| `AddPasswordField(expr, label?, minLength, requireSpecialChars)` | Password with strength |
| `AddCheckboxField(expr, label, helpText?)` | Boolean checkbox |
| `AddFileUploadField(expr, label, types?, maxSize, required)` | File upload |
| `AddTextArea(expr, label, rows, config?)` | Multi-line text |
| `AddRequiredField(expr, label, placeholder?, errorMessage?)` | Simple required field |
| `AddOptionalField(expr, label, placeholder?)` | Optional field |

### Common Development Patterns

#### Adding a Custom Field Renderer
```csharp
// 1. Create renderer class
public class ColorPickerRenderer : CustomFieldRendererBase<string>
{
    protected override RenderFragment RenderField(IFieldRenderContext<string> context)
    {
        return builder => {
            builder.OpenComponent<MudColorPicker>(0);
            builder.AddAttribute(1, "Value", context.Value);
            builder.AddAttribute(2, "ValueChanged", context.ValueChanged);
            builder.CloseComponent();
        };
    }
}

// 2. Register globally in DI
services.AddFormCraft(options => {
    options.RegisterRenderer(new ColorPickerRenderer());
});

// 3. Or use inline
.WithCustomRenderer(new ColorPickerRenderer())
```

#### Creating Reusable Field Configurations
```csharp
public static class FormExtensions
{
    public static FormBuilder<TModel> AddEmailField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string>> propertyExpression)
        where TModel : new()
    {
        return builder.AddField(propertyExpression, field => field
            .WithLabel("Email Address")
            .WithPlaceholder("user@example.com")
            .WithInputType("email")
            .Required("Email is required")
            .WithValidator(new EmailValidator<TModel>()));
    }
}
```

#### Implementing Field Dependencies
```csharp
// Conditional visibility - show state field only when country is selected
.AddField(x => x.State, field => field
    .WithLabel("State")
    .DependsOn(x => x.Country, (model, country) => {
        // Clear state when country changes
        model.State = "";
    })
    .VisibleWhen(model => model.Country == "USA"))

// Cascading dropdowns with options based on parent
.AddField(x => x.City, field => field
    .WithLabel("City")
    .DependsOn(x => x.State, (model, state) => {
        // Clear city and update available options when state changes
        model.City = "";
    })
    .VisibleWhen(model => !string.IsNullOrEmpty(model.State)))

// Read-only calculated field (calculate in form submit or use computed property)
.AddField(x => x.Total, field => field
    .WithLabel("Total")
    .ReadOnly())
```

#### Reusable Form Patterns
```csharp
// Create reusable form configuration methods
public static class FormPatterns
{
    // Login form pattern
    public static IFormConfiguration<LoginModel> CreateLoginForm()
    {
        return FormBuilder<LoginModel>.Create()
            .AddEmailField(x => x.Email)
            .AddPasswordField(x => x.Password)
            .AddCheckboxField(x => x.RememberMe, "Remember me")
            .Build();
    }

    // Contact form pattern with field groups
    public static IFormConfiguration<ContactModel> CreateContactForm()
    {
        return FormBuilder<ContactModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Personal Information")
                .WithColumns(2)
                .ShowInCard()
                .AddField(x => x.FirstName, f => f.WithLabel("First Name").Required())
                .AddField(x => x.LastName, f => f.WithLabel("Last Name").Required()))
            .AddFieldGroup(group => group
                .WithGroupName("Contact Details")
                .WithColumns(1)
                .ShowInCard()
                .AddField(x => x.Email, f => f.WithLabel("Email").Required().WithEmailValidation())
                .AddField(x => x.Message, f => f.WithLabel("Message").AsTextArea(5)))
            .Build();
    }
}
```

### Advanced Features

#### Security Configuration
```csharp
.WithSecurity(security => security
    // Field-level encryption (uses IEncryptionService from DI)
    .EncryptField(x => x.SSN)
    .EncryptField(x => x.CreditCard)

    // CSRF protection (token field name is configurable)
    .EnableCsrfProtection("__RequestVerificationToken")

    // Rate limiting (maxAttempts, timeWindow, identifierType)
    .WithRateLimit(5, TimeSpan.FromMinutes(1), "IP")

    // Audit logging with configuration
    .EnableAuditLogging(config => {
        config.LogFieldChanges = true;
        config.LogSubmissions = true;
        config.ExcludedFields.Add("Password");
    }))
```

#### Field Groups with Layouts
```csharp
.AddFieldGroup(group => group
    .WithGroupName("Contact Information")
    .WithColumns(2)
    .ShowInCard(elevation: 2)
    .WithCssClass("contact-group")
    .AddField(x => x.Email, field => field.WithLabel("Email"))
    .AddField(x => x.Phone, field => field.WithLabel("Phone"))
    .AddField(x => x.Address, field => field.WithLabel("Address")))

// Available FieldGroupBuilder methods:
// .WithGroupName(string) - Sets the display name for the group
// .WithColumns(int) - Sets grid columns (1-6)
// .WithCssClass(string) - Adds CSS class to group container
// .ShowInCard(int elevation) - Renders group in card with elevation (0-24)
// .WithOrder(int) - Sets rendering order
// .WithHeaderRightContent(RenderFragment) - Custom header content
// .AddField<TValue>(expression, config) - Adds field to group
```

#### Async Operations
```csharp
// Async validation - checks if value is valid asynchronously
// Signature: WithAsyncValidator(Func<TValue, Task<bool>> validation, string errorMessage)
.AddField(x => x.Username, field => field
    .WithLabel("Username")
    .Required()
    .WithAsyncValidator(
        async value => await CheckUsernameAvailableAsync(value),
        "Username is already taken"))

// For complex async validation logic, create a custom validator class
public class UniqueEmailValidator<TModel> : IFieldValidator<TModel, string>
{
    private readonly IApiService _api;

    public UniqueEmailValidator(IApiService api) => _api = api;

    public string? ErrorMessage { get; set; } = "Email is already registered";

    public async Task<ValidationResult> ValidateAsync(TModel model, string value, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(value)) return ValidationResult.Success();

        var isUnique = await _api.CheckEmailUniqueAsync(value);
        return isUnique
            ? ValidationResult.Success()
            : ValidationResult.Failure(ErrorMessage!);
    }
}

// Use with dependency injection
.AddField(x => x.Email, field => field
    .WithValidator(serviceProvider.GetRequiredService<UniqueEmailValidator<MyModel>>()))
```

### Versioning and Release Process
- **Versioning**: MinVer for automatic semantic versioning from Git tags
- **Changelog**: Automated via git-cliff using conventional commits
- **Commits**: Follow conventional commits (feat:, fix:, docs:, test:, refactor:)
- **Releases**: Tag-based (v1.0.0, v2.0.0, etc.)
- **CI/CD**: GitHub Actions with automated NuGet publishing