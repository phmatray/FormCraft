namespace FormCraft;

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