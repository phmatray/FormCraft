using System.Linq.Expressions;
using FormCraft.Forms.Builders;
using FormCraft.Forms.Core;
using FormCraft.Forms.Extensions;

namespace FormCraft.Forms.Templates;

/// <summary>
/// Provides pre-built form templates for common use cases, reducing boilerplate code for standard forms.
/// </summary>
public static class FormTemplates
{
    /// <summary>
    /// Creates a basic contact form template with placeholder configuration.
    /// This is a generic template that requires specific field configuration for your model.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A basic form configuration that can be extended with specific fields.</returns>
    /// <example>
    /// <code>
    /// var contactFormConfig = FormTemplates.ContactForm&lt;ContactModel&gt;();
    /// // Then extend with specific fields using the builder pattern
    /// </code>
    /// </example>
    public static IFormConfiguration<T> ContactForm<T>() where T : new()
    {
        // This would need to be implemented with reflection or specific model binding
        // For now, return a placeholder
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates a registration form template with placeholder configuration.
    /// This is a generic template that requires specific field configuration for your model.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A basic form configuration that can be extended with specific fields.</returns>
    public static IFormConfiguration<T> RegistrationForm<T>() where T : new()
    {
        // This would need to be implemented with reflection or specific model binding
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates a login form template with placeholder configuration.
    /// This is a generic template that requires specific field configuration for your model.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A basic form configuration that can be extended with specific fields.</returns>
    public static IFormConfiguration<T> LoginForm<T>() where T : new()
    {
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates an address form template with placeholder configuration.
    /// This is a generic template that requires specific field configuration for your model.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A basic form configuration that can be extended with specific fields.</returns>
    public static IFormConfiguration<T> AddressForm<T>() where T : new()
    {
        return FormBuilder<T>.Create().Build();
    }
}

/// <summary>
/// Provides extension methods for creating specific form templates using reflection-based property binding.
/// These templates attempt to find common property names on your models and configure appropriate fields.
/// </summary>
public static class ContactFormTemplate
{
    /// <summary>
    /// Configures the form builder as a contact form by adding common contact fields.
    /// This method uses reflection to find properties named FirstName, LastName, Email, and Phone.
    /// </summary>
    /// <typeparam name="TModel">The model type that should contain the expected properties.</typeparam>
    /// <param name="builder">The FormBuilder instance to configure.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// var contactForm = FormBuilder&lt;ContactModel&gt;.Create()
    ///     .AsContactForm()
    ///     .Build();
    /// </code>
    /// </example>
    /// <remarks>
    /// Requires the model to have properties: FirstName (string), LastName (string), Email (string), Phone (string).
    /// If properties don't exist, exceptions will be thrown at runtime.
    /// </remarks>
    public static FormBuilder<TModel> AsContactForm<TModel>(this FormBuilder<TModel> builder)
        where TModel : new()
    {
        return builder
            .AddRequiredTextField(GetPropertyExpression<TModel>("FirstName"), "First Name", "Enter your first name", 2)
            .AddRequiredTextField(GetPropertyExpression<TModel>("LastName"), "Last Name", "Enter your last name", 2)
            .AddEmailField(GetPropertyExpression<TModel>("Email"))
            .AddPhoneField(GetPropertyExpression<TModel>("Phone"));
    }

    /// <summary>
    /// Configures the form builder as a registration form by adding common registration fields.
    /// This method uses reflection to find properties for user registration.
    /// </summary>
    /// <typeparam name="TModel">The model type that should contain the expected properties.</typeparam>
    /// <param name="builder">The FormBuilder instance to configure.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// var registrationForm = FormBuilder&lt;RegistrationModel&gt;.Create()
    ///     .AsRegistrationForm()
    ///     .Build();
    /// </code>
    /// </example>
    /// <remarks>
    /// Requires the model to have properties: FirstName (string), LastName (string), Email (string), Password (string), AcceptTerms (bool).
    /// If properties don't exist, exceptions will be thrown at runtime.
    /// </remarks>
    public static FormBuilder<TModel> AsRegistrationForm<TModel>(this FormBuilder<TModel> builder)
        where TModel : new()
    {
        return builder
            .AddRequiredTextField(GetPropertyExpression<TModel>("FirstName"), "First Name", minLength: 2)
            .AddRequiredTextField(GetPropertyExpression<TModel>("LastName"), "Last Name", minLength: 2)
            .AddEmailField(GetPropertyExpression<TModel>("Email"))
            .AddPasswordField(GetPropertyExpression<TModel>("Password"))
            .AddCheckboxField(GetBoolPropertyExpression<TModel>("AcceptTerms"), "I accept the terms and conditions");
    }

    /// <summary>
    /// Creates a lambda expression for accessing a string property by name using reflection.
    /// </summary>
    /// <typeparam name="TModel">The model type containing the property.</typeparam>
    /// <param name="propertyName">The name of the string property to access.</param>
    /// <returns>A lambda expression that can access the specified property.</returns>
    /// <exception cref="ArgumentException">Thrown when the property doesn't exist on the model type.</exception>
    private static Expression<Func<TModel, string>> GetPropertyExpression<TModel>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, propertyName);
        return Expression.Lambda<Func<TModel, string>>(property, parameter);
    }

    /// <summary>
    /// Creates a lambda expression for accessing a boolean property by name using reflection.
    /// </summary>
    /// <typeparam name="TModel">The model type containing the property.</typeparam>
    /// <param name="propertyName">The name of the boolean property to access.</param>
    /// <returns>A lambda expression that can access the specified property.</returns>
    /// <exception cref="ArgumentException">Thrown when the property doesn't exist on the model type.</exception>
    private static Expression<Func<TModel, bool>> GetBoolPropertyExpression<TModel>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, propertyName);
        return Expression.Lambda<Func<TModel, bool>>(property, parameter);
    }
}