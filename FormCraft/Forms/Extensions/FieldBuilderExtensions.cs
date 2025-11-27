using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// Provides extension methods for the FieldBuilder to configure common field types and validation rules.
/// </summary>
public static class FieldBuilderExtensions
{
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value (string or string?).</typeparam>
    extension<TModel, TValue>(FieldBuilder<TModel, TValue> builder) where TModel : new()
    {
        /// <summary>
        /// Configures a string field to be rendered as a multi-line text area (supports both nullable and non-nullable strings).
        /// </summary>
        /// <param name="lines">Number of visible lines for the text area (default: 3).</param>
        /// <param name="maxLength">Optional maximum character length allowed.</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Description)
        ///     .AsTextArea(lines: 5, maxLength: 500)
        /// </code>
        /// </example>
        public FieldBuilder<TModel, TValue> AsTextArea(int lines = 3,
            int? maxLength = null)
        {
            builder.WithAttribute("Lines", lines);
            if (maxLength.HasValue)
            {
                builder.WithAttribute("MaxLength", maxLength.Value);
            }
            return builder;
        }

        /// <summary>
        /// Configures the field to use a custom renderer with explicit type parameters.
        /// </summary>
        /// <typeparam name="TRenderer">The type of the custom renderer.</typeparam>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Color)
        ///     .WithCustomRenderer&lt;ProductModel, string, ColorPickerRenderer&gt;()
        /// </code>
        /// </example>
        public FieldBuilder<TModel, TValue> WithCustomRenderer<TRenderer>() where TRenderer : ICustomFieldRenderer<TValue>
        {
            builder.Configuration.CustomRendererType = typeof(TRenderer);
            return builder;
        }

        /// <summary>
        /// Configures the field to use a custom renderer by type.
        /// </summary>
        /// <param name="rendererType">The type of the custom renderer.</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Color)
        ///     .WithCustomRenderer(typeof(MudBlazorColorPickerRenderer))
        /// </code>
        /// </example>
        public FieldBuilder<TModel, TValue> WithCustomRenderer(Type rendererType)
        {
            builder.Configuration.CustomRendererType = rendererType;
            return builder;
        }

        /// <summary>
        /// Adds options to a field for rendering as a dropdown or select list.
        /// </summary>
        /// <param name="options">Variable number of tuples containing (value, label) pairs for options.</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Status)
        ///     .WithOptions(
        ///         ("active", "Active"),
        ///         ("inactive", "Inactive"),
        ///         ("pending", "Pending"))
        /// </code>
        /// </example>
        public FieldBuilder<TModel, TValue> WithOptions(params (TValue value, string label)[] options)
        {
            var selectOptions = options.Select(o => new SelectOption<TValue>(o.value, o.label));
            return builder.WithAttribute("Options", selectOptions);
        }

        /// <summary>
        /// Adds options to a field for rendering as a dropdown or select list.
        /// This overload accepts SelectOption collection for more control over options.
        /// </summary>
        /// <param name="options">Collection of SelectOption objects.</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        public FieldBuilder<TModel, TValue> WithSelectOptions(IEnumerable<SelectOption<TValue>> options)
        {
            return builder.WithAttribute("Options", options);
        }
    }

    /// <param name="builder">The FieldBuilder instance for an IEnumerable field.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of individual option values.</typeparam>
    extension<TModel, TValue>(FieldBuilder<TModel, IEnumerable<TValue>> builder) where TModel : new()
    {
        /// <summary>
        /// Configures a field to allow multiple selections.
        /// </summary>
        /// <param name="options">Variable number of tuples containing (value, label) pairs for options.</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.SelectedCategories)
        ///     .AsMultiSelect(
        ///         ("tech", "Technology"),
        ///         ("health", "Healthcare"),
        ///         ("finance", "Finance"))
        /// </code>
        /// </example>
        public FieldBuilder<TModel, IEnumerable<TValue>> AsMultiSelect(params (TValue value, string label)[] options)
        {
            var selectOptions = options.Select(o => new SelectOption<TValue>(o.value, o.label));
            return builder.WithAttribute("MultiSelectOptions", selectOptions);
        }
    }
    
