namespace FormCraft;

/// <summary>
/// A flexible validator that allows custom validation logic using a synchronous function.
/// </summary>
/// <typeparam name="TModel">The model type that contains the field being validated.</typeparam>
/// <typeparam name="TValue">The type of the field value being validated.</typeparam>
/// <example>
/// <code>
/// // Using with a lambda expression
/// .AddField(x => x.Username)
///     .WithValidator(new CustomValidator&lt;MyModel, string&gt;(
///         value => value.Length >= 3 && !value.Contains(" "),
///         "Username must be at least 3 characters and contain no spaces"));
/// 
/// // Using with a method reference
/// .AddField(x => x.Email)
///     .WithValidator(new CustomValidator&lt;MyModel, string&gt;(
///         IsValidEmail,
///         "Please enter a valid email address"));
/// </code>
/// </example>
public class CustomValidator<TModel, TValue> : IFieldValidator<TModel, TValue>
{
    private readonly Func<TValue, bool> _validation;
    
    /// <inheritdoc />
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the CustomValidator class.
    /// </summary>
    /// <param name="validation">A function that takes the field value and returns true if valid, false if invalid.</param>
    /// <param name="errorMessage">The error message to display when validation fails.</param>
    public CustomValidator(Func<TValue, bool> validation, string errorMessage)
    {
        _validation = validation;
        ErrorMessage = errorMessage;
    }
    
    /// <summary>
    /// Validates the field value using the custom validation function.
    /// </summary>
    /// <param name="model">The complete model instance containing the field being validated.</param>
    /// <param name="value">The current value of the field being validated.</param>
    /// <param name="services">The service provider for dependency injection (not used by this validator).</param>
    /// <returns>A ValidationResult indicating success if the custom function returns true, or failure with the error message if it returns false.</returns>
    public Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services)
    {
        var isValid = _validation(value);
        return Task.FromResult(isValid 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(ErrorMessage!));
    }
}