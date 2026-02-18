# Migration Guide

This guide helps you migrate from traditional Blazor forms or other form libraries to FormCraft.

## Migrating from EditForm

### Before: Traditional EditForm

```razor
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="form-group">
        <label for="name">Name</label>
        <InputText id="name" @bind-Value="model.Name" class="form-control" />
        <ValidationMessage For="@(() => model.Name)" />
    </div>

    <div class="form-group">
        <label for="email">Email</label>
        <InputText id="email" @bind-Value="model.Email" class="form-control" />
        <ValidationMessage For="@(() => model.Email)" />
    </div>

    <div class="form-group">
        <label for="age">Age</label>
        <InputNumber id="age" @bind-Value="model.Age" class="form-control" />
        <ValidationMessage For="@(() => model.Age)" />
    </div>

    <button type="submit" class="btn btn-primary">Submit</button>
</EditForm>

@code {
    private MyModel model = new();

    private async Task HandleSubmit()
    {
        await SaveData(model);
    }
}
```

### After: FormCraft

```razor
<FormCraftComponent TModel="MyModel"
                   Model="@model"
                   Configuration="@config"
                   OnValidSubmit="@HandleSubmit" />

@code {
    private MyModel model = new();
    private IFormConfiguration<MyModel> config = null!;

    protected override void OnInitialized()
    {
        config = FormBuilder<MyModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .Required("Name is required"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .Required("Email is required")
                .WithEmailValidation())
            .AddField(x => x.Age, field => field
                .WithLabel("Age")
                .Required("Age is required")
                .WithRange(18, 100, "Age must be between 18 and 100"))
            .Build();
    }

    private async Task HandleSubmit(MyModel model)
    {
        await SaveData(model);
    }
}
```

## Migrating from DataAnnotations

### Before: Model with DataAnnotations

```csharp
public class UserModel
{
    [Required(ErrorMessage = "Name is required")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Range(18, 100, ErrorMessage = "Age must be between 18 and 100")]
    public int Age { get; set; }
}
```

### After: FormCraft with FluentValidation

**Option 1: Inline validation with FormBuilder**

```csharp
config = FormBuilder<UserModel>
    .Create()
    .AddField(x => x.Name, field => field
        .WithLabel("Name")
        .Required("Name is required")
        .WithMinLength(2, "Name must be at least 2 characters"))
    .AddField(x => x.Email, field => field
        .WithLabel("Email")
        .Required("Email is required")
        .WithEmailValidation("Invalid email format"))
    .AddField(x => x.Age, field => field
        .WithLabel("Age")
        .WithRange(18, 100, "Age must be between 18 and 100"))
    .Build();
```

**Option 2: FluentValidation validator class**

```csharp
// Create validator
public class UserModelValidator : AbstractValidator<UserModel>
{
    public UserModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 100).WithMessage("Age must be between 18 and 100");
    }
}

// Register in DI
services.AddScoped<IValidator<UserModel>, UserModelValidator>();

// Use with FormCraft
config = FormBuilder<UserModel>
    .Create()
    .AddField(x => x.Name, field => field
        .WithLabel("Name")
        .WithFluentValidation(x => x.Name))
    .AddField(x => x.Email, field => field
        .WithLabel("Email")
        .WithFluentValidation(x => x.Email))
    .AddField(x => x.Age, field => field
        .WithLabel("Age")
        .WithFluentValidation(x => x.Age))
    .Build();
```

## DataAnnotation to FormCraft Mapping

| DataAnnotation | FormCraft Method |
|---------------|------------------|
| `[Required]` | `.Required("message")` |
| `[MinLength]` | `.WithMinLength(n, "message")` |
| `[MaxLength]` | `.WithMaxLength(n, "message")` |
| `[StringLength]` | `.WithMinLength(min).WithMaxLength(max)` |
| `[Range]` | `.WithRange(min, max, "message")` |
| `[EmailAddress]` | `.WithEmailValidation("message")` |
| `[RegularExpression]` | `.WithValidator(v => Regex.IsMatch(v, pattern), "message")` |
| `[Compare]` | Use FluentValidation |
| Custom validation | `.WithValidator(predicate, "message")` or `.WithAsyncValidator(func, "message")` |

## Migrating Conditional Fields

### Before: Manual visibility control

```razor
<div class="form-group" style="@(model.ShowCity ? "" : "display:none")">
    <label for="city">City</label>
    <InputText id="city" @bind-Value="model.City" class="form-control" />
</div>

@code {
    private void OnCountryChanged()
    {
        model.ShowCity = !string.IsNullOrEmpty(model.Country);
        model.City = ""; // Reset city when country changes
        StateHasChanged();
    }
}
```

### After: FormCraft with DependsOn and VisibleWhen

```csharp
config = FormBuilder<MyModel>
    .Create()
    .AddField(x => x.Country, field => field
        .WithLabel("Country")
        .WithSelectOptions([
            new("US", "United States"),
            new("CA", "Canada"),
            new("UK", "United Kingdom")
        ]))
    .AddField(x => x.City, field => field
        .WithLabel("City")
        .VisibleWhen(m => !string.IsNullOrEmpty(m.Country))
        .DependsOn(x => x.Country, (model, country) =>
        {
            model.City = ""; // Reset city when country changes
        }))
    .Build();
```

