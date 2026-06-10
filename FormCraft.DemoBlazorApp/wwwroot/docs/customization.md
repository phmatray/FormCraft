# Customization

Learn how to customize FormCraft to fit your specific needs.

## Custom Field Renderers

Create your own field renderers for specialized input types.

### Creating a Custom Renderer

For most cases, derive from `CustomFieldRendererBase<TValue>`:

```csharp
public class ColorPickerRenderer : CustomFieldRendererBase<string>
{
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", "color");
            builder.AddAttribute(2, "value", GetValue(context) ?? "#000000");
            builder.CloseElement();
        };
    }
}

// Attach it to a field (type arguments: model, value, renderer)
.AddField(x => x.Color, field => field
    .WithCustomRenderer<MyModel, string, ColorPickerRenderer>())
```

To replace rendering for a whole field *type*, implement the `IFieldRenderer` interface:

```csharp
public class MyTypeFieldRenderer : IFieldRenderer
{
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(MyCustomType);
    }

    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            builder.OpenComponent<MyCustomComponent>(0);
            builder.AddAttribute(1, "Value", context.CurrentValue);
            builder.AddAttribute(2, "ValueChanged", context.OnValueChanged);
            builder.CloseComponent();
        };
    }
}
```

### Registering Custom Renderers

Add your renderer to the service collection:

```csharp
builder.Services.AddScoped<IFieldRenderer, MyTypeFieldRenderer>();
```

## Custom Validators

Create reusable validation logic for your specific business rules.

### Simple Custom Validator

```csharp
public class BusinessRuleValidator<TModel> : IFieldValidator<TModel, string>
{
    public string? ErrorMessage { get; set; } = "Value violates business rule";

    public async Task<ValidationResult> ValidateAsync(TModel model, string value, IServiceProvider services)
    {
        // Your validation logic
        if (await IsValidBusinessRule(value))
        {
            return ValidationResult.Success();
        }
        
        return ValidationResult.Failure("Value violates business rule");
    }
    
    private async Task<bool> IsValidBusinessRule(string value)
    {
        // Implement your business logic
        return await Task.FromResult(true);
    }
}
```

### Using Custom Validators

```csharp
.AddField(x => x.BusinessCode, field => field
    .WithValidator(new BusinessRuleValidator<MyModel>()))
```

## Custom Themes

### MudBlazor Theme Integration

FormCraft inherits MudBlazor's theming system:

```csharp
// In Program.cs
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
});

// Custom theme
var theme = new MudTheme()
{
    Palette = new PaletteLight()
    {
        Primary = "#1976d2",
        Secondary = "#dc004e",
        // ... other colors
    }
};
```

### Custom CSS Classes

Apply custom styling to forms and fields:

```csharp
.AddField(x => x.SpecialField, field => field
    .WithCssClass("my-special-field"))
```

```css
.my-special-field {
    background: linear-gradient(45deg, #f0f0f0, #ffffff);
    border-radius: 8px;
    padding: 1rem;
}
```

## Layout Customization

### Custom Form Layouts

Create your own layout enum and logic:

```csharp
public enum MyCustomLayout
{
    Sidebar,
    Wizard,
    Accordion
}

// Custom layout logic
public static string GetCustomLayoutClass(MyCustomLayout layout)
{
    return layout switch
    {
        MyCustomLayout.Sidebar => "d-flex",
        MyCustomLayout.Wizard => "wizard-container",
        MyCustomLayout.Accordion => "accordion-form",
        _ => ""
    };
}
```

### Field Groups

Organize fields into logical groups with `AddFieldGroup`:

```csharp
.AddFieldGroup(group => group
    .WithGroupName("Personal Information")
    .WithColumns(2)
    .ShowInCard()
    .AddField(x => x.FirstName, field => field.WithLabel("First Name"))
    .AddField(x => x.LastName, field => field.WithLabel("Last Name")))
.AddFieldGroup(group => group
    .WithGroupName("Contact Details")
    .AddField(x => x.Email, field => field.WithLabel("Email"))
    .AddField(x => x.Phone, field => field.WithLabel("Phone")))
```

## Advanced Customization

### Custom Form Builder

Extend the FormBuilder with your own methods:

```csharp
public static class MyFormBuilderExtensions
{
    public static FormBuilder<TModel> AddCurrencyField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, decimal>> expression,
        string label,
        string currency = "USD") where TModel : new()
    {
        return builder.AddField(expression, field => field
            .WithLabel(label)
            .WithAttribute("currency", currency)
            .WithValidator(value => value >= 0, "Amount must be positive"));
    }
}
```

### Custom Component Templates

Override default rendering with custom Blazor components:

```csharp
.AddField(x => x.ComplexData, field => field
    .WithCustomTemplate(context => builder =>
    {
        builder.OpenComponent<MyComplexComponent>(0);
        builder.AddAttribute(1, "Data", context.Value);
        builder.AddAttribute(2, "OnChanged", context.ValueChanged);
        builder.CloseComponent();
    }))
```

## Configuration Options

### Form-Level Settings

Configure layout and indicators per form on the builder:

```csharp
FormBuilder<MyModel>.Create()
    .WithLayout(FormLayout.Horizontal)
    .ShowRequiredIndicator(true, "*")
    .ShowValidationSummary()
    // ... fields
    .Build();
```

### Localization

Support multiple languages:

```csharp
// Resource files: Resources/FormLabels.en.resx, Resources/FormLabels.fr.resx
.AddField(x => x.Name, field => field
    .WithLabel(Localizer["NameLabel"])
    .Required(Localizer["NameRequired"]))
```