    /// <param name="builder">The FieldBuilder instance for a numeric field.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The numeric type of the field value.</typeparam>
    extension<TModel, TValue>(FieldBuilder<TModel, TValue> builder) where TModel : new() where TValue : struct, IComparable<TValue>
    {
        /// <summary>
        /// Configures a numeric field to be rendered as a slider with min/max values.
        /// </summary>
        /// <param name="min">Minimum value for the slider.</param>
        /// <param name="max">Maximum value for the slider.</param>
        /// <param name="step">Step increment for the slider.</param>
        /// <param name="showValue">Whether to display the current value (default: true).</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Rating)
        ///     .AsSlider(min: 0, max: 10, step: 1)
        /// </code>
        /// </example>
        public FieldBuilder<TModel, TValue> AsSlider(TValue min,
            TValue max,
            TValue step,
            bool showValue = true)
        {
            return builder
                .WithAttribute("UseSlider", true)
                .WithAttribute("Min", min)
                .WithAttribute("Max", max)
                .WithAttribute("Step", step)
                .WithAttribute("ShowValue", showValue);
        }
    }

    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value (string or string?).</typeparam>
    extension<TModel, TValue>(FieldBuilder<TModel, TValue> builder) where TModel : new()
    {
        /// <summary>
        /// Adds email format validation to a string field (supports both nullable and non-nullable strings).
        /// </summary>
        /// <param name="errorMessage">Custom error message (default: "Please enter a valid email address").</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Email)
        ///     .WithEmailValidation("Invalid email format")
        /// </code>
        /// </example>
        public FieldBuilder<TModel, TValue> WithEmailValidation(string? errorMessage = null)
        {
            return builder.WithValidator(
                value => value == null || IsValidEmail(value?.ToString() ?? ""),
                errorMessage ?? "Please enter a valid email address");
        }
    }

    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    extension<TModel>(FieldBuilder<TModel, string> builder) where TModel : new()
    {
        /// <summary>
        /// Adds minimum length validation to a string field.
        /// </summary>
        /// <param name="minLength">Minimum required character length.</param>
        /// <param name="errorMessage">Custom error message (default: "Must be at least {minLength} characters long").</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Username)
        ///     .WithMinLength(3, "Username too short")
        /// </code>
        /// </example>
        public FieldBuilder<TModel, string> WithMinLength(int minLength,
            string? errorMessage = null)
        {
            return builder.WithValidator(
                value => string.IsNullOrEmpty(value) || value.Length >= minLength,
                errorMessage ?? $"Must be at least {minLength} characters long");
        }

        /// <summary>
        /// Adds maximum length validation to a string field.
        /// </summary>
        /// <param name="maxLength">Maximum allowed character length.</param>
        /// <param name="errorMessage">Custom error message (default: "Must be no more than {maxLength} characters long").</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Bio)
        ///     .WithMaxLength(500, "Bio too long")
        /// </code>
        /// </example>
        public FieldBuilder<TModel, string> WithMaxLength(int maxLength,
            string? errorMessage = null)
        {
            return builder.WithValidator(
                value => string.IsNullOrEmpty(value) || value.Length <= maxLength,
                errorMessage ?? $"Must be no more than {maxLength} characters long");
        }
    }

    /// <param name="builder">The FieldBuilder instance.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value that implements IComparable.</typeparam>
    extension<TModel, TValue>(FieldBuilder<TModel, TValue> builder) where TModel : new() where TValue : IComparable<TValue>
    {
        /// <summary>
        /// Adds range validation to a field with comparable values.
        /// </summary>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <param name="errorMessage">Custom error message (default: "Must be between {min} and {max}").</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Age)
        ///     .WithRange(18, 65, "Age must be between 18 and 65")
        /// </code>
        /// </example>
        public FieldBuilder<TModel, TValue> WithRange(TValue min,
            TValue max,
            string? errorMessage = null)
        {
            return builder.WithValidator(
                value => value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0,
                errorMessage ?? $"Must be between {min} and {max}");
        }
    }

