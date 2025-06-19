# FluentValidation Integration

FormCraft provides seamless integration with FluentValidation, allowing you to leverage your existing validation rules and validators within dynamic forms.

## Overview

While FormCraft includes its own validation system, it also supports FluentValidation through adapters that bridge the two systems. This allows you to:

- Reuse existing FluentValidation validators
- Leverage FluentValidation's extensive rule set
- Maintain consistency with your domain validation logic
- Combine FormCraft's built-in validators with FluentValidation rules

## Basic Usage

### Using Registered Validators

The simplest way to use FluentValidation is to register your validators with dependency injection and reference them in your form configuration:

```csharp
// 1. Define your FluentValidation validator
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(customer => customer.Name)
            .NotEmpty().WithMessage("Customer name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters");
            
        RuleFor(customer => customer.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
            
        RuleFor(customer => customer.Address)
            .SetValidator(new AddressValidator());
    }
}

// 2. Register the validator in DI
services.AddScoped<IValidator<Customer>, CustomerValidator>();

// 3. Use in FormCraft
var formConfig = FormBuilder<Customer>
    .Create()
    .AddField(x => x.Name, field => field
        .WithLabel("Customer Name")
        .WithFluentValidation(x => x.Name))
    .AddField(x => x.Email, field => field
        .WithLabel("Email Address")
        .WithFluentValidation(x => x.Email))
    .Build();
```

### Using Specific Validator Instances

You can also provide a specific validator instance directly:

```csharp
var customerValidator = new CustomerValidator();

var formConfig = FormBuilder<Customer>
    .Create()
    .AddField(x => x.Name, field => field
        .WithLabel("Customer Name")
        .WithFluentValidator(customerValidator, x => x.Name))
    .Build();
```

## Combining Validators

FormCraft allows you to combine its built-in validators with FluentValidation:

```csharp
var formConfig = FormBuilder<Customer>
    .Create()
    .AddField(x => x.Name, field => field
        .WithLabel("Customer Name")
        .Required("This field is required") // FormCraft validator
        .WithFluentValidation(x => x.Name) // FluentValidation rules
        .WithMaxLength(50, "Maximum 50 characters")) // FormCraft validator
    .Build();
```

## Complex Scenarios

### Nested Objects with Validators

FluentValidation's `SetValidator` for nested objects works seamlessly:

```csharp
public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.PostalCode).Matches(@"^\d{5}$");
    }
}

public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Address).SetValidator(new AddressValidator());
    }
}

// FormCraft will automatically handle nested validation
var formConfig = FormBuilder<Customer>
    .Create()
    .AddField(x => x.Address.Street, field => field
        .WithLabel("Street")
        .WithFluentValidation(x => x.Address.Street))
    .Build();
```

### Conditional Validation

FluentValidation's conditional rules are fully supported:

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.DeliveryDate)
            .NotEmpty()
            .When(x => x.IsExpressDelivery);
            
        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.SpecialInstructions));
    }
}
```

### Async Validation

FluentValidation's async validators work with FormCraft:

```csharp
public class UserValidator : AbstractValidator<User>
{
    private readonly IUserService _userService;
    
    public UserValidator(IUserService userService)
    {
        _userService = userService;
        
        RuleFor(x => x.Email)
            .MustAsync(async (email, cancellation) => 
            {
                return await _userService.IsEmailUniqueAsync(email);
            })
            .WithMessage("Email already exists");
    }
}
```

## Integration Details

### How It Works

FormCraft's FluentValidation integration works through the `FluentValidationAdapter` class, which:

1. Implements FormCraft's `IFieldValidator<TModel, TProperty>` interface
2. Retrieves the FluentValidation validator from DI or uses a provided instance
3. Executes validation and maps results to FormCraft's validation system
4. Filters validation results to only show errors for the specific field

### Validation Timing

- Field-level validation occurs on field change (blur)
- Form-level validation occurs on form submission
- All FluentValidation rules for a field are executed together

### Error Messages

- Error messages from FluentValidation are preserved exactly as defined
- Only the first error message for a field is displayed by default
- Custom error message formatting can be applied through FluentValidation

## Best Practices

### 1. Organize Validators

Keep your validators organized and reusable:

```csharp
// Separate validators by concern
public class CustomerValidators
{
    public class Create : AbstractValidator<Customer> { }
    public class Update : AbstractValidator<Customer> { }
}
```

### 2. Register Validators Properly

Ensure validators are registered with the correct lifetime:

```csharp
// For stateless validators
services.AddSingleton<IValidator<Customer>, CustomerValidator>();

// For validators with dependencies
services.AddScoped<IValidator<User>, UserValidator>();
```

### 3. Combine Validation Approaches

Use FluentValidation for complex business rules and FormCraft's built-in validators for simple field-level validation:

```csharp
.AddField(x => x.Email, field => field
    .Required() // Simple presence validation
    .WithEmailFormat() // Basic format validation
    .WithFluentValidation(x => x.Email)) // Complex business rules
```

### 4. Handle Validation State

Remember that validation state is managed by Blazor's `EditContext`:

```razor
<EditForm Model="@model">
    <DynamicFormValidator TModel="Customer" Configuration="@formConfig" />
    <FormCraftComponent TModel="Customer" Model="@model" Configuration="@formConfig" />
</EditForm>
```

## Troubleshooting

### Validator Not Found

If validation is not working, ensure:
- The validator is registered in DI
- The correct property expression is used
- The validator is accessible from the current service scope

### Validation Not Triggering

Check that:
- `DynamicFormValidator` is included in your form
- The field has `WithFluentValidation` applied
- The validator rules match the property being validated

### Performance Considerations

For optimal performance:
- Register stateless validators as singletons
- Avoid heavy operations in validators
- Consider caching validation results for expensive async operations