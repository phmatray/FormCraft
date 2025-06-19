using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Adapter that allows using FluentValidation validators with FormCraft's validation system.
/// This adapter retrieves the validator from dependency injection and validates the specified property.
/// </summary>
/// <typeparam name="TModel">The model type being validated</typeparam>
/// <typeparam name="TProperty">The property type being validated</typeparam>
public class FluentValidationAdapter<TModel, TProperty> : IFieldValidator<TModel, TProperty>
    where TModel : new()
{
    private readonly Expression<Func<TModel, TProperty>> _propertyExpression;
    
    /// <summary>
    /// Gets or sets the custom error message to display when validation fails.
    /// If not set, the error message from FluentValidation will be used.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationAdapter{TModel, TProperty}"/> class.
    /// </summary>
    /// <param name="propertyExpression">The expression identifying the property to validate</param>
    public FluentValidationAdapter(Expression<Func<TModel, TProperty>> propertyExpression)
    {
        _propertyExpression = propertyExpression;
    }

    /// <summary>
    /// Validates the specified property value using FluentValidation.
    /// </summary>
    /// <param name="model">The model instance containing the property</param>
    /// <param name="value">The property value to validate</param>
    /// <param name="serviceProvider">The service provider used to resolve the FluentValidation validator</param>
    /// <returns>A task that represents the asynchronous validation operation, containing the validation result</returns>
    public async Task<ValidationResult> ValidateAsync(
        TModel model, 
        TProperty value, 
        IServiceProvider serviceProvider)
    {
        var validator = serviceProvider.GetService<IValidator<TModel>>();
        if (validator == null)
        {
            return ValidationResult.Success();
        }

        var context = new ValidationContext<TModel>(model);
        var validationResult = await validator.ValidateAsync(context);

        if (!validationResult.IsValid)
        {
            var propertyName = GetPropertyName();
            var error = validationResult.Errors
                .FirstOrDefault(e => e.PropertyName == propertyName);
            
            if (error != null)
            {
                return ValidationResult.Failure(error.ErrorMessage);
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Extracts the property name from the property expression.
    /// </summary>
    /// <returns>The name of the property</returns>
    /// <exception cref="InvalidOperationException">Thrown when the property name cannot be determined from the expression</exception>
    private string GetPropertyName()
    {
        var expression = _propertyExpression.Body;
        var propertyPath = new List<string>();
        
        while (expression is MemberExpression memberExpression)
        {
            propertyPath.Insert(0, memberExpression.Member.Name);
            expression = memberExpression.Expression;
        }
        
        if (propertyPath.Count == 0)
        {
            throw new InvalidOperationException("Unable to determine property name from expression");
        }
        
        return string.Join(".", propertyPath);
    }
}

/// <summary>
/// Adapter for using a specific FluentValidation validator instance.
/// This adapter uses a provided validator instance instead of resolving it from dependency injection.
/// </summary>
/// <typeparam name="TModel">The model type being validated</typeparam>
/// <typeparam name="TProperty">The property type being validated</typeparam>
internal class SpecificFluentValidationAdapter<TModel, TProperty> : IFieldValidator<TModel, TProperty>
    where TModel : new()
{
    private readonly IValidator<TModel> _validator;
    private readonly Expression<Func<TModel, TProperty>> _propertyExpression;
    
    /// <summary>
    /// Gets or sets the custom error message to display when validation fails.
    /// If not set, the error message from FluentValidation will be used.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificFluentValidationAdapter{TModel, TProperty}"/> class.
    /// </summary>
    /// <param name="validator">The FluentValidation validator instance to use</param>
    /// <param name="propertyExpression">The expression identifying the property to validate</param>
    public SpecificFluentValidationAdapter(
        IValidator<TModel> validator, 
        Expression<Func<TModel, TProperty>> propertyExpression)
    {
        _validator = validator;
        _propertyExpression = propertyExpression;
    }

    /// <summary>
    /// Validates the specified property value using the provided FluentValidation validator.
    /// </summary>
    /// <param name="model">The model instance containing the property</param>
    /// <param name="value">The property value to validate</param>
    /// <param name="serviceProvider">The service provider (not used in this implementation)</param>
    /// <returns>A task that represents the asynchronous validation operation, containing the validation result</returns>
    public async Task<ValidationResult> ValidateAsync(
        TModel model, 
        TProperty value, 
        IServiceProvider serviceProvider)
    {
        var context = new ValidationContext<TModel>(model);
        var validationResult = await _validator.ValidateAsync(context);

        if (!validationResult.IsValid)
        {
            var propertyName = GetPropertyName();
            var error = validationResult.Errors
                .FirstOrDefault(e => e.PropertyName == propertyName);
            
            if (error != null)
            {
                return ValidationResult.Failure(error.ErrorMessage);
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Extracts the property name from the property expression.
    /// </summary>
    /// <returns>The name of the property</returns>
    /// <exception cref="InvalidOperationException">Thrown when the property name cannot be determined from the expression</exception>
    private string GetPropertyName()
    {
        var expression = _propertyExpression.Body;
        var propertyPath = new List<string>();
        
        while (expression is MemberExpression memberExpression)
        {
            propertyPath.Insert(0, memberExpression.Member.Name);
            expression = memberExpression.Expression;
        }
        
        if (propertyPath.Count == 0)
        {
            throw new InvalidOperationException("Unable to determine property name from expression");
        }
        
        return string.Join(".", propertyPath);
    }
}