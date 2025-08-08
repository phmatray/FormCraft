using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace FormCraft;

/// <summary>
/// Extension methods for <see cref="FormBuilder{TModel}"/> that allow
/// configuring fields based on attributes applied to model properties.
/// </summary>
public static class AttributeFormBuilderExtensions
{
    /// <summary>
    /// Scans <typeparamref name="TModel"/> for properties decorated with
    /// form field attributes and adds corresponding fields to the builder.
    /// Supports multiple field types including text, number, email, date,
    /// select, checkbox, and textarea fields along with validation attributes.
    /// </summary>
    /// <typeparam name="TModel">The model type containing the annotated properties.</typeparam>
    /// <param name="builder">The form builder to configure.</param>
    /// <returns>The builder instance for chaining.</returns>
    public static FormBuilder<TModel> AddFieldsFromAttributes<TModel>(this FormBuilder<TModel> builder)
        where TModel : new()
    {
        foreach (var prop in typeof(TModel).GetProperties())
        {
            // Handle TextFieldAttribute
            var textAttr = prop.GetCustomAttribute<TextFieldAttribute>();
            if (textAttr != null && prop.PropertyType == typeof(string))
            {
                AddStringField(builder, prop, textAttr.Label, textAttr.Placeholder, "text");
                continue;
            }

            // Handle EmailFieldAttribute
            var emailAttr = prop.GetCustomAttribute<EmailFieldAttribute>();
            if (emailAttr != null && prop.PropertyType == typeof(string))
            {
                var parameter = Expression.Parameter(typeof(TModel), "x");
                var property = Expression.Property(parameter, prop);
                var lambda = Expression.Lambda<Func<TModel, string>>(property, parameter);

                builder.AddField(lambda, field =>
                {
                    field.WithLabel(emailAttr.Label)
                         .WithInputType("email");
                    
                    if (!string.IsNullOrEmpty(emailAttr.Placeholder))
                        field.WithPlaceholder(emailAttr.Placeholder);

                    if (emailAttr.ValidateFormat)
                    {
                        field.WithValidator(value => 
                            !string.IsNullOrEmpty(value) && value.Contains("@") && value.Contains("."),
                            "Please enter a valid email address");
                    }

                    ApplyValidationAttributes(field, prop, emailAttr.Label);
                });
                continue;
            }

            // Handle TextAreaAttribute
            var textAreaAttr = prop.GetCustomAttribute<TextAreaAttribute>();
            if (textAreaAttr != null && prop.PropertyType == typeof(string))
            {
                var parameter = Expression.Parameter(typeof(TModel), "x");
                var property = Expression.Property(parameter, prop);
                var lambda = Expression.Lambda<Func<TModel, string>>(property, parameter);

                builder.AddField(lambda, field =>
                {
                    field.WithLabel(textAreaAttr.Label);
                    
                    if (!string.IsNullOrEmpty(textAreaAttr.Placeholder))
                        field.WithPlaceholder(textAreaAttr.Placeholder);

                    field.WithAttribute("rows", textAreaAttr.Rows);
                    
                    if (textAreaAttr.MaxLength.HasValue)
                    {
                        field.WithMaxLength(textAreaAttr.MaxLength.Value, 
                            $"Must be no more than {textAreaAttr.MaxLength.Value} characters");
                    }

                    if (textAreaAttr.AutoResize)
                        field.WithAttribute("auto-resize", true);

                    ApplyValidationAttributes(field, prop, textAreaAttr.Label);
                });
                continue;
            }

            // Handle NumberFieldAttribute
            var numberAttr = prop.GetCustomAttribute<NumberFieldAttribute>();
            if (numberAttr != null && IsNumericType(prop.PropertyType))
            {
                AddNumericField(builder, prop, numberAttr);
                continue;
            }

            // Handle DateFieldAttribute
            var dateAttr = prop.GetCustomAttribute<DateFieldAttribute>();
            if (dateAttr != null && (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?)))
            {
                AddDateField(builder, prop, dateAttr);
                continue;
            }

            // Handle CheckboxFieldAttribute
            var checkboxAttr = prop.GetCustomAttribute<CheckboxFieldAttribute>();
            if (checkboxAttr != null && prop.PropertyType == typeof(bool))
            {
                var parameter = Expression.Parameter(typeof(TModel), "x");
                var property = Expression.Property(parameter, prop);
                var lambda = Expression.Lambda<Func<TModel, bool>>(property, parameter);

                builder.AddField(lambda, field =>
                {
                    field.WithLabel(checkboxAttr.Label);
                    
                    if (!string.IsNullOrEmpty(checkboxAttr.Text))
                        field.WithAttribute("text", checkboxAttr.Text);
                    
                    if (checkboxAttr.DefaultChecked)
                        field.WithAttribute("default-checked", true);
                });
                continue;
            }

            // Handle SelectFieldAttribute
            var selectAttr = prop.GetCustomAttribute<SelectFieldAttribute>();
            if (selectAttr != null)
            {
                AddSelectField(builder, prop, selectAttr);
                continue;
            }
        }

