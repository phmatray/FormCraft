namespace FormCraft;

/// <summary>
/// Service for managing CSRF tokens.
/// </summary>
public interface ICsrfTokenService
{
    /// <summary>
    /// Generates a new CSRF token.
    /// </summary>
    /// <returns>A new CSRF token.</returns>
    Task<string> GenerateTokenAsync();

    /// <summary>
    /// Validates a CSRF token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>True if the token is valid, false otherwise.</returns>
    Task<bool> ValidateTokenAsync(string token);
}