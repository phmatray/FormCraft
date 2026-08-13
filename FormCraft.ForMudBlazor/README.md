# FormCraft.ForMudBlazor

MudBlazor UI framework implementation for FormCraft dynamic forms.

## Installation

```bash
dotnet add package FormCraft.ForMudBlazor
```

## Usage

```csharp
// In Program.cs
builder.Services.AddFormCraft();
builder.Services.AddFormCraftMudBlazor();
```

## Project Structure

The project is organized by feature, with each field type having its own folder containing both the component and renderer:

```
FormCraft.ForMudBlazor/
├── Extensions/
│   └── ServiceCollectionExtensions.cs    # DI registration
├── Features/
│   ├── TextField/
│   │   ├── MudBlazorTextFieldComponent.razor
│   │   └── MudBlazorTextFieldRenderer.cs
│   ├── NumericField/
│   │   ├── MudBlazorNumericFieldComponent.razor
│   │   └── MudBlazorNumericFieldRenderer.cs
│   ├── BooleanField/
│   │   ├── MudBlazorBooleanFieldComponent.razor
│   │   └── MudBlazorBooleanFieldRenderer.cs
│   ├── DateTimeField/
│   │   ├── MudBlazorDateTimeFieldComponent.razor
│   │   └── MudBlazorDateTimeFieldRenderer.cs
│   ├── SelectField/
│   │   ├── MudBlazorSelectFieldComponent.razor
│   │   └── MudBlazorSelectFieldRenderer.cs
│   ├── FileUploadField/
│   │   ├── MudBlazorFileUploadFieldComponent.razor
│   │   └── MudBlazorFileUploadFieldRenderer.cs
│   ├── CustomFields/
│   │   └── RatingRenderer.cs
│   ├── FormContainer/
│   │   ├── FormCraftComponent.razor
│   │   └── FormCraftComponent.razor.cs
│   └── Validation/
│       ├── FieldValidationMessage.razor
│       └── FieldValidationMessage.razor.cs
```

## ⚠️ Moved to FormCraft core (#279) — breaking

`DynamicFormValidator<TModel>` used to live in this package, in namespace
`FormCraft.ForMudBlazor`. It references nothing from MudBlazor, so a second UI adapter could not
reuse it without copying — it now lives in **`FormCraft`** core as
`FormCraft.DynamicFormValidator<TModel>`, and both adapters render the same component.

**If you name it in your own markup**, add `@using FormCraft` to that file (or your
`_Imports.razor`) — `@using FormCraft.ForMudBlazor` alone no longer finds it:

```razor
@using FormCraft
@using FormCraft.ForMudBlazor

<EditForm EditContext="@editContext">
    <DynamicFormValidator TModel="MyModel" Configuration="@formConfig" />
</EditForm>
```

Most applications never name it: `<FormCraftComponent TModel="..." />` renders it for you, and that
is unchanged. No forwarding shim was left behind on purpose — both packages surface types in
namespace `FormCraft`, so a same-named shim here would make `DynamicFormValidator` an *ambiguous
reference* (CS0104) in any file importing both namespaces, which is the normal setup and would break
more callers than it rescued.

`MudBlazorUIFrameworkAdapter` was removed in the same release, along with the core
`IUIFrameworkAdapter`, `FrameworkAgnosticFieldRenderer<T>` and `UIFrameworkConfiguration` types it
implemented. Nothing consumed them; the real extension point is `FieldRendererBase` plus DI
registration order.

`.WithNativeRequired(...)` also moved to core, but its **namespace is unchanged** (`FormCraft` in
both packages), so existing call sites compile untouched.

## Features

- **Feature-based organization**: Each field type is self-contained with its component and renderer
- **MudBlazor components**: Uses MudBlazor's rich component library
- **Type-safe rendering**: Strong typing throughout the rendering pipeline
- **Validation support**: Integrated validation with MudBlazor's validation system
- **Custom field support**: Extensible architecture for custom field types

## Creating Custom Fields

To add a new field type:

1. Create a new feature folder under `Features/`
2. Add your component (`.razor` file) and renderer (`.cs` file)
3. Register the renderer in `ServiceCollectionExtensions.cs`

Example:
```csharp
// Features/ColorPickerField/MudBlazorColorPickerFieldRenderer.cs
public class MudBlazorColorPickerFieldRenderer : FieldRendererBase
{
    protected override Type ComponentType => typeof(MudBlazorColorPickerFieldComponent<>);
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => fieldType == typeof(string) && field.AdditionalAttributes.ContainsKey("color-picker");
}

// Features/ColorPickerField/MudBlazorColorPickerFieldComponent.razor
@namespace FormCraft.ForMudBlazor.Features.ColorPickerField
@typeparam TModel
@inherits FieldComponentBase<TModel, string>

<MudColorPicker T="string" 
                Label="@Label" 
                @bind-Value="@CurrentValue" 
                Required="@IsRequired"
                ReadOnly="@IsReadOnly"
                Disabled="@IsDisabled" />
```

## Dependencies

- FormCraft (core library)
- MudBlazor (UI components)
- Microsoft.AspNetCore.Components.Web

## License

This package is part of the FormCraft project and follows the same MIT license.