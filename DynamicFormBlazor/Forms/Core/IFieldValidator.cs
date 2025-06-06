namespace DynamicFormBlazor.Forms.Core;

public interface IFieldValidator<TModel, TValue>
{
    string? ErrorMessage { get; set; }
    Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services);
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    
    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }
    
    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}