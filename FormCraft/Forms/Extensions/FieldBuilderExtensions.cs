using FormCraft.Forms.Builders;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.Forms.Extensions;

/// <summary>
/// Provides extension methods for the FieldBuilder to configure common field types and validation rules.
/// </summary>
public static class FieldBuilderExtensions
{
    /// <summary>
    /// Configures a string field to be rendered as a multi-line text area.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <param name="lines">Number of visible lines for the text area (default: 3).</param>
    /// <param name="maxLength">Optional maximum character length allowed.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Description)
    ///     .AsTextArea(lines: 5, maxLength: 500)
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> AsTextArea<TModel>(
        this FieldBuilder<TModel, string> builder, 
        int lines = 3, 
        int? maxLength = null) where TModel : new()
    {
        builder.WithAttribute("Lines", lines);
        if (maxLength.HasValue)
        {
            builder.WithAttribute("MaxLength", maxLength.Value);
        }
        return builder;
    }
    
    /// <summary>
    /// Adds options to a field for rendering as a dropdown or select list.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <param name="builder">The FieldBuilder instance.</param>
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
    public static FieldBuilder<TModel, TValue> WithOptions<TModel, TValue>(
        this FieldBuilder<TModel, TValue> builder,
        params (TValue value, string label)[] options) where TModel : new()
    {
        var selectOptions = options.Select(o => new SelectOption<TValue>(o.value, o.label));
        return builder.WithAttribute("Options", selectOptions);
    }
    
    /// <summary>
    /// Configures a field to allow multiple selections.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of individual option values.</typeparam>
    /// <param name="builder">The FieldBuilder instance for an IEnumerable field.</param>
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
    public static FieldBuilder<TModel, IEnumerable<TValue>> AsMultiSelect<TModel, TValue>(
        this FieldBuilder<TModel, IEnumerable<TValue>> builder,
        params (TValue value, string label)[] options) where TModel : new()
    {
        var selectOptions = options.Select(o => new SelectOption<TValue>(o.value, o.label));
        return builder.WithAttribute("MultiSelectOptions", selectOptions);
    }
    
    /// <summary>
    /// Configures a field for file uploads with size and type restrictions.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for an IBrowserFile field.</param>
    /// <param name="maxFileSize">Maximum file size in bytes (default: 10MB).</param>
    /// <param name="acceptedFileTypes">Comma-separated list of accepted file extensions (default: ".jpg,.jpeg,.png,.pdf").</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.ProfileImage)
    ///     .AsFileUpload(maxFileSize: 5 * 1024 * 1024, acceptedFileTypes: ".jpg,.png")
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, IBrowserFile?> AsFileUpload<TModel>(
        this FieldBuilder<TModel, IBrowserFile?> builder,
        long maxFileSize = 10 * 1024 * 1024,
        string acceptedFileTypes = ".jpg,.jpeg,.png,.pdf") where TModel : new()
    {
        return builder
            .WithAttribute("MaxFileSize", maxFileSize)
            .WithAttribute("AcceptedFileTypes", acceptedFileTypes);
    }
    
    /// <summary>
    /// Configures a numeric field to be rendered as a slider with min/max values.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The numeric type of the field value.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a numeric field.</param>
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
    public static FieldBuilder<TModel, TValue> AsSlider<TModel, TValue>(
        this FieldBuilder<TModel, TValue> builder,
        TValue min,
        TValue max,
        TValue step,
        bool showValue = true) where TModel : new() where TValue : struct, IComparable<TValue>
    {
        return builder
            .WithAttribute("UseSlider", true)
            .WithAttribute("Min", min)
            .WithAttribute("Max", max)
            .WithAttribute("Step", step)
            .WithAttribute("ShowValue", showValue);
    }
    
    /// <summary>
    /// Adds email format validation to a string field.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <param name="errorMessage">Custom error message (default: "Please enter a valid email address").</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Email)
    ///     .WithEmailValidation("Invalid email format")
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> WithEmailValidation<TModel>(
        this FieldBuilder<TModel, string> builder,
        string? errorMessage = null) where TModel : new()
    {
        return builder.WithValidator(
            value => IsValidEmail(value),
            errorMessage ?? "Please enter a valid email address");
    }
    
    /// <summary>
    /// Adds minimum length validation to a string field.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <param name="minLength">Minimum required character length.</param>
    /// <param name="errorMessage">Custom error message (default: "Must be at least {minLength} characters long").</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Username)
    ///     .WithMinLength(3, "Username too short")
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> WithMinLength<TModel>(
        this FieldBuilder<TModel, string> builder,
        int minLength,
        string? errorMessage = null) where TModel : new()
    {
        return builder.WithValidator(
            value => string.IsNullOrEmpty(value) || value.Length >= minLength,
            errorMessage ?? $"Must be at least {minLength} characters long");
    }
    
    /// <summary>
    /// Adds maximum length validation to a string field.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <param name="maxLength">Maximum allowed character length.</param>
    /// <param name="errorMessage">Custom error message (default: "Must be no more than {maxLength} characters long").</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Bio)
    ///     .WithMaxLength(500, "Bio too long")
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> WithMaxLength<TModel>(
        this FieldBuilder<TModel, string> builder,
        int maxLength,
        string? errorMessage = null) where TModel : new()
    {
        return builder.WithValidator(
            value => string.IsNullOrEmpty(value) || value.Length <= maxLength,
            errorMessage ?? $"Must be no more than {maxLength} characters long");
    }
    
    /// <summary>
    /// Adds range validation to a field with comparable values.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value that implements IComparable.</typeparam>
    /// <param name="builder">The FieldBuilder instance.</param>
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
    public static FieldBuilder<TModel, TValue> WithRange<TModel, TValue>(
        this FieldBuilder<TModel, TValue> builder,
        TValue min,
        TValue max,
        string? errorMessage = null) where TModel : new() where TValue : IComparable<TValue>
    {
        return builder.WithValidator(
            value => value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0,
            errorMessage ?? $"Must be between {min} and {max}");
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

/// <summary>
/// Represents an option in a select list or dropdown field.
/// </summary>
/// <typeparam name="T">The type of the option value.</typeparam>
public class SelectOption<T>
{
    /// <summary>
    /// Gets or sets the value that will be bound to the model when this option is selected.
    /// </summary>
    public T Value { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the display text shown to the user for this option.
    /// </summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Initializes a new instance of the SelectOption class.
    /// </summary>
    public SelectOption() { }
    
    /// <summary>
    /// Initializes a new instance of the SelectOption class with a value and label.
    /// </summary>
    /// <param name="value">The value for this option.</param>
    /// <param name="label">The display label for this option.</param>
    public SelectOption(T value, string label)
    {
        Value = value;
        Label = label;
    }
}