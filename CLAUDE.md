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

# Build with warnings as errors (for CI quality checks)
dotnet build /p:TreatWarningsAsErrors=true

# Create local NuGet package
./pack-local.sh  # macOS/Linux
./pack-local.ps1 # Windows
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test FormCraft.UnitTests/FormCraft.UnitTests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class or method
dotnet test --filter "FullyQualifiedName~FormBuilderTests"
dotnet test --filter "DisplayName~Should_Build_Valid_Configuration"

# Run tests for a specific category
dotnet test --filter "Category=Integration"
```

### Running the Demo Application
```bash
cd FormCraft.DemoBlazorApp
dotnet run
# Navigate to https://localhost:5001

# Run with hot reload
dotnet watch run
```

### NUKE Build System
```bash
# Run NUKE build (macOS/Linux)
./build.sh

# Run NUKE build (Windows)
./build.ps1

# Run specific NUKE targets
./build.sh --target Clean
./build.sh --target Compile
./build.sh --target Test
```

### Version Management
The project uses GitVersion for automatic versioning based on git history:
- Main branch: Release versions
- Develop branch: Alpha versions with minor increments
- Release branches: Beta versions
- Feature branches: Use branch name as tag

## High-Level Architecture

### Project Structure
- **FormCraft/** - Core library providing fluent API for dynamic form generation
  - `/Forms` - Core form building logic and abstractions
  - `/Components` - Base component classes
- **FormCraft.ForMudBlazor/** - MudBlazor UI framework adapter
  - `/Features` - Feature-based organization (each field type in its folder)
  - `/Extensions` - DI registration and extension methods
- **FormCraft.DemoBlazorApp/** - Interactive demo and documentation site
- **FormCraft.UnitTests/** - Test suite using xUnit, bUnit, FakeItEasy, and Shouldly

### Core Design Patterns

1. **Fluent Builder Pattern**: Method chaining for form configuration
   ```csharp
   FormBuilder<TModel>.Create()
       .AddField(x => x.Property, field => field
           .WithLabel("Label")
           .Required()
           .WithValidator(...))
       .Build()
   ```

2. **Rendering Pipeline**: Modular, type-based renderer system
   - `IFieldRendererService` - Central registry for field renderers
   - `IFieldRenderer` - Interface for type-specific renderers
   - `FieldComponentBase<TModel, TValue>` - Base class for field components
   - Renderers check `CanRender()` to determine if they handle a field type

3. **UI Framework Abstraction**: Pluggable UI framework support
   - `IUIFrameworkAdapter` - Abstraction for UI framework integration
   - `MudBlazorUIFrameworkAdapter` - MudBlazor implementation
   - `FormCraftComponent<TModel>` - Main form container component

4. **Validation Architecture**: Multi-layer validation system
   - `IFieldValidator<TModel, TValue>` - Field-level validation interface
   - `RequiredValidator`, `CustomValidator`, `AsyncValidator` - Built-in validators
   - `DynamicFormValidator<TModel>` - Integrates with Blazor's EditContext
   - Browser validation disabled via JavaScript `novalidate` attribute

5. **Field Dependencies**: Reactive field relationships
   - `IFieldDependency<TModel>` - Dependency interface
   - `DependsOn()` - Creates field dependencies
   - `VisibleWhen()` - Conditional visibility
   - Dependencies stored in `IFormConfiguration.FieldDependencies`

### Field Rendering Architecture

The `FormCraftComponent` renders fields using a refactored approach:
- `RenderField()` - Main dispatcher method
- Type-specific render methods: `RenderTextField()`, `RenderNumericField<T>()`, etc.
- `AddCommonFieldAttributes()` - Shared attribute logic
- Fields with options render as select/dropdown (checked before type-based rendering)

### Key Conventions

- **API Design**: All fluent methods return builder instance for chaining
- **Immutability**: Configurations are immutable after `.Build()`
- **Component Structure**: Feature-based folders containing both `.razor` and `.cs` files
- **Validation Messages**: Multiple validators can add separate messages (displayed with proper spacing)
- **Field Selection**: Options check happens before type check to ensure dropdowns render correctly
- **Null Safety**: Nullable reference types enabled project-wide

### Important Technical Details

- **AddField Syntax**: Always use lambda configuration: `.AddField(x => x.Prop, field => field.WithLabel(...))`
- **Service Registration**: Call both `AddFormCraft()` and `AddFormCraftMudBlazor()` in DI
- **Browser Validation**: Disabled via JavaScript to use FluentValidation exclusively
- **Field Components**: Inherit from `FieldComponentBase<TModel, TValue>`
- **Custom Renderers**: Implement `IFieldRenderer` or extend type-specific base classes
- **Validation Messages**: Use `d-block` CSS class for proper spacing between messages

### Common Development Patterns

#### Adding a New Field Type
1. Create feature folder: `/Features/MyFieldType/`
2. Add component: `MudBlazorMyFieldComponent.razor`
3. Add renderer: `MudBlazorMyFieldRenderer.cs`
4. Register in `ServiceCollectionExtensions.cs`

#### Creating Form Templates
```csharp
public static class FormTemplates
{
    public static IFormConfiguration<T> CreateTemplate<T>() where T : new()
    {
        return FormBuilder<T>.Create()
            // Add common fields
            .Build();
    }
}
```

#### Custom Validation with Dependencies
```csharp
.AddField(x => x.ConfirmPassword, field => field
    .WithValidator((value, model) => value == model.Password, "Passwords must match"))
```