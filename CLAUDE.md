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
# Run all tests (400+ unit tests)
dotnet test

# Run specific test project
dotnet test FormCraft.UnitTests/FormCraft.UnitTests.csproj

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
FormCraft.UnitTests/            # Comprehensive test suite
build/                          # NUKE build automation
```

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
Reactive field updates based on dependencies:
```csharp
.AddField(x => x.TotalPrice)
    .DependsOn(x => x.Quantity, x => x.Price)
    .WithValueProvider((model, services) => model.Quantity * model.Price)
    .WithVisibilityProvider(model => model.Quantity > 0)
```

**Dependency Types:**
- Value dependencies - Auto-calculate field values
- Visibility dependencies - Show/hide fields conditionally
- Validation dependencies - Conditional validation rules

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
    .EncryptField(x => x.SSN, algorithm: "AES256")
    .EncryptField(x => x.CreditCard)
    .EnableCsrfProtection()
    .WithRateLimit(maxRequests: 5, window: TimeSpan.FromMinutes(1))
    .EnableAuditLogging(logger => logger.LogToDatabase()))
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
// Conditional visibility
.AddField(x => x.State)
    .DependsOn(x => x.Country)
    .WithVisibilityProvider(model => model.Country == "USA")

// Calculated values
.AddField(x => x.Total)
    .DependsOn(x => x.Quantity, x => x.Price, x => x.TaxRate)
    .WithValueProvider((model, _) => 
        model.Quantity * model.Price * (1 + model.TaxRate))
    .ReadOnly()
```

#### Form Templates
```csharp
// Use predefined templates
var form = FormTemplates.CreateLoginForm<LoginModel>();
var form = FormTemplates.CreateRegistrationForm<UserModel>();

// Create custom template
public static class MyTemplates
{
    public static FormBuilder<T> CreateWizardForm<T>() where T : new()
    {
        return FormBuilder<T>.Create()
            .WithLayout(FormLayout.Wizard)
            .WithNavigation(nav => nav.EnableStepIndicator());
    }
}
```

### Advanced Features

#### Security Configuration
```csharp
.WithSecurity(security => security
    // Field-level encryption
    .EncryptField(x => x.SSN)
    .EncryptField(x => x.CreditCard, algorithm: "AES256")
    
    // CSRF protection
    .EnableCsrfProtection()
    .WithCsrfTokenProvider(customProvider)
    
    // Rate limiting
    .WithRateLimit(5, TimeSpan.FromMinutes(1))
    
    // Audit logging
    .EnableAuditLogging()
    .WithAuditLogger(customLogger))
```

#### Field Groups with Layouts
```csharp
.AddFieldGroup(group => group
    .WithGroupName("Contact Information")
    .WithColumns(2)
    .ShowInCard(elevation: 2)
    .Collapsible(defaultExpanded: true)
    .AddField(x => x.Email)
    .AddField(x => x.Phone)
    .AddField(x => x.Address, field => field.FullWidth()))
```

#### Async Operations
```csharp
// Async validation
.WithAsyncValidator(async (value, services) => {
    var api = services.GetRequiredService<IApiService>();
    var isUnique = await api.CheckUniqueAsync(value);
    return isUnique 
        ? ValidationResult.Success()
        : ValidationResult.Error("Value must be unique");
})

// Async value provider
.WithAsyncValueProvider(async (model, services) => {
    var api = services.GetRequiredService<IApiService>();
    return await api.GetDefaultValueAsync(model.Id);
})
```

### Versioning and Release Process
- **Versioning**: MinVer for automatic semantic versioning from Git tags
- **Changelog**: Automated via git-cliff using conventional commits
- **Commits**: Follow conventional commits (feat:, fix:, docs:, test:, refactor:)
- **Releases**: Tag-based (v1.0.0, v2.0.0, etc.)
- **CI/CD**: GitHub Actions with automated NuGet publishing