    /// <param name="builder">The FieldBuilder instance for an IBrowserFile field.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    extension<TModel>(FieldBuilder<TModel, IBrowserFile> builder) where TModel : new()
    {
        /// <summary>
        /// Configures a field for file upload with specified constraints.
        /// </summary>
        /// <param name="acceptedFileTypes">Array of accepted file extensions (e.g., ".jpg", ".pdf").</param>
        /// <param name="maxFileSize">Maximum file size in bytes.</param>
        /// <param name="showPreview">Whether to show image preview for image files.</param>
        /// <param name="enableDragDrop">Whether to enable drag and drop functionality.</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Resume)
        ///     .AsFileUpload(
        ///         acceptedFileTypes: new[] { ".pdf", ".doc", ".docx" },
        ///         maxFileSize: 5 * 1024 * 1024 // 5MB
        ///     )
        /// </code>
        /// </example>
        public FieldBuilder<TModel, IBrowserFile> AsFileUpload(string[]? acceptedFileTypes = null,
            long? maxFileSize = null,
            bool showPreview = true,
            bool enableDragDrop = true)
        {
            var config = new FileUploadConfiguration
            {
                AcceptedFileTypes = acceptedFileTypes,
                MaxFileSize = maxFileSize,
                MaxFiles = 1,
                ShowPreview = showPreview,
                EnableDragDrop = enableDragDrop
            };

            builder.WithAttribute("FileUploadConfiguration", config);
            builder.WithCustomRenderer(new FileUploadFieldRenderer());

            return builder;
        }
    }

    /// <param name="builder">The FieldBuilder instance for an IReadOnlyList&lt;IBrowserFile&gt; field.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    extension<TModel>(FieldBuilder<TModel, IReadOnlyList<IBrowserFile>> builder) where TModel : new()
    {
        /// <summary>
        /// Configures a field for multiple file uploads with specified constraints.
        /// </summary>
        /// <param name="maxFiles">Maximum number of files allowed.</param>
        /// <param name="acceptedFileTypes">Array of accepted file extensions (e.g., ".jpg", ".pdf").</param>
        /// <param name="maxFileSize">Maximum file size in bytes per file.</param>
        /// <param name="showPreview">Whether to show image preview for image files.</param>
        /// <param name="enableDragDrop">Whether to enable drag and drop functionality.</param>
        /// <returns>The FieldBuilder instance for method chaining.</returns>
        /// <example>
        /// <code>
        /// .AddField(x => x.Documents)
        ///     .AsMultipleFileUpload(
        ///         maxFiles: 5,
        ///         acceptedFileTypes: new[] { ".pdf", ".jpg", ".png" },
        ///         maxFileSize: 10 * 1024 * 1024 // 10MB per file
        ///     )
        /// </code>
        /// </example>
        public FieldBuilder<TModel, IReadOnlyList<IBrowserFile>> AsMultipleFileUpload(int maxFiles = 10,
            string[]? acceptedFileTypes = null,
            long? maxFileSize = null,
            bool showPreview = true,
            bool enableDragDrop = true)
        {
            var config = new FileUploadConfiguration
            {
                AcceptedFileTypes = acceptedFileTypes,
                MaxFileSize = maxFileSize,
                MaxFiles = maxFiles,
                ShowPreview = showPreview,
                EnableDragDrop = enableDragDrop
            };

            builder.WithAttribute("FileUploadConfiguration", config);
            builder.WithCustomRenderer(new FileUploadFieldRenderer());

            return builder;
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
