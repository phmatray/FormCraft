using System.Linq.Expressions;
using DynamicFormBlazor.Forms.Builders;

namespace DynamicFormBlazor.Forms.Extensions;

public static class FluentFormBuilderExtensions
{
    /// <summary>
    /// Adds a required text field with common configuration
    /// </summary>
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
    /// Adds a required email field with built-in validation
    /// </summary>
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
    /// Adds a numeric field with range validation
    /// </summary>
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
    /// Adds a dropdown field with predefined options
    /// </summary>
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
    /// Adds a phone number field with validation
    /// </summary>
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
    /// Adds a password field with strength requirements
    /// </summary>
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
    /// Adds a checkbox field with custom text
    /// </summary>
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