using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// Extension methods for <see cref="FormBuilder{TModel}"/> that generate form fields
/// automatically from the model's public read-write properties, without requiring
/// any attributes or per-field configuration.
/// </summary>
/// <remarks>
/// <para>
/// The method is named <c>AddFieldsAuto</c> to sit next to the existing
/// <c>AddFieldsFromAttributes()</c> in IntelliSense (both share the <c>AddFields</c>
/// prefix) and to follow the repository's fluent naming rule that <c>Add*</c>
/// methods add items to the form.
/// </para>
/// <para>Type mapping:</para>
/// <list type="bullet">
/// <item><description><c>string</c> → text input. Property names containing "Email" get the email input type plus format validation; names containing "Password" get the password input type.</description></item>
/// <item><description><c>int</c>, <c>long</c>, <c>short</c>, <c>byte</c>, <c>decimal</c>, <c>double</c>, <c>float</c> → numeric input.</description></item>
/// <item><description><c>bool</c> → checkbox.</description></item>
/// <item><description><c>DateTime</c>, <c>DateOnly</c> → date picker; <c>TimeOnly</c> → time picker.</description></item>
/// <item><description>Enums → select populated with the enum values (humanized labels).</description></item>
/// <item><description><c>IBrowserFile</c> / <c>IReadOnlyList&lt;IBrowserFile&gt;</c> → file upload.</description></item>
/// <item><description>Nullable variants of all of the above are supported.</description></item>
/// </list>
/// <para>
/// Indexers, read-only or write-only properties, complex objects, collections of
/// complex types, and properties marked with <see cref="ExcludeFieldAttribute"/> are skipped.
/// DataAnnotations (<c>[Required]</c>, <c>[Range]</c>, <c>[MinLength]</c>, <c>[MaxLength]</c>,
/// <c>[StringLength]</c>, <c>[EmailAddress]</c>, <c>[Display(Name = ...)]</c>) are honored
/// when present, but none are required.
/// </para>
/// </remarks>
public static class AutoFormBuilderExtensions
{
    private static readonly MethodInfo AddAutoFieldMethod = typeof(AutoFormBuilderExtensions)
        .GetMethod(nameof(AddAutoField), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <param name="builder">The form builder to configure.</param>
    /// <typeparam name="TModel">The model type to scan for properties.</typeparam>
    extension<TModel>(FormBuilder<TModel> builder) where TModel : new()
    {
        /// <summary>
        /// Scans <typeparamref name="TModel"/> for public read-write properties and
        /// generates sensible form fields for each supported property type, without
        /// requiring any attributes. Labels are humanized from PascalCase property
        /// names ("FirstName" → "First Name") and DataAnnotations are honored when present.
        /// </summary>
        /// <param name="configure">Optional callback to include/exclude properties or customize individual generated fields.</param>
        /// <returns>The builder instance for chaining.</returns>
        /// <example>
        /// <code>
        /// // Zero configuration:
        /// var config = FormBuilder&lt;CustomerModel&gt;.Create()
        ///     .AddFieldsAuto()
        ///     .Build();
        ///
        /// // With customization:
        /// var config = FormBuilder&lt;CustomerModel&gt;.Create()
        ///     .AddFieldsAuto(options => options
        ///         .Exclude(x => x.Id)
        ///         .ConfigureField(x => x.Name, field => field.Required()))
        ///     .Build();
        /// </code>
        /// </example>
        public FormBuilder<TModel> AddFieldsAuto(Action<AutoFieldsOptions<TModel>>? configure = null)
        {
            var options = new AutoFieldsOptions<TModel>();
            configure?.Invoke(options);

            foreach (var prop in typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!IsCandidate(prop, options))
                {
                    continue;
                }

                AddAutoFieldMethod
                    .MakeGenericMethod(typeof(TModel), prop.PropertyType)
                    .Invoke(null, [builder, prop, options]);
            }

            return builder;
        }
    }

    private static bool IsCandidate<TModel>(PropertyInfo prop, AutoFieldsOptions<TModel> options)
        where TModel : new()
    {
        // Indexers and properties that cannot be both read and written are skipped.
        if (prop.GetIndexParameters().Length > 0)
        {
            return false;
        }

        if (prop.GetGetMethod() == null || prop.GetSetMethod() == null)
        {
            return false;
        }

        if (prop.GetCustomAttribute<ExcludeFieldAttribute>() != null)
        {
            return false;
        }

        if (options.ExcludedProperties.Contains(prop.Name))
        {
            return false;
        }

        if (options.IncludedProperties.Count > 0 && !options.IncludedProperties.Contains(prop.Name))
        {
            return false;
        }

        return IsSupportedFieldType(prop.PropertyType);
    }

    private static bool IsSupportedFieldType(Type type)
    {
        if (type == typeof(string))
        {
            return true;
        }

        // File uploads are the only supported collection-like types.
        if (type == typeof(IBrowserFile) || type == typeof(IReadOnlyList<IBrowserFile>))
        {
            return true;
        }

        // All other collections (arrays, lists of complex types, etc.) are skipped.
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsEnum
               || IsNumericType(underlyingType)
               || underlyingType == typeof(bool)
               || underlyingType == typeof(DateTime)
               || underlyingType == typeof(DateOnly)
               || underlyingType == typeof(TimeOnly);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(decimal) || type == typeof(double) ||
               type == typeof(float);
    }

    private static void AddAutoField<TModel, TValue>(
        FormBuilder<TModel> builder,
        PropertyInfo prop,
        AutoFieldsOptions<TModel> options)
        where TModel : new()
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var lambda = Expression.Lambda<Func<TModel, TValue>>(Expression.Property(parameter, prop), parameter);

