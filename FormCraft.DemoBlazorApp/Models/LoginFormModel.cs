namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Model for login form demonstration.
/// </summary>
public class LoginFormModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

/// <summary>
/// Model for registration form with password confirmation.
/// </summary>
public class RegisterFormModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool AcceptTerms { get; set; }
}

/// <summary>
/// Model for advanced password features demonstration.
/// </summary>
public class SecurityFormModel
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretToken { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}