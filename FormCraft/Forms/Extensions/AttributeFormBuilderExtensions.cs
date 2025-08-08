using System;
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
    /// Currently supports <see cref="TextFieldAttribute"/> for string
    /// properties and common validation attributes like
    /// <see cref="RequiredAttribute"/>, <see cref="MinLengthAttribute"/>,
    /// and <see cref="MaxLengthAttribute"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type containing the annotated properties.</typeparam>
    /// <param name="builder">The form builder to configure.</param>
    /// <returns>The builder instance for chaining.</returns>
    public static FormBuilder<TModel> AddFieldsFromAttributes<TModel>(this FormBuilder<TModel> builder)
        where TModel : new()
    {
        foreach (var prop in typeof(TModel).GetProperties())
        {
            var textAttr = prop.GetCustomAttribute<TextFieldAttribute>();
            if (textAttr != null && prop.PropertyType == typeof(string))
            {
                var parameter = Expression.Parameter(typeof(TModel), "x");
                var property = Expression.Property(parameter, prop);
                var lambda = Expression.Lambda<Func<TModel, string>>(property, parameter);

                builder.AddField(lambda, field =>
                {
                    field.WithLabel(textAttr.Label);
                    if (!string.IsNullOrEmpty(textAttr.Placeholder))
                        field.WithPlaceholder(textAttr.Placeholder);

                    var required = prop.GetCustomAttribute<RequiredAttribute>();
                    if (required != null)
                        field.Required(required.ErrorMessage ?? $"{textAttr.Label} is required");

                    var min = prop.GetCustomAttribute<MinLengthAttribute>();
                    if (min != null)
                        field.WithMinLength(min.Length, min.ErrorMessage ?? $"Must be at least {min.Length} characters");

                    var max = prop.GetCustomAttribute<MaxLengthAttribute>();
                    if (max != null)
                        field.WithMaxLength(max.Length, max.ErrorMessage ?? $"Must be no more than {max.Length} characters");
                });
            }
        }

        return builder;
    }
}
