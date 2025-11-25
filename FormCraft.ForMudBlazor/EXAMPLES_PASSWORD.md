# Password Field Examples

This document provides examples of how to use password fields with MudBlazor in FormCraft.

## Basic Password Field

The simplest way to create a password field is using `.WithInputType("password")`:

```csharp
var form = FormBuilder<LoginFormModel>
    .Create()
    .AddField(x => x.Password, field => field
        .WithLabel("Password")
        .WithInputType("password")
        .Required("Password is required"))
    .Build();
```

## Password Field with Visibility Toggle

Use the `.AsPassword()` extension method for a password field with a built-in show/hide toggle icon:

```csharp
var form = FormBuilder<LoginFormModel>
    .Create()
    .AddField(x => x.Password, field => field
        .WithLabel("Password")
        .AsPassword(enableVisibilityToggle: true)  // Adds eye icon to toggle visibility
        .Required("Password is required")
        .WithMinLength(8, "Password must be at least 8 characters"))
    .Build();
```

### How It Works

When `AsPassword(enableVisibilityToggle: true)` is called:
1. Sets the input type to "password"
2. Adds a visibility toggle icon (eye icon) at the end of the field
3. Clicking the icon toggles between showing the password as text and hiding it with dots/asterisks
4. The icon changes from "Visibility" to "VisibilityOff" when toggled

## Advanced: Custom Adornment

You can add custom adornments (icons) to any text field:

```csharp
var form = FormBuilder<UserModel>
    .Create()
    .AddField(x => x.Email, field => field
        .WithLabel("Email")
        .WithInputType("email")
        .WithAdornment(
            Icons.Material.Filled.Email,
            position: MudBlazor.Adornment.Start,
            color: MudBlazor.Color.Primary))
    .Build();
```

## Complete Login Form Example

```csharp
public class LoginFormModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

var loginForm = FormBuilder<LoginFormModel>
    .Create()
    .AddEmailField(x => x.Email,
        label: "Email",
        placeholder: "Enter your email")
    .AddField(x => x.Password, field => field
        .WithLabel("Password")
        .WithPlaceholder("Enter your password")
        .AsPassword(enableVisibilityToggle: true)  // Password with toggle
        .Required("Password is required")
        .WithMinLength(8, "Password must be at least 8 characters"))
    .Build();
```

## Password Field Without Toggle

If you want a password field without the visibility toggle:

```csharp
.AddField(x => x.Password, field => field
    .WithLabel("Password")
    .AsPassword(enableVisibilityToggle: false))  // No toggle icon
```

Or simply:

```csharp
.AddField(x => x.Password, field => field
    .WithLabel("Password")
    .WithInputType("password"))  // Basic password field
```

## Registration Form with Password Confirmation

```csharp
public class RegisterFormModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

var registerForm = FormBuilder<RegisterFormModel>
    .Create()
    .AddEmailField(x => x.Email,
        label: "Email",
        placeholder: "your.email@example.com")
    .AddField(x => x.Password, field => field
        .WithLabel("Password")
        .WithPlaceholder("Enter a strong password")
        .AsPassword(enableVisibilityToggle: true)
        .Required("Password is required")
        .WithMinLength(8, "Must be at least 8 characters")
        .WithValidator(new PasswordStrengthValidator<RegisterFormModel>()))
    .AddField(x => x.ConfirmPassword, field => field
        .WithLabel("Confirm Password")
        .WithPlaceholder("Re-enter your password")
        .AsPassword(enableVisibilityToggle: true)
        .Required("Please confirm your password")
        .WithValidator(new PasswordMatchValidator<RegisterFormModel>()))
    .Build();
```

## Custom Adornment Colors and Icons

```csharp
.AddField(x => x.ApiKey, field => field
    .WithLabel("API Key")
    .AsPassword(enableVisibilityToggle: true)
    .WithAdornment(
        Icons.Material.Filled.Key,
        position: MudBlazor.Adornment.Start,
        color: MudBlazor.Color.Secondary))
```

## Available MudBlazor Icons for Adornments

Common icons you can use:
- `Icons.Material.Filled.Visibility` - Eye icon (show)
- `Icons.Material.Filled.VisibilityOff` - Eye with slash (hide)
- `Icons.Material.Filled.Lock` - Lock icon
- `Icons.Material.Filled.LockOpen` - Open lock
- `Icons.Material.Filled.Key` - Key icon
- `Icons.Material.Filled.Security` - Shield icon
- `Icons.Material.Filled.Email` - Email icon
- `Icons.Material.Filled.Person` - User icon
- `Icons.Material.Filled.Phone` - Phone icon

## Technical Details

### How InputType is Resolved

The component reads `InputType` in this order of priority:
1. `field.InputType` property (set via `.WithInputType()`)
2. `AdditionalAttributes["InputType"]` (set via `.WithAttribute()`)
3. Default value: "text"

### Supported InputType Values

The following input types are automatically converted to MudBlazor's `InputType` enum:
- `"email"` → `MudBlazor.InputType.Email`
- `"password"` → `MudBlazor.InputType.Password`
- `"tel"` or `"telephone"` → `MudBlazor.InputType.Telephone`
- `"url"` → `MudBlazor.InputType.Url`
- `"search"` → `MudBlazor.InputType.Search`
- All others → `MudBlazor.InputType.Text`