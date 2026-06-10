using System.Linq.Expressions;
using System.Reflection;

namespace FormCraft;

/// <summary>
/// Provides pre-built form templates for common use cases, reducing boilerplate code for standard forms.
/// Each template uses property-name conventions: it inspects <c>TModel</c> via reflection and adds a
/// pre-configured field for every conventional property the model exposes (e.g. <c>Email</c>, <c>Password</c>).
/// If the model has none of the expected properties, an <see cref="InvalidOperationException"/> is thrown
/// instead of silently returning an empty form.
/// </summary>
public static class FormTemplates
{
    /// <summary>
    /// Creates a contact form configuration based on property-name conventions.
    /// Recognized string properties: <c>Name</c> (or <c>FirstName</c> and <c>LastName</c>),
    /// <c>Email</c>, <c>Phone</c>, <c>Subject</c>, and <c>Message</c>.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A form configuration containing the contact fields found on <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="T"/> has none of the expected contact form properties.
    /// </exception>
    /// <example>
    /// <code>
    /// public class ContactModel
    /// {
    ///     public string Name { get; set; } = "";
    ///     public string Email { get; set; } = "";
    ///     public string Message { get; set; } = "";
    /// }
    ///
    /// var contactFormConfig = FormTemplates.ContactForm&lt;ContactModel&gt;();
    /// </code>
    /// </example>
    public static IFormConfiguration<T> ContactForm<T>() where T : new()
    {
        var builder = FormBuilder<T>.Create();
        var fieldsAdded = 0;

        if (TryGetStringProperty<T>("FirstName", out var firstName))
        {
            builder.AddRequiredTextField(StringExpression<T>(firstName), "First Name", "Enter your first name", 2);
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("LastName", out var lastName))
        {
            builder.AddRequiredTextField(StringExpression<T>(lastName), "Last Name", "Enter your last name", 2);
            fieldsAdded++;
        }

        // Only fall back to a single "Name" field when FirstName/LastName are not available
        if (fieldsAdded == 0 && TryGetStringProperty<T>("Name", out var name))
        {
            builder.AddRequiredTextField(StringExpression<T>(name), "Name", "Enter your name", 2);
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("Email", out var email))
        {
            builder.AddEmailField(StringExpression<T>(email));
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("Phone", out var phone))
        {
            builder.AddPhoneField(StringExpression<T>(phone));
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("Subject", out var subject))
        {
            builder.AddRequiredTextField(StringExpression<T>(subject), "Subject", "Enter a subject");
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("Message", out var message))
        {
            builder.AddTextArea(NullableStringExpression<T>(message), "Message", rows: 4,
                fieldConfig: field => field
                    .Required("Message is required")
                    .WithPlaceholder("Enter your message"));
            fieldsAdded++;
        }

        if (fieldsAdded == 0)
        {
            throw new InvalidOperationException(
                $"FormTemplates.ContactForm<{typeof(T).Name}> could not find any matching properties. " +
                "Expected writable string properties named: Name (or FirstName and LastName), Email, Phone, Subject, or Message.");
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates a registration form configuration based on property-name conventions.
    /// Recognized properties: <c>FirstName</c>, <c>LastName</c>, <c>Email</c>, <c>Password</c>,
    /// <c>ConfirmPassword</c> (string), and <c>AcceptTerms</c> (bool).
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A form configuration containing the registration fields found on <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="T"/> has none of the expected registration form properties.
    /// </exception>
    public static IFormConfiguration<T> RegistrationForm<T>() where T : new()
    {
        var builder = FormBuilder<T>.Create();
        var fieldsAdded = 0;

        if (TryGetStringProperty<T>("FirstName", out var firstName))
        {
            builder.AddRequiredTextField(StringExpression<T>(firstName), "First Name", "Enter your first name", 2);
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("LastName", out var lastName))
        {
            builder.AddRequiredTextField(StringExpression<T>(lastName), "Last Name", "Enter your last name", 2);
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("Email", out var email))
        {
            builder.AddEmailField(StringExpression<T>(email));
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("Password", out var password))
        {
            builder.AddPasswordField(StringExpression<T>(password));
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("ConfirmPassword", out var confirmPassword))
        {
            builder.AddField(StringExpression<T>(confirmPassword), field => field
                .WithLabel("Confirm Password")
                .WithInputType("password")
                .Required("Please confirm your password"));
            fieldsAdded++;
        }

        if (TryGetBoolProperty<T>("AcceptTerms", out var acceptTerms))
        {
            builder.AddCheckboxField(BoolExpression<T>(acceptTerms), "I accept the terms and conditions");
            fieldsAdded++;
        }

        if (fieldsAdded == 0)
        {
            throw new InvalidOperationException(
                $"FormTemplates.RegistrationForm<{typeof(T).Name}> could not find any matching properties. " +
                "Expected writable string properties named: FirstName, LastName, Email, Password, ConfirmPassword, " +
                "or a writable bool property named AcceptTerms.");
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates a login form configuration based on property-name conventions.
    /// Recognized properties: <c>Email</c> or <c>Username</c> (string), <c>Password</c> (string),
    /// and <c>RememberMe</c> (bool).
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A form configuration containing the login fields found on <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="T"/> has none of the expected login form properties.
    /// </exception>
    public static IFormConfiguration<T> LoginForm<T>() where T : new()
    {
        var builder = FormBuilder<T>.Create();
        var fieldsAdded = 0;

        if (TryGetStringProperty<T>("Email", out var email))
        {
            builder.AddEmailField(StringExpression<T>(email));
            fieldsAdded++;
        }
        else if (TryGetStringProperty<T>("Username", out var username))
        {
            builder.AddRequiredTextField(StringExpression<T>(username), "Username", "Enter your username");
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("Password", out var password))
        {
            // Login forms should not enforce strength rules on existing passwords
            builder.AddField(StringExpression<T>(password), field => field
                .WithLabel("Password")
                .WithInputType("password")
                .Required("Password is required"));
            fieldsAdded++;
        }

        if (TryGetBoolProperty<T>("RememberMe", out var rememberMe))
        {
            builder.AddCheckboxField(BoolExpression<T>(rememberMe), "Remember me");
            fieldsAdded++;
        }

        if (fieldsAdded == 0)
        {
            throw new InvalidOperationException(
                $"FormTemplates.LoginForm<{typeof(T).Name}> could not find any matching properties. " +
                "Expected writable string properties named: Email or Username, Password, " +
                "or a writable bool property named RememberMe.");
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates an address form configuration based on property-name conventions.
    /// Recognized string properties: <c>Street</c> or <c>AddressLine1</c>, <c>AddressLine2</c>,
    /// <c>City</c>, <c>State</c>, <c>PostalCode</c> or <c>ZipCode</c>, and <c>Country</c>.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A form configuration containing the address fields found on <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="T"/> has none of the expected address form properties.
    /// </exception>
    public static IFormConfiguration<T> AddressForm<T>() where T : new()
    {
        var builder = FormBuilder<T>.Create();
        var fieldsAdded = 0;

        if (TryGetStringProperty<T>("Street", out var street))
        {
            builder.AddRequiredTextField(StringExpression<T>(street), "Street Address", "Enter your street address");
            fieldsAdded++;
        }
        else if (TryGetStringProperty<T>("AddressLine1", out var addressLine1))
        {
            builder.AddRequiredTextField(StringExpression<T>(addressLine1), "Address Line 1", "Enter your street address");
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("AddressLine2", out var addressLine2))
        {
            builder.AddOptionalField(NullableStringExpression<T>(addressLine2), "Address Line 2", "Apartment, suite, etc. (optional)");
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("City", out var city))
        {
            builder.AddRequiredTextField(StringExpression<T>(city), "City", "Enter your city");
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("State", out var state))
        {
            builder.AddOptionalField(NullableStringExpression<T>(state), "State / Province", "Enter your state or province");
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("PostalCode", out var postalCode))
        {
            builder.AddRequiredTextField(StringExpression<T>(postalCode), "Postal Code", "Enter your postal code");
            fieldsAdded++;
        }
        else if (TryGetStringProperty<T>("ZipCode", out var zipCode))
        {
            builder.AddRequiredTextField(StringExpression<T>(zipCode), "ZIP Code", "Enter your ZIP code");
            fieldsAdded++;
        }

        if (TryGetStringProperty<T>("Country", out var country))
        {
            builder.AddRequiredTextField(StringExpression<T>(country), "Country", "Enter your country");
            fieldsAdded++;
        }

        if (fieldsAdded == 0)
        {
            throw new InvalidOperationException(
                $"FormTemplates.AddressForm<{typeof(T).Name}> could not find any matching properties. " +
                "Expected writable string properties named: Street or AddressLine1, AddressLine2, City, State, " +
                "PostalCode or ZipCode, or Country.");
        }

        return builder.Build();
    }

    private static bool TryGetStringProperty<T>(string propertyName, out PropertyInfo property)
    {
        return TryGetProperty<T>(propertyName, typeof(string), out property);
    }

    private static bool TryGetBoolProperty<T>(string propertyName, out PropertyInfo property)
    {
        return TryGetProperty<T>(propertyName, typeof(bool), out property);
    }

    private static bool TryGetProperty<T>(string propertyName, Type propertyType, out PropertyInfo property)
    {
        var candidate = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (candidate != null && candidate.PropertyType == propertyType && candidate.CanRead && candidate.CanWrite)
        {
            property = candidate;
            return true;
        }

        property = null!;
        return false;
    }

    private static Expression<Func<T, string>> StringExpression<T>(PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        return Expression.Lambda<Func<T, string>>(Expression.Property(parameter, property), parameter);
    }

    private static Expression<Func<T, string?>> NullableStringExpression<T>(PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        return Expression.Lambda<Func<T, string?>>(Expression.Property(parameter, property), parameter);
    }

    private static Expression<Func<T, bool>> BoolExpression<T>(PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        return Expression.Lambda<Func<T, bool>>(Expression.Property(parameter, property), parameter);
    }
}
