using System.Linq.Expressions;
using DynamicFormBlazor.Forms.Builders;
using DynamicFormBlazor.Forms.Core;
using DynamicFormBlazor.Forms.Extensions;

namespace DynamicFormBlazor.Forms.Templates;

public static class FormTemplates
{
    /// <summary>
    /// Creates a basic contact form template
    /// </summary>
    public static IFormConfiguration<T> ContactForm<T>() where T : new()
    {
        // This would need to be implemented with reflection or specific model binding
        // For now, return a placeholder
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates a registration form template
    /// </summary>
    public static IFormConfiguration<T> RegistrationForm<T>() where T : new()
    {
        // This would need to be implemented with reflection or specific model binding
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates a login form template
    /// </summary>
    public static IFormConfiguration<T> LoginForm<T>() where T : new()
    {
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates an address form template
    /// </summary>
    public static IFormConfiguration<T> AddressForm<T>() where T : new()
    {
        return FormBuilder<T>.Create().Build();
    }
}

/// <summary>
/// Specific form templates with known models
/// </summary>
public static class ContactFormTemplate
{
    public static FormBuilder<TModel> AsContactForm<TModel>(this FormBuilder<TModel> builder)
        where TModel : new()
    {
        return builder
            .AddRequiredTextField(GetPropertyExpression<TModel>("FirstName"), "First Name", "Enter your first name", 2)
            .AddRequiredTextField(GetPropertyExpression<TModel>("LastName"), "Last Name", "Enter your last name", 2)
            .AddEmailField(GetPropertyExpression<TModel>("Email"), "Email Address")
            .AddPhoneField(GetPropertyExpression<TModel>("Phone"), "Phone Number", false);
    }

    public static FormBuilder<TModel> AsRegistrationForm<TModel>(this FormBuilder<TModel> builder)
        where TModel : new()
    {
        return builder
            .AddRequiredTextField(GetPropertyExpression<TModel>("FirstName"), "First Name", minLength: 2)
            .AddRequiredTextField(GetPropertyExpression<TModel>("LastName"), "Last Name", minLength: 2)
            .AddEmailField(GetPropertyExpression<TModel>("Email"))
            .AddPasswordField(GetPropertyExpression<TModel>("Password"), "Password", 8, true)
            .AddCheckboxField(GetBoolPropertyExpression<TModel>("AcceptTerms"), "I accept the terms and conditions");
    }

    private static Expression<Func<TModel, string>> GetPropertyExpression<TModel>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, propertyName);
        return Expression.Lambda<Func<TModel, string>>(property, parameter);
    }

    private static Expression<Func<TModel, bool>> GetBoolPropertyExpression<TModel>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, propertyName);
        return Expression.Lambda<Func<TModel, bool>>(property, parameter);
    }
}