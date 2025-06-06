using DynamicFormBlazor.Forms.Core;

namespace DynamicFormBlazor.Forms.Validators;

public class RequiredValidator<TModel, TValue> : IFieldValidator<TModel, TValue>
{
    public string? ErrorMessage { get; set; }
    
    public RequiredValidator(string? errorMessage = null)
    {
        ErrorMessage = errorMessage ?? "This field is required.";
    }
    
    public Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services)
    {
        var isValid = value switch
        {
            null => false,
            string str => !string.IsNullOrWhiteSpace(str),
            _ => true
        };
        
        return Task.FromResult(isValid 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(ErrorMessage!));
    }
}