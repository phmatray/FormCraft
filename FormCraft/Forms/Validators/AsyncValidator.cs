namespace FormCraft;

/// <summary>
/// A flexible validator that allows custom validation logic using an asynchronous function.
/// This is useful for validation that requires external services, database calls, or API requests.
/// </summary>
/// <typeparam name="TModel">The model type that contains the field being validated.</typeparam>
/// <typeparam name="TValue">The type of the field value being validated.</typeparam>
/// <example>
/// <code>
/// // Using with async validation (e.g., checking username availability)
/// .AddField(x => x.Username)
///     .WithValidator(new AsyncValidator&lt;MyModel, string&gt;(
///         async value => await userService.IsUsernameAvailableAsync(value),
///         "Username is already taken"));
/// 
/// // Using with async API validation
/// .AddField(x => x.ZipCode)
///     .WithValidator(new AsyncValidator&lt;MyModel, string&gt;(
///         async value => await addressService.ValidateZipCodeAsync(value),
///         "Invalid zip code"));
/// </code>
/// </example>
public class AsyncValidator<TModel, TValue> : IFieldValidator<TModel, TValue>
{
    private readonly Func<TValue, Task<bool>> _validation;

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the AsyncValidator class.
    /// </summary>
    /// <param name="validation">An async function that takes the field value and returns true if valid, false if invalid.</param>
    /// <param name="errorMessage">The error message to display when validation fails.</param>
    public AsyncValidator(Func<TValue, Task<bool>> validation, string errorMessage)
    {
        _validation = validation;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Validates the field value asynchronously using the custom validation function.
    /// </summary>
    /// <param name="model">The complete model instance containing the field being validated.</param>
    /// <param name="value">The current value of the field being validated.</param>
    /// <param name="services">The service provider for dependency injection that can be used within the validation function.</param>
    /// <returns>A ValidationResult indicating success if the async function returns true, or failure with the error message if it returns false.</returns>
    public async Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services)
    {
        try
        {
            var isValid = await _validation(value);
            return isValid
                ? ValidationResult.Success()
                : ValidationResult.Failure(ErrorMessage!);
        }
        catch
        {
            return ValidationResult.Failure(ErrorMessage!);
        }
    }
}