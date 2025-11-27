using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Provides extension methods for creating specific form templates using reflection-based property binding.
/// These templates attempt to find common property names on your models and configure appropriate fields.
/// </summary>
public static class ContactFormTemplate
{
    /// <param name="builder">The FormBuilder instance to configure.</param>
    /// <typeparam name="TModel">The model type that should contain the expected properties.</typeparam>
    extension<TModel>(FormBuilder<TModel> builder) where TModel : new()
    {
        /// <summary>
        /// Configures the form builder as a contact form by adding common contact fields.
        /// This method uses reflection to find properties named FirstName, LastName, Email, and Phone.
        /// </summary>
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
        public FormBuilder<TModel> AsContactForm()
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
        public FormBuilder<TModel> AsRegistrationForm()
        {
            return builder
                .AddRequiredTextField(GetPropertyExpression<TModel>("FirstName"), "First Name", minLength: 2)
                .AddRequiredTextField(GetPropertyExpression<TModel>("LastName"), "Last Name", minLength: 2)
                .AddEmailField(GetPropertyExpression<TModel>("Email"))
                .AddPasswordField(GetPropertyExpression<TModel>("Password"))
                .AddCheckboxField(GetBoolPropertyExpression<TModel>("AcceptTerms"), "I accept the terms and conditions");
        }
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