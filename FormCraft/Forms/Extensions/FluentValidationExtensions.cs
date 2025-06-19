using System.Linq.Expressions;
using FluentValidation;

namespace FormCraft;

/// <summary>
/// Extension methods for integrating FluentValidation with FormCraft.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Adds FluentValidation support to a field using the registered IValidator for the model.
    /// The validator must be registered in the dependency injection container.
    /// </summary>
    /// <typeparam name="TModel">The model type being validated</typeparam>
    /// <typeparam name="TProperty">The property type being validated</typeparam>
    /// <param name="builder">The field builder to extend</param>
    /// <param name="propertyExpression">The expression identifying the property to validate</param>
    /// <returns>The field builder for method chaining</returns>
    /// <example>
    /// <code>
    /// services.AddScoped&lt;IValidator&lt;Customer&gt;, CustomerValidator&gt;();
    /// 
    /// var config = FormBuilder&lt;Customer&gt;.Create()
    ///     .AddField(x => x.Email, field => field
    ///         .WithFluentValidation(x => x.Email))
    ///     .Build();
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TProperty> WithFluentValidation<TModel, TProperty>(
        this FieldBuilder<TModel, TProperty> builder,
        Expression<Func<TModel, TProperty>> propertyExpression)
        where TModel : new()
    {
        return builder.WithValidator(new FluentValidationAdapter<TModel, TProperty>(propertyExpression));
    }
    
    /// <summary>
    /// Adds a specific FluentValidation validator instance to a field.
    /// This allows using a validator without registering it in dependency injection.
    /// </summary>
    /// <typeparam name="TModel">The model type being validated</typeparam>
    /// <typeparam name="TProperty">The property type being validated</typeparam>
    /// <param name="builder">The field builder to extend</param>
    /// <param name="validator">The FluentValidation validator instance to use</param>
    /// <param name="propertyExpression">The expression identifying the property to validate</param>
    /// <returns>The field builder for method chaining</returns>
    /// <example>
    /// <code>
    /// var validator = new CustomerValidator();
    /// 
    /// var config = FormBuilder&lt;Customer&gt;.Create()
    ///     .AddField(x => x.Email, field => field
    ///         .WithFluentValidator(validator, x => x.Email))
    ///     .Build();
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TProperty> WithFluentValidator<TModel, TProperty>(
        this FieldBuilder<TModel, TProperty> builder,
        IValidator<TModel> validator,
        Expression<Func<TModel, TProperty>> propertyExpression)
        where TModel : new()
    {
        return builder.WithValidator(new SpecificFluentValidationAdapter<TModel, TProperty>(validator, propertyExpression));
    }
}