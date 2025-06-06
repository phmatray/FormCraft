namespace FormCraft;

/// <summary>
/// Represents the result of a field validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; }
    
    /// <summary>
    /// Gets the error message if validation failed, or null if validation was successful.
    /// </summary>
    public string? ErrorMessage { get; }
    
    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }
    
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A ValidationResult indicating successful validation.</returns>
    public static ValidationResult Success() => new(true);
    
    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why validation failed.</param>
    /// <returns>A ValidationResult indicating failed validation with the specified error message.</returns>
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}