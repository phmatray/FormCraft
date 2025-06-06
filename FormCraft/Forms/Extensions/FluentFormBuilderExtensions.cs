using System.Linq.Expressions;
using FormCraft.Forms.Builders;

namespace FormCraft.Forms.Extensions;

/// <summary>
/// Provides convenient fluent extension methods for quickly configuring common form field types.
/// These methods reduce boilerplate code by combining multiple configuration steps into single method calls.
/// </summary>
public static class FluentFormBuilderExtensions
{
    /// <summary>
    /// Adds a required text field with validation and common configuration options.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the string property on the model.</param>
    /// <param name="label">The display label for the field.</param>
    /// <param name="placeholder">Optional placeholder text to display in the field.</param>
    /// <param name="minLength">Minimum character length required (default: 1).</param>
    /// <param name="maxLength">Maximum character length allowed (default: 255).</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddRequiredTextField(x => x.FirstName, "First Name", "Enter your first name", minLength: 2);
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddRequiredTextField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string>> expression,
        string label,
        string? placeholder = null,
        int minLength = 1,
        int maxLength = 255) where TModel : new()
    {
        var fieldBuilder = builder.AddField(expression)
            .WithLabel(label)
            .Required($"{label} is required");

        if (minLength > 1)
            fieldBuilder.WithMinLength(minLength, $"Must be at least {minLength} characters");

        if (maxLength < 255)
            fieldBuilder.WithMaxLength(maxLength, $"Must be no more than {maxLength} characters");

        if (!string.IsNullOrEmpty(placeholder))
            fieldBuilder.WithPlaceholder(placeholder);

        return builder;
    }

    /// <summary>
    /// Adds an email field with built-in email format validation and required validation.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the string property for the email.</param>
    /// <param name="label">The display label for the field (default: "Email Address").</param>
    /// <param name="placeholder">Optional placeholder text (default: "your.email@example.com").</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddEmailField(x => x.Email);
    /// // or with custom label
    /// builder.AddEmailField(x => x.EmailAddress, "Contact Email");
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddEmailField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string>> expression,
        string label = "Email Address",
        string? placeholder = null) where TModel : new()
    {
        builder.AddField(expression)
            .WithLabel(label)
            .Required($"{label} is required")
            .WithEmailValidation()
            .WithPlaceholder(placeholder ?? "your.email@example.com");

        return builder;
    }

    /// <summary>
    /// Adds a numeric input field with optional range validation and required validation.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the integer property.</param>
    /// <param name="label">The display label for the field.</param>
    /// <param name="min">Minimum allowed value (default: int.MinValue, meaning no minimum).</param>
    /// <param name="max">Maximum allowed value (default: int.MaxValue, meaning no maximum).</param>
    /// <param name="required">Whether the field is required (default: true).</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddNumericField(x => x.Age, "Age", min: 18, max: 100);
    /// // or optional field
    /// builder.AddNumericField(x => x.YearsOfExperience, "Years of Experience", min: 0, required: false);
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddNumericField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, int>> expression,
        string label,
        int min = int.MinValue,
        int max = int.MaxValue,
        bool required = true) where TModel : new()
    {
        var fieldBuilder = builder.AddField(expression)
            .WithLabel(label);

        if (required)
            fieldBuilder.Required($"{label} is required");

        if (min != int.MinValue || max != int.MaxValue)
            fieldBuilder.WithRange(min, max, $"Must be between {min} and {max}");

        return builder;
    }

    /// <summary>
    /// Adds a dropdown selection field with predefined options and required validation.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the string property for the selected value.</param>
    /// <param name="label">The display label for the field.</param>
    /// <param name="options">Variable number of tuples containing (value, label) pairs for the dropdown options.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddDropdownField(x => x.Country, "Country",
    ///     ("US", "United States"),
    ///     ("CA", "Canada"),
    ///     ("UK", "United Kingdom"));
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddDropdownField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string>> expression,
        string label,
        params (string value, string label)[] options) where TModel : new()
    {
        builder.AddField(expression)
            .WithLabel(label)
            .Required($"Please select {label}")
            .WithOptions(options);

        return builder;
    }

    /// <summary>
    /// Adds a phone number field with US phone format validation.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the string property for the phone number.</param>
    /// <param name="label">The display label for the field (default: "Phone Number").</param>
    /// <param name="required">Whether the field is required (default: false).</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddPhoneField(x => x.PhoneNumber, required: true);
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddPhoneField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string>> expression,
        string label = "Phone Number",
        bool required = false) where TModel : new()
    {
        var fieldBuilder = builder.AddField(expression)
            .WithLabel(label)
            .WithPlaceholder("(555) 123-4567")
            .WithValidator(phone => string.IsNullOrEmpty(phone) || IsValidPhone(phone), 
                "Please enter a valid phone number");

        if (required)
            fieldBuilder.Required($"{label} is required");

        return builder;
    }

    /// <summary>
    /// Adds a password field with configurable strength requirements.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the string property for the password.</param>
    /// <param name="label">The display label for the field (default: "Password").</param>
    /// <param name="minLength">Minimum password length required (default: 8).</param>
    /// <param name="requireSpecialChars">Whether special characters are required (default: true).</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddPasswordField(x => x.Password, minLength: 10, requireSpecialChars: true);
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddPasswordField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string>> expression,
        string label = "Password",
        int minLength = 8,
        bool requireSpecialChars = true) where TModel : new()
    {
        var fieldBuilder = builder.AddField(expression)
            .WithLabel(label)
            .Required($"{label} is required")
            .WithMinLength(minLength, $"Must be at least {minLength} characters");

        if (requireSpecialChars)
        {
            fieldBuilder.WithValidator(password => 
                string.IsNullOrEmpty(password) || HasSpecialCharacters(password),
                "Must contain at least one special character");
        }

        return builder;
    }

    /// <summary>
    /// Adds a checkbox field for boolean properties with optional help text.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the boolean property.</param>
    /// <param name="label">The display label/text for the checkbox.</param>
    /// <param name="helpText">Optional help text to display below the checkbox.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddCheckboxField(x => x.AcceptTerms, "I accept the terms and conditions",
    ///     helpText: "You must accept to continue");
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddCheckboxField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, bool>> expression,
        string label,
        string? helpText = null) where TModel : new()
    {
        var fieldBuilder = builder.AddField(expression)
            .WithLabel(label);

        if (!string.IsNullOrEmpty(helpText))
            fieldBuilder.WithHelpText(helpText);

        return builder;
    }

    /// <summary>
    /// Creates a contact form with common fields
    /// </summary>
    public static FormBuilder<TModel> AsContactForm<TModel>(this FormBuilder<TModel> builder) 
        where TModel : new()
    {
        // This is a template method - would need reflection or specific implementation
        return builder;
    }

    /// <summary>
    /// Creates a registration form with common fields
    /// </summary>
    public static FormBuilder<TModel> AsRegistrationForm<TModel>(this FormBuilder<TModel> builder)
        where TModel : new()
    {
        // This is a template method - would need reflection or specific implementation
        return builder;
    }

    #region Helper Methods
    
    private static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Remove common phone formatting
        var cleanPhone = phone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "").Replace(".", "");
        
        // Check if it's 10 or 11 digits (US format)
        return cleanPhone.Length >= 10 && cleanPhone.Length <= 11 && cleanPhone.All(char.IsDigit);
    }

    private static bool HasSpecialCharacters(string password)
    {
        return password.Any(c => !char.IsLetterOrDigit(c));
    }

    #endregion
}