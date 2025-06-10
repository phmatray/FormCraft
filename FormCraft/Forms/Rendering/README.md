# FormCraft Rendering Architecture

## Overview

The FormCraft rendering system has been refactored to support multiple UI frameworks through a plugin-based architecture. This allows developers to use FormCraft with different UI libraries like MudBlazor, Blazor Fluent UI, Bootstrap, or Tailwind CSS.

## Key Components

### 1. Framework-Agnostic Interfaces

- **`IFieldComponent<TModel>`**: Base interface for all field components
- **`ITextFieldComponent<TModel>`**: Interface for text input fields
- **`INumericFieldComponent<TModel, TValue>`**: Interface for numeric input fields
- **`IBooleanFieldComponent<TModel>`**: Interface for boolean fields
- **`IDateTimeFieldComponent<TModel>`**: Interface for date/time fields
- **`ISelectFieldComponent<TModel, TValue>`**: Interface for select/dropdown fields
- **`IFileUploadFieldComponent<TModel>`**: Interface for file upload fields

### 2. UI Framework Adapter

**`IUIFrameworkAdapter`** provides framework-specific implementations for:
- Field renderers for each data type
- Form container rendering
- Field group rendering
- Submit button rendering
- Validation message rendering

### 3. Base Classes

- **`FieldComponentBase<TModel, TValue>`**: Base class for field components with common functionality
- **`FieldRendererBase`**: Base class for field renderers using Razor components
- **`FrameworkAgnosticFieldRenderer<TComponent>`**: Base class for framework-agnostic renderers

## Architecture Benefits

1. **Separation of Concerns**: UI framework code is isolated from core form logic
2. **Extensibility**: New UI frameworks can be added without modifying core code
3. **Type Safety**: Strong typing throughout the rendering pipeline
4. **Testability**: Components can be tested independently of UI frameworks
5. **Consistency**: Common patterns across all UI framework implementations

## Usage Example

```csharp
// Configure with MudBlazor (default)
builder.Services.AddFormCraft();

// Or configure with a specific framework
builder.Services.AddFormCraft(options =>
{
    options.UseFramework("MudBlazor");
});

// Future: Configure with other frameworks
builder.Services.AddFormCraft(options =>
{
    var fluentUIAdapter = new FluentUIFrameworkAdapter();
    options.RegisterFramework(fluentUIAdapter);
    options.UseFramework("FluentUI");
});
```

## Creating a New UI Framework Adapter

1. Implement `IUIFrameworkAdapter`
2. Create framework-specific field components
3. Register the adapter with `UIFrameworkConfiguration`

```csharp
public class MyFrameworkAdapter : IUIFrameworkAdapter
{
    public string FrameworkName => "MyFramework";
    
    public IFieldRenderer TextFieldRenderer => new MyTextFieldRenderer();
    // ... implement other properties
    
    public RenderFragment RenderForm(RenderFragment content, string? cssClass = null)
    {
        // Framework-specific form rendering
    }
    // ... implement other methods
}
```

## Migration Guide

Existing code using FormCraft will continue to work with MudBlazor as the default UI framework. No changes are required unless you want to use a different UI framework.

### Before (Direct MudBlazor dependency):
```csharp
public class StringFieldRenderer : IFieldRenderer
{
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        // Direct MudTextField usage
    }
}
```

### After (Framework-agnostic):
```csharp
public class StringFieldRenderer : FieldRendererBase
{
    protected override Type ComponentType => typeof(StringFieldComponent<>);
}
```

The actual UI framework component is determined at runtime based on configuration.