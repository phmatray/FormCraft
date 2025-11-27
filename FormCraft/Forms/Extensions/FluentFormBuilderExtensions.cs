using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// Provides convenient fluent extension methods for quickly configuring common form field types.
/// These methods reduce boilerplate code by combining multiple configuration steps into single method calls.
/// </summary>
public static class FluentFormBuilderExtensions
{
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    extension<TModel>(FormBuilder<TModel> builder) where TModel : new()
    {
        /// <summary>
        /// Adds a required text field with validation and common configuration options.
        /// </summary>
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
        public FormBuilder<TModel> AddRequiredTextField(Expression<Func<TModel, string>> expression,
            string label,
            string? placeholder = null,
            int minLength = 1,
            int maxLength = 255)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label)
                    .Required($"{label} is required");

                if (minLength > 1)
                    field.WithMinLength(minLength, $"Must be at least {minLength} characters");

                if (maxLength < 255)
                    field.WithMaxLength(maxLength, $"Must be no more than {maxLength} characters");

                if (!string.IsNullOrEmpty(placeholder))
                    field.WithPlaceholder(placeholder);
            });
        }

        /// <summary>
        /// Adds an email field with built-in email format validation and required validation.
        /// </summary>
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
        public FormBuilder<TModel> AddEmailField(Expression<Func<TModel, string>> expression,
            string label = "Email Address",
            string? placeholder = null)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label)
                    .Required($"{label} is required")
                    .WithEmailValidation()
                    .WithPlaceholder(placeholder ?? "your.email@example.com");
            });
        }

        /// <summary>
        /// Adds a numeric input field with optional range validation and required validation.
        /// </summary>
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
        public FormBuilder<TModel> AddNumericField(Expression<Func<TModel, int>> expression,
            string label,
            int min = int.MinValue,
            int max = int.MaxValue,
            bool required = true)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label);

                if (required)
                    field.Required($"{label} is required");

                if (min != int.MinValue || max != int.MaxValue)
                    field.WithRange(min, max, $"Must be between {min} and {max}");
            });
        }

        /// <summary>
        /// Adds a decimal field with optional range validation for currency, percentages, or other decimal values.
        /// </summary>
        /// <param name="expression">A lambda expression identifying the decimal property.</param>
        /// <param name="label">The display label for the field.</param>
        /// <param name="min">Minimum value allowed (default: decimal.MinValue).</param>
        /// <param name="max">Maximum value allowed (default: decimal.MaxValue).</param>
        /// <param name="required">Whether the field is required (default: true).</param>
        /// <param name="placeholder">Optional placeholder text.</param>
        /// <returns>The FormBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// builder.AddDecimalField(x => x.Price, "Price", min: 0, max: 1000, placeholder: "0.00");
        /// </code>
        /// </example>
        public FormBuilder<TModel> AddDecimalField(Expression<Func<TModel, decimal>> expression,
            string label,
            decimal min = decimal.MinValue,
            decimal max = decimal.MaxValue,
            bool required = true,
            string? placeholder = null)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label);

                if (required)
                    field.Required($"{label} is required");

                if (min != decimal.MinValue || max != decimal.MaxValue)
                    field.WithRange(min, max, $"Must be between {min} and {max}");

                if (!string.IsNullOrEmpty(placeholder))
                    field.WithPlaceholder(placeholder);
            });
        }

        /// <summary>
        /// Adds a currency field with decimal support and formatting.
        /// </summary>
        /// <param name="expression">A lambda expression identifying the decimal property for currency.</param>
        /// <param name="label">The display label for the field.</param>
        /// <param name="currencySymbol">Currency symbol to display (default: "$").</param>
        /// <param name="required">Whether the field is required (default: true).</param>
        /// <returns>The FormBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// builder.AddCurrencyField(x => x.Amount, "Amount", currencySymbol: "€");
        /// </code>
        /// </example>
        public FormBuilder<TModel> AddCurrencyField(Expression<Func<TModel, decimal>> expression,
            string label,
            string currencySymbol = "$",
            bool required = true)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label)
                    .WithPlaceholder($"{currencySymbol}0.00")
                    .WithHelpText($"Enter amount in {currencySymbol}");

                if (required)
                    field.Required($"{label} is required");

                // Ensure non-negative values for currency
                field.WithRange(0, decimal.MaxValue, "Amount must be positive");
            });
        }

        /// <summary>
        /// Adds a percentage field with decimal support (0-100 range).
        /// </summary>
        /// <param name="expression">A lambda expression identifying the decimal property for percentage.</param>
        /// <param name="label">The display label for the field.</param>
        /// <param name="required">Whether the field is required (default: true).</param>
        /// <returns>The FormBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// builder.AddPercentageField(x => x.DiscountRate, "Discount Rate");
        /// </code>
        /// </example>
        public FormBuilder<TModel> AddPercentageField(Expression<Func<TModel, decimal>> expression,
            string label,
            bool required = true)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label)
                    .WithPlaceholder("0.00")
                    .WithHelpText("Enter percentage (0-100)")
                    .WithRange(0, 100, "Percentage must be between 0 and 100");

                if (required)
                    field.Required($"{label} is required");
            });
        }

        /// <summary>
        /// Adds a dropdown selection field with predefined options and required validation.
        /// </summary>
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
        public FormBuilder<TModel> AddDropdownField(Expression<Func<TModel, string>> expression,
            string label,
            params (string value, string label)[] options)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label)
                    .Required($"Please select {label}")
                    .WithOptions(options);
            });
        }

        /// <summary>
        /// Adds a phone number field with US phone format validation.
        /// </summary>
        /// <param name="expression">A lambda expression identifying the string property for the phone number.</param>
        /// <param name="label">The display label for the field (default: "Phone Number").</param>
        /// <param name="required">Whether the field is required (default: false).</param>
        /// <returns>The FormBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// builder.AddPhoneField(x => x.PhoneNumber, required: true);
        /// </code>
        /// </example>
        public FormBuilder<TModel> AddPhoneField(Expression<Func<TModel, string>> expression,
            string label = "Phone Number",
            bool required = false)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label)
                    .WithPlaceholder("(555) 123-4567")
                    .WithValidator(phone => string.IsNullOrEmpty(phone) || IsValidPhone(phone),
                        "Please enter a valid phone number");

                if (required)
                    field.Required($"{label} is required");
            });
        }

        /// <summary>
        /// Adds a password field with configurable strength requirements.
        /// </summary>
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
        public FormBuilder<TModel> AddPasswordField(Expression<Func<TModel, string>> expression,
            string label = "Password",
            int minLength = 8,
            bool requireSpecialChars = true)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label)
                    .Required($"{label} is required")
                    .WithMinLength(minLength, $"Must be at least {minLength} characters");

                if (requireSpecialChars)
                {
                    field.WithValidator(password =>
                            string.IsNullOrEmpty(password) || HasSpecialCharacters(password),
                        "Must contain at least one special character");
                }
            });
        }

        /// <summary>
        /// Adds a checkbox field for boolean properties with optional help text.
        /// </summary>
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
        public FormBuilder<TModel> AddCheckboxField(Expression<Func<TModel, bool>> expression,
            string label,
            string? helpText = null)
        {
            return builder.AddField(expression, field =>
            {
                field.WithLabel(label);

                if (!string.IsNullOrEmpty(helpText))
                    field.WithHelpText(helpText);
            });
        }
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

    /// <summary>
    /// Adds a file upload field with specified constraints.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the IBrowserFile property.</param>
    /// <param name="label">The display label for the field.</param>
    /// <param name="acceptedFileTypes">Array of accepted file extensions (e.g., ".jpg", ".pdf").</param>
    /// <param name="maxFileSize">Maximum file size in bytes (default: 10MB).</param>
    /// <param name="required">Whether the field is required (default: false).</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddFileUploadField(x => x.Resume, "Upload Resume", 
    ///     acceptedFileTypes: new[] { ".pdf", ".doc", ".docx" },
    ///     maxFileSize: 5 * 1024 * 1024); // 5MB
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddFileUploadField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, IBrowserFile>> expression,
        string label,
        string[]? acceptedFileTypes = null,
        long maxFileSize = 10 * 1024 * 1024,
        bool required = false) where TModel : new()
    {
        return builder.AddField(expression, field =>
        {
            field.WithLabel(label)
                 .AsFileUpload(acceptedFileTypes, maxFileSize);

            if (required)
                field.Required($"{label} is required");
        });
    }

    /// <summary>
    /// Adds a multiple file upload field with specified constraints.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance to extend.</param>
    /// <param name="expression">A lambda expression identifying the IReadOnlyList&lt;IBrowserFile&gt; property.</param>
    /// <param name="label">The display label for the field.</param>
    /// <param name="maxFiles">Maximum number of files allowed (default: 5).</param>
    /// <param name="acceptedFileTypes">Array of accepted file extensions (e.g., ".jpg", ".pdf").</param>
    /// <param name="maxFileSize">Maximum file size in bytes per file (default: 10MB).</param>
    /// <param name="required">Whether at least one file is required (default: false).</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddMultipleFileUploadField(x => x.Documents, "Upload Documents",
    ///     maxFiles: 3,
    ///     acceptedFileTypes: new[] { ".pdf", ".jpg", ".png" },
    ///     maxFileSize: 5 * 1024 * 1024); // 5MB per file
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddMultipleFileUploadField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, IReadOnlyList<IBrowserFile>>> expression,
        string label,
        int maxFiles = 5,
        string[]? acceptedFileTypes = null,
        long maxFileSize = 10 * 1024 * 1024,
        bool required = false) where TModel : new()
    {
        return builder.AddField(expression, field =>
        {
            field.WithLabel(label)
                 .AsMultipleFileUpload(maxFiles, acceptedFileTypes, maxFileSize);

            if (required)
                field.Required($"At least one {label.ToLower()} is required");
        });
    }

    /// <summary>
    /// Adds a text area field to the form with a single method call, allowing for additional field configuration.
    /// Supports nullable string properties.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance.</param>
    /// <param name="propertyExpression">Expression selecting the property to bind.</param>
    /// <param name="label">The label for the field.</param>
    /// <param name="rows">Number of visible rows for the text area (default: 3).</param>
    /// <param name="fieldConfig">Optional additional field configuration.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// formConfig = FormBuilder&lt;CreatePaisRequest&gt;
    ///     .Create()
    ///     .AddTextArea(x => x.Observacions, "Observacions", rows: 5, 
    ///         fieldConfig: field => field
    ///             .WithMaxLength(500)
    ///             .WithHelpText("Maximum 500 characters"))
    ///     .Build();
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddTextArea<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string?>> propertyExpression,
        string label,
        int rows = 3,
        Func<FieldBuilder<TModel, string?>, FieldBuilder<TModel, string?>>? fieldConfig = null)
        where TModel : new()
    {
        return builder.AddField(propertyExpression, field =>
        {
            field.WithLabel(label)
                 .AsTextArea(lines: rows);

            if (fieldConfig != null)
            {
                fieldConfig(field);
            }
        });
    }

    /// <summary>
    /// Adds a required text field to the form with a single method call.
    /// Supports nullable string properties.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance.</param>
    /// <param name="propertyExpression">Expression selecting the property to bind.</param>
    /// <param name="label">The label for the field.</param>
    /// <param name="placeholder">Optional placeholder text.</param>
    /// <param name="errorMessage">Optional custom error message for required validation.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// formConfig = FormBuilder&lt;CreatePaisRequest&gt;
    ///     .Create()
    ///     .AddRequiredField(x => x.Codi, "Codi")
    ///     .AddRequiredField(x => x.NomPais, "Nom Pais")
    ///     .Build();
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddRequiredField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string?>> propertyExpression,
        string label,
        string? placeholder = null,
        string? errorMessage = null)
        where TModel : new()
    {
        return builder.AddField(propertyExpression, field =>
        {
            field.WithLabel(label)
                 .Required(errorMessage);

            if (!string.IsNullOrWhiteSpace(placeholder))
            {
                field.WithPlaceholder(placeholder);
            }
        });
    }

    /// <summary>
    /// Adds an optional text field to the form with a single method call.
    /// Supports nullable string properties.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FormBuilder instance.</param>
    /// <param name="propertyExpression">Expression selecting the property to bind.</param>
    /// <param name="label">The label for the field.</param>
    /// <param name="placeholder">Optional placeholder text.</param>
    /// <returns>The FormBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// formConfig = FormBuilder&lt;ContactModel&gt;
    ///     .Create()
    ///     .AddOptionalField(x => x.MiddleName, "Middle Name", "Optional")
    ///     .Build();
    /// </code>
    /// </example>
    public static FormBuilder<TModel> AddOptionalField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string?>> propertyExpression,
        string label,
        string? placeholder = null)
        where TModel : new()
    {
        return builder.AddField(propertyExpression, field =>
        {
            field.WithLabel(label);

            if (!string.IsNullOrWhiteSpace(placeholder))
            {
                field.WithPlaceholder(placeholder);
            }
        });
    }

    #endregion
}