        return builder;
    }

    private static void AddStringField<TModel>(FormBuilder<TModel> builder, PropertyInfo prop, 
        string label, string? placeholder, string inputType) where TModel : new()
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, prop);
        var lambda = Expression.Lambda<Func<TModel, string>>(property, parameter);

        builder.AddField(lambda, field =>
        {
            field.WithLabel(label)
                 .WithInputType(inputType);
            
            if (!string.IsNullOrEmpty(placeholder))
                field.WithPlaceholder(placeholder);

            ApplyValidationAttributes(field, prop, label);
        });
    }

    private static void AddNumericField<TModel>(FormBuilder<TModel> builder, PropertyInfo prop, 
        NumberFieldAttribute numberAttr) where TModel : new()
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, prop);
        
        // Handle different numeric types
        if (prop.PropertyType == typeof(int))
        {
            var lambda = Expression.Lambda<Func<TModel, int>>(property, parameter);
            builder.AddField(lambda, field => ConfigureNumericField(field, numberAttr, prop));
        }
        else if (prop.PropertyType == typeof(decimal))
        {
            var lambda = Expression.Lambda<Func<TModel, decimal>>(property, parameter);
            builder.AddField(lambda, field => ConfigureNumericField(field, numberAttr, prop));
        }
        else if (prop.PropertyType == typeof(double))
        {
            var lambda = Expression.Lambda<Func<TModel, double>>(property, parameter);
            builder.AddField(lambda, field => ConfigureNumericField(field, numberAttr, prop));
        }
        else if (prop.PropertyType == typeof(float))
        {
            var lambda = Expression.Lambda<Func<TModel, float>>(property, parameter);
            builder.AddField(lambda, field => ConfigureNumericField(field, numberAttr, prop));
        }
        else if (prop.PropertyType == typeof(long))
        {
            var lambda = Expression.Lambda<Func<TModel, long>>(property, parameter);
            builder.AddField(lambda, field => ConfigureNumericField(field, numberAttr, prop));
        }
    }

    private static void ConfigureNumericField<TModel, TValue>(FieldBuilder<TModel, TValue> field, 
        NumberFieldAttribute numberAttr, PropertyInfo prop) where TModel : new()
    {
        field.WithLabel(numberAttr.Label)
             .WithInputType("number");
        
        if (!string.IsNullOrEmpty(numberAttr.Placeholder))
            field.WithPlaceholder(numberAttr.Placeholder);
        
        if (numberAttr.Min.HasValue)
            field.WithAttribute("min", numberAttr.Min.Value);
        
        if (numberAttr.Max.HasValue)
            field.WithAttribute("max", numberAttr.Max.Value);
        
        if (numberAttr.Step.HasValue)
            field.WithAttribute("step", numberAttr.Step.Value);
        
        ApplyValidationAttributes(field, prop, numberAttr.Label);
    }

    private static void AddDateField<TModel>(FormBuilder<TModel> builder, PropertyInfo prop, 
        DateFieldAttribute dateAttr) where TModel : new()
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, prop);
        
        if (prop.PropertyType == typeof(DateTime))
        {
            var lambda = Expression.Lambda<Func<TModel, DateTime>>(property, parameter);
            builder.AddField(lambda, field => ConfigureDateField(field, dateAttr, prop));
        }
        else if (prop.PropertyType == typeof(DateTime?))
        {
            var lambda = Expression.Lambda<Func<TModel, DateTime?>>(property, parameter);
            builder.AddField(lambda, field => ConfigureDateField(field, dateAttr, prop));
        }
    }

    private static void ConfigureDateField<TModel, TValue>(FieldBuilder<TModel, TValue> field, 
        DateFieldAttribute dateAttr, PropertyInfo prop) where TModel : new()
    {
        field.WithLabel(dateAttr.Label)
             .WithInputType("date");
        
        if (!string.IsNullOrEmpty(dateAttr.Placeholder))
            field.WithPlaceholder(dateAttr.Placeholder);
        
        if (dateAttr.MinDate.HasValue)
            field.WithAttribute("min", dateAttr.MinDate.Value.ToString("yyyy-MM-dd"));
        
        if (dateAttr.MaxDate.HasValue)
            field.WithAttribute("max", dateAttr.MaxDate.Value.ToString("yyyy-MM-dd"));
        
        if (!string.IsNullOrEmpty(dateAttr.Format))
            field.WithAttribute("format", dateAttr.Format);
        
        ApplyValidationAttributes(field, prop, dateAttr.Label);
    }

    private static void AddSelectField<TModel>(FormBuilder<TModel> builder, PropertyInfo prop, 
        SelectFieldAttribute selectAttr) where TModel : new()
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, prop);
        
        // Create lambda expression based on property type
        var lambdaType = typeof(Func<,>).MakeGenericType(typeof(TModel), prop.PropertyType);
        var lambda = Expression.Lambda(lambdaType, property, parameter);
        
        // Use reflection to call AddField with the correct type
        var addFieldMethod = typeof(FormBuilder<TModel>).GetMethod("AddField")!
            .MakeGenericMethod(prop.PropertyType);
        
        var fieldBuilder = addFieldMethod.Invoke(builder, new object[] { lambda, 
            (Action<object>)(fieldObj => 
            {
                dynamic field = fieldObj;
                field.WithLabel(selectAttr.Label);
                
                if (!string.IsNullOrEmpty(selectAttr.Placeholder))
                    field.WithPlaceholder(selectAttr.Placeholder);
                
                if (selectAttr.Options != null && selectAttr.Options.Length > 0)
                    field.WithAttribute("options", selectAttr.Options);
                
                if (selectAttr.AllowMultiple)
                    field.WithAttribute("multiple", true);
                
                if (!string.IsNullOrEmpty(selectAttr.OptionsProviderName))
                    field.WithAttribute("options-provider", selectAttr.OptionsProviderName);
                
                ApplyValidationAttributesDynamic(field, prop, selectAttr.Label);
            })
        });
    }

    private static void ApplyValidationAttributes<TModel, TValue>(FieldBuilder<TModel, TValue> field, 
        PropertyInfo prop, string label) where TModel : new()
    {
        var required = prop.GetCustomAttribute<RequiredAttribute>();
        if (required != null)
            field.Required(required.ErrorMessage ?? $"{label} is required");

        // Only apply string-specific validations for string fields
        if (typeof(TValue) == typeof(string))
        {
            var stringField = field as FieldBuilder<TModel, string>;
            if (stringField != null)
            {
                var minLength = prop.GetCustomAttribute<MinLengthAttribute>();
                if (minLength != null)
                    stringField.WithMinLength(minLength.Length, minLength.ErrorMessage ?? $"Must be at least {minLength.Length} characters");

                var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLength != null)
                    stringField.WithMaxLength(maxLength.Length, maxLength.ErrorMessage ?? $"Must be no more than {maxLength.Length} characters");
            }
        }

        var range = prop.GetCustomAttribute<RangeAttribute>();
        if (range != null)
        {
            field.WithAttribute("min", range.Minimum);
            field.WithAttribute("max", range.Maximum);
        }

        var pattern = prop.GetCustomAttribute<RegularExpressionAttribute>();
        if (pattern != null)
        {
            field.WithAttribute("pattern", pattern.Pattern);
            field.WithValidator(value => 
            {
                if (value == null) return true;
                return System.Text.RegularExpressions.Regex.IsMatch(value.ToString()!, pattern.Pattern);
            }, pattern.ErrorMessage ?? "Invalid format");
        }
    }

    private static void ApplyValidationAttributesDynamic(dynamic field, PropertyInfo prop, string label)
    {
        var required = prop.GetCustomAttribute<RequiredAttribute>();
        if (required != null)
            field.Required(required.ErrorMessage ?? $"{label} is required");

        var minLength = prop.GetCustomAttribute<MinLengthAttribute>();
        if (minLength != null && prop.PropertyType == typeof(string))
            field.WithMinLength(minLength.Length, minLength.ErrorMessage ?? $"Must be at least {minLength.Length} characters");

        var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>();
        if (maxLength != null && prop.PropertyType == typeof(string))
            field.WithMaxLength(maxLength.Length, maxLength.ErrorMessage ?? $"Must be no more than {maxLength.Length} characters");
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(decimal) || type == typeof(double) ||
               type == typeof(float) || type == typeof(long) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
               type == typeof(ushort) || type == typeof(sbyte);
    }
}