        builder.AddField(lambda, field =>
        {
            var label = GetLabel(prop);
            field.WithLabel(label);

            ApplyTypeDefaults(field, prop);
            ApplyDataAnnotations(field, prop, label);

            if (options.FieldConfigurators.TryGetValue(prop.Name, out var configurator) &&
                configurator is Action<FieldBuilder<TModel, TValue>> typedConfigurator)
            {
                typedConfigurator(field);
            }
        });
    }

    private static void ApplyTypeDefaults<TModel, TValue>(FieldBuilder<TModel, TValue> field, PropertyInfo prop)
        where TModel : new()
    {
        var underlyingType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

        if (underlyingType == typeof(string))
        {
            if (prop.Name.Contains("Password", StringComparison.OrdinalIgnoreCase))
            {
                field.WithInputType("password");
            }
            else if (prop.Name.Contains("Email", StringComparison.OrdinalIgnoreCase))
            {
                field.WithInputType("email");
                field.WithEmailValidation();
            }
            else
            {
                field.WithInputType("text");
            }
        }
        else if (IsNumericType(underlyingType))
        {
            field.WithInputType("number");
        }
        else if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateOnly))
        {
            field.WithInputType("date");
        }
        else if (underlyingType == typeof(TimeOnly))
        {
            field.WithInputType("time");
        }
        else if (underlyingType.IsEnum)
        {
            field.WithSelectOptions(BuildEnumOptions<TValue>(underlyingType));
        }
        else if (field is FieldBuilder<TModel, IBrowserFile> fileField)
        {
            fileField.AsFileUpload();
        }
        else if (field is FieldBuilder<TModel, IReadOnlyList<IBrowserFile>> multiFileField)
        {
            multiFileField.AsMultipleFileUpload();
        }

        // bool needs no input type: the boolean renderer produces a checkbox.
    }

    private static List<SelectOption<TValue>> BuildEnumOptions<TValue>(Type enumType)
    {
        var optionsList = new List<SelectOption<TValue>>();

        foreach (var value in Enum.GetValues(enumType))
        {
            optionsList.Add(new SelectOption<TValue>((TValue)value, Humanize(value.ToString()!)));
        }

        return optionsList;
    }

    private static void ApplyDataAnnotations<TModel, TValue>(
        FieldBuilder<TModel, TValue> field,
        PropertyInfo prop,
        string label)
        where TModel : new()
    {
        var required = prop.GetCustomAttribute<RequiredAttribute>();
        if (required != null)
        {
            field.Required(required.ErrorMessage ?? $"{label} is required");
        }

        var emailAddress = prop.GetCustomAttribute<EmailAddressAttribute>();
        if (emailAddress != null && typeof(TValue) == typeof(string))
        {
            field.WithInputType("email");
            field.WithEmailValidation(emailAddress.ErrorMessage);
        }

        if (field is FieldBuilder<TModel, string> stringField)
        {
            var minLength = prop.GetCustomAttribute<MinLengthAttribute>();
            if (minLength != null)
            {
                stringField.WithMinLength(minLength.Length,
                    minLength.ErrorMessage ?? $"Must be at least {minLength.Length} characters");
            }

            var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>();
            if (maxLength != null)
            {
                stringField.WithMaxLength(maxLength.Length,
                    maxLength.ErrorMessage ?? $"Must be no more than {maxLength.Length} characters");
            }

            var stringLength = prop.GetCustomAttribute<StringLengthAttribute>();
            if (stringLength != null)
            {
                if (stringLength.MinimumLength > 0)
                {
                    stringField.WithMinLength(stringLength.MinimumLength,
                        stringLength.ErrorMessage ?? $"Must be at least {stringLength.MinimumLength} characters");
                }

                stringField.WithMaxLength(stringLength.MaximumLength,
                    stringLength.ErrorMessage ?? $"Must be no more than {stringLength.MaximumLength} characters");
            }
        }

        var range = prop.GetCustomAttribute<RangeAttribute>();
        if (range != null)
        {
            field.WithAttribute("min", range.Minimum);
            field.WithAttribute("max", range.Maximum);
            field.WithValidator(value => value == null || range.IsValid(value),
                range.ErrorMessage ?? $"Must be between {range.Minimum} and {range.Maximum}");
        }
    }

    private static string GetLabel(PropertyInfo prop)
    {
        var display = prop.GetCustomAttribute<DisplayAttribute>();
        var displayName = display?.GetName();

        return string.IsNullOrWhiteSpace(displayName) ? Humanize(prop.Name) : displayName;
    }

    /// <summary>
    /// Converts a PascalCase identifier into a human-readable label:
    /// "FirstName" → "First Name", "SSNNumber" → "SSN Number", "Address1" → "Address 1".
    /// </summary>
    private static string Humanize(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = new StringBuilder(name.Length + 4);
        result.Append(name[0]);

        for (var i = 1; i < name.Length; i++)
        {
            var current = name[i];
            var previous = name[i - 1];
            var startsNewWord =
                (char.IsUpper(current) && (char.IsLower(previous) ||
                                           (i + 1 < name.Length && char.IsLower(name[i + 1]) && char.IsLetter(previous)))) ||
                (char.IsDigit(current) && char.IsLetter(previous)) ||
                (char.IsLetter(current) && char.IsDigit(previous));

            if (startsNewWord && previous != ' ')
            {
                result.Append(' ');
            }

            result.Append(current);
        }

        return result.ToString();
    }
}
