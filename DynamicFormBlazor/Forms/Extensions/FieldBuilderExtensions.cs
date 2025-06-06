using DynamicFormBlazor.Forms.Builders;
using Microsoft.AspNetCore.Components.Forms;

namespace DynamicFormBlazor.Forms.Extensions;

public static class FieldBuilderExtensions
{
    // Text Area Extensions
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
    
    // Select Extensions
    public static FieldBuilder<TModel, TValue> WithOptions<TModel, TValue>(
        this FieldBuilder<TModel, TValue> builder,
        params (TValue value, string label)[] options) where TModel : new()
    {
        var selectOptions = options.Select(o => new SelectOption<TValue>(o.value, o.label));
        return builder.WithAttribute("Options", selectOptions);
    }
    
    // Multi-Select Extensions
    public static FieldBuilder<TModel, IEnumerable<TValue>> AsMultiSelect<TModel, TValue>(
        this FieldBuilder<TModel, IEnumerable<TValue>> builder,
        params (TValue value, string label)[] options) where TModel : new()
    {
        var selectOptions = options.Select(o => new SelectOption<TValue>(o.value, o.label));
        return builder.WithAttribute("MultiSelectOptions", selectOptions);
    }
    
    // File Upload Extensions
    public static FieldBuilder<TModel, IBrowserFile?> AsFileUpload<TModel>(
        this FieldBuilder<TModel, IBrowserFile?> builder,
        long maxFileSize = 10 * 1024 * 1024,
        string acceptedFileTypes = ".jpg,.jpeg,.png,.pdf") where TModel : new()
    {
        return builder
            .WithAttribute("MaxFileSize", maxFileSize)
            .WithAttribute("AcceptedFileTypes", acceptedFileTypes);
    }
    
    // Slider Extensions
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
    
    // Validation Extensions
    public static FieldBuilder<TModel, string> WithEmailValidation<TModel>(
        this FieldBuilder<TModel, string> builder,
        string? errorMessage = null) where TModel : new()
    {
        return builder.WithValidator(
            value => IsValidEmail(value),
            errorMessage ?? "Please enter a valid email address");
    }
    
    public static FieldBuilder<TModel, string> WithMinLength<TModel>(
        this FieldBuilder<TModel, string> builder,
        int minLength,
        string? errorMessage = null) where TModel : new()
    {
        return builder.WithValidator(
            value => string.IsNullOrEmpty(value) || value.Length >= minLength,
            errorMessage ?? $"Must be at least {minLength} characters long");
    }
    
    public static FieldBuilder<TModel, string> WithMaxLength<TModel>(
        this FieldBuilder<TModel, string> builder,
        int maxLength,
        string? errorMessage = null) where TModel : new()
    {
        return builder.WithValidator(
            value => string.IsNullOrEmpty(value) || value.Length <= maxLength,
            errorMessage ?? $"Must be no more than {maxLength} characters long");
    }
    
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

public class SelectOption<T>
{
    public T Value { get; set; } = default!;
    public string Label { get; set; } = string.Empty;
    
    public SelectOption() { }
    
    public SelectOption(T value, string label)
    {
        Value = value;
        Label = label;
    }
}