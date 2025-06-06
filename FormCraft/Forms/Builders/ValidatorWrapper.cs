using FormCraft.Forms.Abstractions;
using FormCraft.Forms.Core;

namespace FormCraft.Forms.Builders;

/// <summary>
/// Internal wrapper class that converts strongly-typed field validators to object-based validators.
/// This enables the form system to handle different validator types uniformly while preserving type safety.
/// </summary>
/// <typeparam name="TModel">The model type that the form binds to.</typeparam>
/// <typeparam name="TValue">The actual type of the field value being validated.</typeparam>
public class ValidatorWrapper<TModel, TValue> : IFieldValidator<TModel, object>
{
    private readonly IFieldValidator<TModel, TValue> _inner;
    
    /// <summary>
    /// Initializes a new instance of the ValidatorWrapper class.
    /// </summary>
    /// <param name="inner">The strongly-typed validator to wrap.</param>
    public ValidatorWrapper(IFieldValidator<TModel, TValue> inner)
    {
        _inner = inner;
    }
    
    /// <inheritdoc />
    public string? ErrorMessage 
    { 
        get => _inner.ErrorMessage; 
        set => _inner.ErrorMessage = value; 
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
        
        return await _inner.ValidateAsync(model, typedValue, services);
    }
}