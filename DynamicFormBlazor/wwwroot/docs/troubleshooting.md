# Troubleshooting

Common issues and solutions when working with Dynamic Form Blazor.

## Common Issues

### Validation Not Working

**Problem**: Form validation is not triggering or displaying error messages.

**Solutions**:

1. **Ensure ValidationMessageStore is properly configured**:
```csharp
protected override void OnInitialized()
{
    _editContext = new EditContext(Model);
    _messageStore = new ValidationMessageStore(_editContext);
}
```

2. **Check validator registration**:
```csharp
// Make sure validators are added to field configuration
.AddField(x => x.Email)
    .Required("Email is required")
    .WithEmailValidation()
```

3. **Verify EditForm setup**:
```html
<EditForm Model="@model" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <DynamicFormValidator TModel="MyModel" Configuration="@formConfig" />
    <!-- form content -->
</EditForm>
```

### Field Not Rendering

**Problem**: Field appears in configuration but doesn't render in the form.

**Solutions**:

1. **Check visibility conditions**:
```csharp
.AddField(x => x.ConditionalField)
    .VisibleWhen(model => model.ShowField) // Ensure this evaluates to true
```

2. **Verify field renderer registration**:
```csharp
// In Program.cs
builder.Services.AddDynamicForms(); // This registers default renderers
```

3. **Check field type support**:
```csharp
// Ensure the field type has a corresponding renderer
// Or add a custom renderer for unsupported types
```

### Type Casting Errors

**Problem**: `InvalidCastException` when working with generic field values.

**Solutions**:

1. **Use proper type expressions**:
```csharp
// Correct
.AddField(x => x.Age) // where Age is int

// Incorrect - mixing types
.AddField(x => x.Age.ToString()) // This creates expression issues
```

2. **Handle nullable types properly**:
```csharp
.AddField(x => x.OptionalDate) // where OptionalDate is DateTime?
    .WithValidator(value => value == null || value > DateTime.MinValue, "Invalid date")
```

### Build Errors

**Problem**: Compilation errors related to expression trees or generic constraints.

**Solutions**:

1. **Ensure model properties are public**:
```csharp
public class MyModel
{
    public string Name { get; set; } = ""; // Must be public
    private string _hiddenField; // Won't work in expressions
}
```

2. **Check generic constraints**:
```csharp
// FormBuilder requires 'new()' constraint
public class MyModel // Add parameterless constructor if needed
{
    public MyModel() { }
}
```

### Performance Issues

**Problem**: Form rendering is slow with many fields.

**Solutions**:

1. **Use field dependencies wisely**:
```csharp
// Avoid complex dependency chains
.DependsOn(x => x.Field1, (model, value) => {
    // Keep logic simple and fast
})
```

2. **Optimize validation**:
```csharp
// Use async validation for expensive operations
public class ExpensiveValidator : IFieldValidator<MyModel, string>
{
    public async Task<ValidationResult> ValidateAsync(MyModel model, string value, IServiceProvider services)
    {
        // Use caching, debouncing, or other optimization techniques
        return await SomeExpensiveValidation(value);
    }
}
```

3. **Consider virtualization for large forms**:
```csharp
// Group fields and render only visible sections
.AddField(x => x.Section1Field)
    .WithGroup("Section 1")
    .VisibleWhen(model => model.CurrentSection == 1)
```

## Debugging Tips

### Enable Detailed Logging

```csharp
// In Program.cs
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

### Inspect Field Configuration

```csharp
// Add debugging information
protected override void OnParametersSet()
{
    Console.WriteLine($"Form has {Configuration.Fields.Count} fields");
    foreach (var field in Configuration.Fields)
    {
        Console.WriteLine($"Field: {field.FieldName}, Type: {field.GetType()}");
    }
}
```

### Browser Developer Tools

1. **Check Console for JavaScript errors**
2. **Inspect Network tab for failed requests**
3. **Use React Developer Tools** (if applicable)
4. **Monitor DOM changes** during field updates

## Error Messages Reference

### Common Error Patterns

| Error Message | Cause | Solution |
|---------------|-------|----------|
| `Cannot resolve symbol 'WithX'` | Missing using directive | Add `@using DynamicFormBlazor.Forms.Extensions` |
| `Object reference not set` | Null model or configuration | Initialize model: `Model = new MyModel()` |
| `InvalidOperationException: Sequence contains no elements` | Empty field collection | Ensure `Build()` is called after adding fields |
| `ArgumentException: Expression must be a member access` | Invalid field expression | Use `x => x.PropertyName` format |
| `NotSupportedException: Field type not supported` | Missing field renderer | Add custom renderer or use supported types |

## Best Practices

### Model Design

```csharp
public class WellDesignedModel
{
    // Use descriptive names
    public string CustomerName { get; set; } = "";
    
    // Initialize collections
    public List<string> Tags { get; set; } = new();
    
    // Use appropriate nullability
    public DateTime? OptionalDate { get; set; }
    
    // Add validation attributes as backup
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";
}
```

### Form Configuration

```csharp
// Keep configuration readable
var config = FormBuilder<MyModel>
    .Create()
    // Group related fields
    .AddRequiredTextField(x => x.FirstName, "First Name")
    .AddRequiredTextField(x => x.LastName, "Last Name")
    // Add clear labels and help text
    .AddEmailField(x => x.Email)
        .WithHelpText("We'll never share your email")
    // Use meaningful validation messages
    .AddField(x => x.Password)
        .Required("Password is required for security")
        .WithMinLength(8, "Password must be at least 8 characters")
    .Build();
```

### Error Handling

```csharp
private async Task HandleValidSubmit(MyModel model)
{
    try
    {
        await SaveModel(model);
        ShowSuccessMessage();
    }
    catch (ValidationException ex)
    {
        // Handle validation errors
        ShowValidationErrors(ex.Errors);
    }
    catch (Exception ex)
    {
        // Handle general errors
        ShowErrorMessage("An unexpected error occurred. Please try again.");
        Logger.LogError(ex, "Form submission failed");
    }
}
```

## Getting Help

If you're still experiencing issues:

1. **Check the [Examples](/docs/examples)** for similar use cases
2. **Review the [API Reference](/docs/api-reference)** for correct usage
3. **Search existing issues** on GitHub
4. **Create a minimal reproduction** of your problem
5. **Submit an issue** with detailed information:
   - Blazor version
   - MudBlazor version
   - Complete error message
   - Minimal code example
   - Steps to reproduce