## Migrating Select/Dropdown Fields

### Before: Manual select with foreach

```razor
<select @bind="model.Country" class="form-control">
    <option value="">Select a country</option>
    @foreach (var country in countries)
    {
        <option value="@country.Code">@country.Name</option>
    }
</select>
```

### After: FormCraft with WithSelectOptions

```csharp
.AddField(x => x.Country, field => field
    .WithLabel("Country")
    .WithPlaceholder("Select a country")
    .WithSelectOptions([
        new("US", "United States"),
        new("CA", "Canada"),
        new("UK", "United Kingdom"),
        new("DE", "Germany")
    ])
    .Required("Please select a country"))
```

Or use the convenience method:

```csharp
.AddDropdownField(x => x.Country, "Country",
    ("US", "United States"),
    ("CA", "Canada"),
    ("UK", "United Kingdom"),
    ("DE", "Germany"))
```

## Migrating Complex Forms

### Step-by-Step Migration Process

1. **Identify all fields** in your existing form
2. **Map validation rules** to FormCraft equivalents
3. **Identify dependencies** between fields
4. **Create FormBuilder configuration**
5. **Replace EditForm** with FormCraftComponent
6. **Test all scenarios**

### Example: Complex Registration Form

**Before:**

```razor
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />

    <div class="row">
        <div class="col-md-6">
            <label>First Name</label>
            <InputText @bind-Value="model.FirstName" />
            <ValidationMessage For="@(() => model.FirstName)" />
        </div>
        <div class="col-md-6">
            <label>Last Name</label>
            <InputText @bind-Value="model.LastName" />
            <ValidationMessage For="@(() => model.LastName)" />
        </div>
    </div>

    <div class="form-group">
        <label>Email</label>
        <InputText @bind-Value="model.Email" type="email" />
        <ValidationMessage For="@(() => model.Email)" />
    </div>

    <div class="form-group">
        <label>Password</label>
        <InputText @bind-Value="model.Password" type="password" />
        <ValidationMessage For="@(() => model.Password)" />
    </div>

    <div class="form-group">
        <label>Confirm Password</label>
        <InputText @bind-Value="model.ConfirmPassword" type="password" />
        <ValidationMessage For="@(() => model.ConfirmPassword)" />
    </div>

    <div class="form-group">
        <InputCheckbox @bind-Value="model.AcceptTerms" />
        <label>I accept the terms and conditions</label>
        <ValidationMessage For="@(() => model.AcceptTerms)" />
    </div>

    <button type="submit">Register</button>
</EditForm>
```

**After:**

```csharp
config = FormBuilder<RegistrationModel>
    .Create()
    .AddFieldGroup(group => group
        .WithGroupName("Personal Information")
        .WithColumns(2)
        .AddField(x => x.FirstName, field => field
            .WithLabel("First Name")
            .Required("First name is required")
            .WithMinLength(2))
        .AddField(x => x.LastName, field => field
            .WithLabel("Last Name")
            .Required("Last name is required")
            .WithMinLength(2)))
    .AddField(x => x.Email, field => field
        .WithLabel("Email")
        .Required("Email is required")
        .WithEmailValidation())
    .AddField(x => x.Password, field => field
        .WithLabel("Password")
        .WithInputType("password")
        .Required("Password is required")
        .WithMinLength(8, "Password must be at least 8 characters"))
    .AddField(x => x.ConfirmPassword, field => field
        .WithLabel("Confirm Password")
        .WithInputType("password")
        .Required("Please confirm your password"))
    // For password matching, use FluentValidation
    .AddField(x => x.AcceptTerms, field => field
        .WithLabel("I accept the terms and conditions")
        .WithValidator(v => v, "You must accept the terms"))
    .Build();
```

## Common Migration Issues

### Issue 1: Missing Required Attribute on HTML

**Problem:** FormCraft doesn't add HTML5 `required` attribute.

**Solution:** This is intentional. FormCraft handles validation server-side for better UX. The form has `novalidate` attribute to disable browser validation.

### Issue 2: Validation Timing

**Problem:** Validation runs differently than with DataAnnotations.

**Solution:** FormCraft validates on blur and submit. Use `OnFieldChanged` callback if you need immediate validation feedback.

### Issue 3: Model Binding

**Problem:** Model values not updating as expected.

**Solution:** Ensure you're using `@bind-Value` pattern internally. FormCraft handles this automatically, but check that your model properties have public setters.

### Issue 4: Custom Validators

**Problem:** Complex validation logic needs migration.

**Solution:** Use `WithValidator` for sync validation or `WithAsyncValidator` for async:

```csharp
// Sync
.WithValidator(value => value.Length >= 5, "Must be at least 5 characters")

// Async
.WithAsyncValidator(async value => {
    await Task.Delay(100); // Simulate API call
    return value != "taken";
}, "This value is already taken")
```

## Checklist

- [ ] Install FormCraft NuGet packages
- [ ] Register FormCraft services: `services.AddFormCraft()`
- [ ] Create FormBuilder configuration for each form
- [ ] Migrate validation rules
- [ ] Migrate conditional visibility logic
- [ ] Migrate field dependencies
- [ ] Replace EditForm with FormCraftComponent
- [ ] Test all validation scenarios
- [ ] Test form submission
- [ ] Verify model binding works correctly
