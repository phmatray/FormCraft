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
```

### Running the Demo Application
```bash
cd FormCraft.DemoBlazorApp
dotnet run
# Navigate to https://localhost:5001
```

### Linting and Code Quality
The project uses .NET's built-in code analysis. Ensure code follows C# conventions and passes all analyzers:
```bash
dotnet build /p:TreatWarningsAsErrors=true
```

### NUKE Build System
The project includes a NUKE build automation system:
```bash
# Run NUKE build (macOS/Linux)
./build.sh

# Run NUKE build (Windows)
./build.ps1
```

## High-Level Architecture

### Project Structure
- **FormCraft/** - Main library providing fluent API for dynamic form generation in Blazor
- **FormCraft.DemoBlazorApp/** - Interactive demo showcasing library features
- **FormCraft.UnitTests/** - Comprehensive test suite using xUnit, bUnit, FakeItEasy, and Shouldly

### Core Design Patterns

1. **Fluent Builder Pattern**: The entire API revolves around method chaining
   - Entry point: `FormBuilder<TModel>.Create()`
   - Field configuration: `.AddField(x => x.Property, field => field.WithLabel("Label"))`
   - Group management: `.AddFieldGroup(group => group.AddField(...))`

2. **Rendering Pipeline**: Type-based renderer selection
   - `IFieldRendererService` manages renderer registry
   - Renderers implement `IFieldRenderer` for each supported type
   - Custom renderers can be registered via DI

3. **Validation Architecture**: Pluggable validation system
   - Built-in validators: `RequiredValidator`, `CustomValidator`, `AsyncValidator`
   - Integration with FluentValidation via `DynamicFormValidator`
   - Validators implement `IFieldValidator`

4. **Dependency System**: Reactive field updates
   - Fields can depend on other fields via `DependsOn()`
   - Dependencies trigger visibility/value updates
   - Managed through `IFieldDependency<TModel>`

### Key Extension Points
- Custom field renderers: Implement `IFieldRenderer` or extend `CustomFieldRendererBase<T>`
- Custom validators: Implement `IFieldValidator<TModel, TValue>`
- Form templates: Extend `FormTemplates` class
- Field types: Add new renderers to `FieldRendererService`

### Important Conventions
- All public APIs use fluent interfaces returning `this` for chaining
- Field configurations are immutable once built
- Validation happens at both field and form levels
- MudBlazor components are used for all UI elements
- Follow Conventional Commits (feat:, fix:, docs:, etc.)
- Changelog is auto-generated via git-cliff

### Validation Behavior
- The `Required()` method adds validation but does NOT set HTML5 required attribute
- Browser validation is disabled in favor of FluentValidation
- The form has `novalidate` attribute added via JavaScript to prevent browser validation
- All validation is handled through FluentValidation validators
- Validation messages come from FluentValidation, not the browser
- MudBlazor field components do not include the `Required` attribute

### Common Development Patterns

#### Adding a Custom Field Renderer
```csharp
public class MyCustomRenderer : CustomFieldRendererBase<MyType>
{
    protected override RenderFragment RenderField(IFieldRenderContext<MyType> context)
    {
        // Return RenderFragment with MudBlazor components
    }
}

// Register in DI or use inline:
.WithCustomRenderer(new MyCustomRenderer())
```

#### Creating Reusable Field Configurations
```csharp
public static class MyFormExtensions
{
    public static FormBuilder<TModel> AddCustomField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string>> propertyExpression)
        where TModel : new()
    {
        return builder.AddField(propertyExpression, field => field
            .WithLabel("Custom Label")
            .WithPlaceholder("Enter value...")
            .Required());
    }
}
```