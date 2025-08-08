namespace FormCraft;

/// <summary>
/// A built-in validator that ensures a field has a value (is not null, empty, or whitespace).
/// </summary>
/// <typeparam name="TModel">The model type that contains the field being validated.</typeparam>
/// <typeparam name="TValue">The type of the field value being validated.</typeparam>
/// <example>
/// <code>
/// // Using with FieldBuilder
/// .AddField(x => x.Name)
///     .WithValidator(new RequiredValidator&lt;MyModel, string&gt;("Name is required"));
/// 
/// // Or using the Required() extension method (recommended)
/// .AddField(x => x.Name)
///     .Required("Name is required");
/// </code>
/// </example>
public class RequiredValidator<TModel, TValue> : IFieldValidator<TModel, TValue>
{
    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the RequiredValidator class.
    /// </summary>
    /// <param name="errorMessage">The error message to display when validation fails. If null, a default message is used.</param>
    public RequiredValidator(string? errorMessage = null)
    {
        ErrorMessage = errorMessage ?? "This field is required.";
    }

    /// <summary>
    /// Validates that the field has a value. For strings, checks that the value is not null, empty, or whitespace.
    /// For other types, checks that the value is not null.
    /// </summary>
    /// <param name="model">The complete model instance containing the field being validated.</param>
    /// <param name="value">The current value of the field being validated.</param>
    /// <param name="services">The service provider for dependency injection (not used by this validator).</param>
    /// <returns>A ValidationResult indicating success if the field has a value, or failure with an error message if it doesn't.</returns>
    public Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services)
    {
        var isValid = value switch
        {
            null => false,
            string str => !string.IsNullOrWhiteSpace(str),
            bool b => b,
            System.Collections.IEnumerable enumerable => enumerable.Cast<object>().Any(),
            _ => true
        };

        return Task.FromResult(isValid
            ? ValidationResult.Success()
            : ValidationResult.Failure(ErrorMessage!));
    }
}