using DynamicFormBlazor.Forms.Core;

namespace DynamicFormBlazor.Forms.Extensions;

public static class FormConfigurationExtensions
{
    /// <summary>
    /// Gets all required field names from the configuration
    /// </summary>
    public static IEnumerable<string> GetRequiredFields<TModel>(this IFormConfiguration<TModel> configuration)
    {
        return configuration.Fields
            .Where(f => f.IsRequired)
            .Select(f => f.FieldName);
    }
    
    /// <summary>
    /// Gets all visible field names from the configuration for a given model
    /// </summary>
    public static IEnumerable<string> GetVisibleFields<TModel>(this IFormConfiguration<TModel> configuration, TModel model)
    {
        return configuration.Fields
            .Where(f => f.VisibilityCondition?.Invoke(model) ?? f.IsVisible)
            .Select(f => f.FieldName);
    }
}