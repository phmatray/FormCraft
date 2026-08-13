namespace FormCraft;

/// <summary>
/// Internal wrapper class that converts strongly-typed field validators to object-based validators.
/// This enables the form system to handle different validator types uniformly while preserving type safety.
/// </summary>
/// <typeparam name="TModel">The model type that the form binds to.</typeparam>
/// <typeparam name="TValue">The actual type of the field value being validated.</typeparam>
public class ValidatorWrapper<TModel, TValue> : IFieldValidator<TModel, object>
{

    /// <summary>
    /// Initializes a new instance of the ValidatorWrapper class.
    /// </summary>
    /// <param name="inner">The strongly-typed validator to wrap.</param>
    public ValidatorWrapper(IFieldValidator<TModel, TValue> inner)
    {
        Inner = inner;
    }

    /// <summary>
    /// Gets the strongly-typed validator wrapped by this instance.
    /// </summary>
    internal IFieldValidator<TModel, TValue> Inner { get; }

    /// <inheritdoc />
    public string? ErrorMessage
    {
        get => Inner.ErrorMessage;
        set => Inner.ErrorMessage = value;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(TModel model, object? value, IServiceProvider services)
    {
        // Convert object back to TValue for the inner validator
        TValue typedValue;
        try
        {
            typedValue = (TValue)(value ?? default(TValue)!);
        }
        catch
        {
            typedValue = default!;
        }

        return await Inner.ValidateAsync(model, typedValue, services);
    }
}
