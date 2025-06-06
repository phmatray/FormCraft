using DynamicFormBlazor.Forms.Core;

namespace DynamicFormBlazor.Forms.Validators;

public class CustomValidator<TModel, TValue> : IFieldValidator<TModel, TValue>
{
    private readonly Func<TValue, bool> _validation;
    public string? ErrorMessage { get; set; }
    
    public CustomValidator(Func<TValue, bool> validation, string errorMessage)
    {
        _validation = validation;
        ErrorMessage = errorMessage;
    }
    
    public Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services)
    {
        var isValid = _validation(value);
        return Task.FromResult(isValid 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(ErrorMessage!));
    }
}

public class AsyncValidator<TModel, TValue> : IFieldValidator<TModel, TValue>
{
    private readonly Func<TValue, Task<bool>> _validation;
    public string? ErrorMessage { get; set; }
    
    public AsyncValidator(Func<TValue, Task<bool>> validation, string errorMessage)
    {
        _validation = validation;
        ErrorMessage = errorMessage;
    }
    
    public async Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services)
    {
        var isValid = await _validation(value);
        return isValid 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(ErrorMessage!);
    }
}