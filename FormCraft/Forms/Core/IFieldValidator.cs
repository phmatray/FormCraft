namespace FormCraft.Forms.Core;

/// <summary>
/// Defines the contract for field validators that can validate individual form field values.
/// </summary>
/// <typeparam name="TModel">The model type that contains the field being validated.</typeparam>
/// <typeparam name="TValue">The type of the field value being validated.</typeparam>
/// <example>
/// <code>
/// public class EmailValidator : IFieldValidator&lt;MyModel, string&gt;
/// {
///     public string? ErrorMessage { get; set; } = "Please enter a valid email address";
///     
///     public async Task&lt;ValidationResult&gt; ValidateAsync(MyModel model, string value, IServiceProvider services)
///     {
///         if (string.IsNullOrEmpty(value) || !value.Contains("@"))
///             return ValidationResult.Failure(ErrorMessage ?? "Invalid email");
///         
///         return ValidationResult.Success();
///     }
/// }
/// </code>
/// </example>
public interface IFieldValidator<TModel, TValue>
{
    /// <summary>
    /// Gets or sets the error message to display when validation fails.
    /// </summary>
    string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Validates the field value asynchronously and returns the validation result.
    /// </summary>
    /// <param name="model">The complete model instance containing the field being validated.</param>
    /// <param name="value">The current value of the field being validated.</param>
    /// <param name="services">The service provider for dependency injection if needed.</param>
    /// <returns>A ValidationResult indicating whether validation passed or failed with an error message.</returns>
    Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services);
}

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