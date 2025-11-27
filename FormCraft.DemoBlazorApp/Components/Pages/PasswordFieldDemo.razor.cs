using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class PasswordFieldDemo
{
    // Login Form State
    private LoginFormModel _loginModel = new();
    private bool _loginSubmitted;
    private bool _loginSubmitting;
    private IFormConfiguration<LoginFormModel> _loginFormConfiguration = null!;

    // Register Form State
    private RegisterFormModel _registerModel = new();
    private bool _registerSubmitted;
    private bool _registerSubmitting;
    private IFormConfiguration<RegisterFormModel> _registerFormConfiguration = null!;
    private int _passwordStrengthScore;

    // Security Form State
    private SecurityFormModel _securityModel = new();
    private bool _securitySubmitted;
    private bool _securitySubmitting;
    private IFormConfiguration<SecurityFormModel> _securityFormConfiguration = null!;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "password-demo",
        Title = "Password Fields & Adornments",
        Description = "Comprehensive demonstration of password fields with visibility toggle, custom adornments, and secure input handling using MudBlazor components. Learn how to implement secure password inputs with various configurations.",
        Icon = Icons.Material.Filled.Lock,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Lock, Color = Color.Primary, Text = "Basic password masking with WithInputType(\"password\")" },
            new() { Icon = Icons.Material.Filled.Visibility, Color = Color.Secondary, Text = "Password visibility toggle with AsPassword() method" },
            new() { Icon = Icons.Material.Filled.Shield, Color = Color.Warning, Text = "Real-time password strength indicators" },
            new() { Icon = Icons.Material.Filled.VpnKey, Color = Color.Tertiary, Text = "Custom adornments for security credentials" },
            new() { Icon = Icons.Material.Filled.Security, Color = Color.Success, Text = "Password confirmation validation patterns" },
            new() { Icon = Icons.Material.Filled.Info, Color = Color.Info, Text = "Flexible adornment positioning and styling" }
        ],
        ApiGuidelines =
        [
            new() { Feature = ".WithInputType(\"password\")", Usage = "Sets input type to password (basic masking)", Example = ".AddField(x => x.Password).WithInputType(\"password\")" },
            new() { Feature = ".AsPassword()", Usage = "Creates password field with visibility toggle", Example = ".AddField(x => x.Password).AsPassword(enableVisibilityToggle: true)" },
            new() { Feature = ".WithAdornment(icon, position)", Usage = "Adds icon to start or end of field", Example = ".WithAdornment(Icons.Material.Filled.Lock, Adornment.Start)" },
            new() { Feature = "EnablePasswordToggle attribute", Usage = "Adds eye icon to show/hide password", Example = ".WithAttribute(\"EnablePasswordToggle\", true)" },
            new() { Feature = "Custom Adornment Colors", Usage = "Customize adornment icon color", Example = ".WithAdornment(icon, Adornment.Start, Color.Primary)" }
        ],
        CodeExamples =
        [
            new() { Title = "Basic Login Form Configuration", Language = "csharp", CodeProvider = GetLoginCodeStatic },
            new() { Title = "Registration with Password Toggle", Language = "csharp", CodeProvider = GetRegisterCodeStatic },
            new() { Title = "Security Credentials with Custom Adornments", Language = "csharp", CodeProvider = GetSecurityCodeStatic }
        ],
        WhenToUse = "Use password fields for sensitive information like passwords, API keys, tokens, and PINs. The AsPassword() method is ideal for user-facing password inputs where visibility toggle enhances UX. For credentials that should remain hidden (like API keys), use basic password masking with custom adornments. Always combine with proper validation and strength indicators for user registration flows.",
        CommonPitfalls =
        [
            "Don't combine AsPassword() visibility toggle with custom adornments - they conflict",
            "Remember to add password confirmation fields for registration/change password forms",
            "Avoid overly strict password requirements that frustrate users",
            "Always mask sensitive credentials in success displays and logs"
        ],
        RelatedDemoIds = ["fluent", "validation", "field-dependencies"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    private List<FormGuidelines.GuidelineItem> _loginFeatures => Documentation.FeatureHighlights
        .Take(3)
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    private List<FormGuidelines.GuidelineItem> _registerFeatures => Documentation.FeatureHighlights
        .Skip(1)
        .Take(4)
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    private List<FormGuidelines.GuidelineItem> _securityFeatures => Documentation.FeatureHighlights
        .Skip(3)
        .Concat([new() { Icon = Icons.Material.Filled.Info, Color = Color.Info, Text = "Note: Can't combine toggle with custom adornments" }])
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        InitializeLoginForm();
        InitializeRegisterForm();
        InitializeSecurityForm();
    }

    private void InitializeLoginForm()
    {
        _loginFormConfiguration = FormBuilder<LoginFormModel>
            .Create()
            .AddEmailField(x => x.Email,
                label: "Email",
                placeholder: "Enter your email")
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithPlaceholder("Enter your password")
                .WithInputType("password")  // Basic password masking
                .Required("Password is required"))
            .AddField(x => x.RememberMe, field => field
                .WithLabel("Remember Me")
                .WithHelpText("Stay logged in on this device"))
            .Build();
    }

    private void InitializeRegisterForm()
    {
        _registerFormConfiguration = FormBuilder<RegisterFormModel>
            .Create()
            .AddField(x => x.Username, field => field
                .WithLabel("Username")
                .WithPlaceholder("Choose a username")
                .WithAdornment(Icons.Material.Filled.Person, Adornment.Start)
                .Required("Username is required")
                .WithMinLength(3, "Username must be at least 3 characters"))
            .AddEmailField(x => x.Email,
                label: "Email Address",
                placeholder: "your.email@example.com")
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithPlaceholder("Create a strong password")
                .AsPassword(enableVisibilityToggle: true)  // Password with visibility toggle!
                .Required("Password is required")
                .WithMinLength(8, "Password must be at least 8 characters")
                .WithHelpText("Use at least 8 characters with letters, numbers, and symbols"))
            .AddField(x => x.ConfirmPassword, field => field
                .WithLabel("Confirm Password")
                .WithPlaceholder("Re-enter your password")
                .AsPassword(enableVisibilityToggle: true)
                .Required("Please confirm your password")
                .WithHelpText("Must match the password above"))
            .AddField(x => x.AcceptTerms, field => field
                .WithLabel("I accept the terms and conditions")
                .Required("You must accept the terms to register"))
            .Build();
    }

    private void InitializeSecurityForm()
    {
        _securityFormConfiguration = FormBuilder<SecurityFormModel>
            .Create()
            .AddField(x => x.ApiKey, field => field
                .WithLabel("API Key")
                .WithPlaceholder("Enter your API key")
                .AsPassword(enableVisibilityToggle: true)  // Password toggle at end
                .Required("API Key is required")
                .WithHelpText("Your secret API key from the dashboard"))
            .AddField(x => x.SecretToken, field => field
                .WithLabel("Secret Token")
                .WithPlaceholder("Enter your secret token")
                .WithInputType("password")  // Basic password without toggle
                .WithAdornment(
                    Icons.Material.Filled.VpnKey,
                    Adornment.Start,
                    Color.Secondary)
                .Required("Secret token is required"))
            .AddField(x => x.Pin, field => field
                .WithLabel("Security PIN")
                .WithPlaceholder("Enter 4-digit PIN")
                .WithInputType("password")  // Basic password without toggle
                .WithAdornment(
                    Icons.Material.Filled.Pin,
                    Adornment.Start,
                    Color.Tertiary)
                .WithAttribute("MaxLength", 4)
                .WithMinLength(4, "PIN must be 4 digits")
                .WithMaxLength(4, "PIN must be 4 digits")
                .Required("PIN is required"))
            .Build();
    }

    // Login Form Handlers
    private async Task HandleLoginSubmit()
    {
        _loginSubmitting = true;
        await Task.Delay(1500); // Simulate API call
        _loginSubmitted = true;
        _loginSubmitting = false;
    }

    private void ResetLoginForm()
    {
        _loginModel = new LoginFormModel();
        _loginSubmitted = false;
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetLoginDisplayItems()
    {
        return
        [
            new FormSuccessDisplay.DataDisplayItem { Label = "Email", Value = _loginModel.Email },
            new FormSuccessDisplay.DataDisplayItem { Label = "Password", Value = new string('•', _loginModel.Password.Length) },
            new FormSuccessDisplay.DataDisplayItem { Label = "Remember Me", Value = _loginModel.RememberMe ? "Yes" : "No" }
        ];
    }

    // Register Form Handlers
    private async Task HandleRegisterSubmit()
    {
        _registerSubmitting = true;
        await Task.Delay(2000); // Simulate API call
        _registerSubmitted = true;
        _registerSubmitting = false;
    }

    private void ResetRegisterForm()
    {
        _registerModel = new RegisterFormModel();
        _registerSubmitted = false;
        _passwordStrengthScore = 0;
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetRegisterDisplayItems()
    {
        return
        [
            new FormSuccessDisplay.DataDisplayItem { Label = "Username", Value = _registerModel.Username },
            new FormSuccessDisplay.DataDisplayItem { Label = "Email", Value = _registerModel.Email },
            new FormSuccessDisplay.DataDisplayItem { Label = "Password", Value = new string('•', _registerModel.Password.Length) },
            new FormSuccessDisplay.DataDisplayItem { Label = "Password Strength", Value = GetPasswordStrengthLabel() },
            new FormSuccessDisplay.DataDisplayItem { Label = "Terms Accepted", Value = _registerModel.AcceptTerms ? "Yes" : "No" }
        ];
    }

    // Security Form Handlers
    private async Task HandleSecuritySubmit()
    {
        _securitySubmitting = true;
        await Task.Delay(1500); // Simulate API call
        _securitySubmitted = true;
        _securitySubmitting = false;
    }

    private void ResetSecurityForm()
    {
        _securityModel = new SecurityFormModel();
        _securitySubmitted = false;
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetSecurityDisplayItems()
    {
        return
        [
            new FormSuccessDisplay.DataDisplayItem { Label = "API Key", Value = MaskCredential(_securityModel.ApiKey) },
            new FormSuccessDisplay.DataDisplayItem { Label = "Secret Token", Value = MaskCredential(_securityModel.SecretToken) },
            new FormSuccessDisplay.DataDisplayItem { Label = "PIN", Value = new string('•', _securityModel.Pin.Length) }
        ];
    }

    private static string MaskCredential(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length <= 8) return new string('•', value.Length);
        return $"{value[..4]}...{value[^4..]}";
    }

    // Password Strength Helpers
    private void CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            _passwordStrengthScore = 0;
            return;
        }

        int score = 0;
        if (password.Length >= 8) score += 20;
        if (password.Length >= 12) score += 10;
        if (password.Any(char.IsUpper)) score += 20;
        if (password.Any(char.IsLower)) score += 20;
        if (password.Any(char.IsDigit)) score += 20;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score += 10;

        _passwordStrengthScore = Math.Min(score, 100);
    }

    private Color GetPasswordStrengthColor()
    {
        return _passwordStrengthScore switch
        {
            < 30 => Color.Error,
            < 50 => Color.Warning,
            < 70 => Color.Info,
            _ => Color.Success
        };
    }

    private string GetPasswordStrengthLabel()
    {
        return _passwordStrengthScore switch
        {
            0 => "No password entered",
            < 30 => "Weak",
            < 50 => "Fair",
            < 70 => "Good",
            < 90 => "Strong",
            _ => "Very Strong"
        };
    }

    // Code Examples
    private string GetLoginCode() => GetLoginCodeStatic();

    private static string GetLoginCodeStatic()
    {
        return @"var loginForm = FormBuilder<LoginFormModel>
    .Create()
    .AddEmailField(x => x.Email,
        label: ""Email"",
        placeholder: ""Enter your email"")
    .AddField(x => x.Password, field => field
        .WithLabel(""Password"")
        .WithPlaceholder(""Enter your password"")
        .WithInputType(""password"")  // Basic password masking
        .Required(""Password is required""))
    .AddField(x => x.RememberMe, field => field
        .WithLabel(""Remember Me"")
        .WithHelpText(""Stay logged in on this device""))
    .Build();";
    }

    private string GetRegisterCode() => GetRegisterCodeStatic();

    private static string GetRegisterCodeStatic()
    {
        return @"var registerForm = FormBuilder<RegisterFormModel>
    .Create()
    .AddField(x => x.Username, field => field
        .WithLabel(""Username"")
        .WithAdornment(Icons.Material.Filled.Person, Adornment.Start)
        .Required(""Username is required"")
        .WithMinLength(3, ""Username must be at least 3 characters""))
    .AddEmailField(x => x.Email, label: ""Email Address"")
    .AddField(x => x.Password, field => field
        .WithLabel(""Password"")
        .AsPassword(enableVisibilityToggle: true)  // Eye icon toggle!
        .Required(""Password is required"")
        .WithMinLength(8, ""Password must be at least 8 characters"")
        .WithHelpText(""Use letters, numbers, and symbols""))
    .AddField(x => x.ConfirmPassword, field => field
        .WithLabel(""Confirm Password"")
        .AsPassword(enableVisibilityToggle: true)  // Eye icon toggle!
        .Required(""Please confirm your password""))
    .AddField(x => x.AcceptTerms, field => field
        .WithLabel(""I accept the terms and conditions"")
        .Required(""You must accept the terms to register""))
    .Build();";
    }

    private string GetSecurityCode() => GetSecurityCodeStatic();

    private static string GetSecurityCodeStatic()
    {
        return @"var securityForm = FormBuilder<SecurityFormModel>
    .Create()
    // Option 1: Password with visibility toggle (no custom adornment)
    .AddField(x => x.ApiKey, field => field
        .WithLabel(""API Key"")
        .AsPassword(enableVisibilityToggle: true)  // Eye icon at end
        .Required(""API Key is required""))

    // Option 2: Password with custom adornment (no toggle)
    .AddField(x => x.SecretToken, field => field
        .WithLabel(""Secret Token"")
        .WithInputType(""password"")               // Basic password masking
        .WithAdornment(                            // Custom icon at start
            Icons.Material.Filled.VpnKey,
            Adornment.Start,
            Color.Secondary)
        .Required(""Secret token is required""))

    // Option 3: PIN with custom adornment
    .AddField(x => x.Pin, field => field
        .WithLabel(""Security PIN"")
        .WithInputType(""password"")
        .WithAdornment(
            Icons.Material.Filled.Pin,
            Adornment.Start,
            Color.Tertiary)
        .WithAttribute(""MaxLength"", 4)
        .WithMinLength(4, ""PIN must be 4 digits"")
        .Required(""PIN is required""))
    .Build();";
    }
}