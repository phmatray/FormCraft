namespace FormCraft;

/// <summary>
/// Provides extension methods for working with form configurations, including field analysis and filtering.
/// </summary>
public static class FormConfigurationExtensions
{
    /// <param name="configuration">The form configuration to analyze.</param>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    extension<TModel>(IFormConfiguration<TModel> configuration) where TModel : new()
    {
        /// <summary>
        /// Gets all required field names from the form configuration.
        /// </summary>
        /// <returns>An enumerable of field names that are marked as required.</returns>
        /// <example>
        /// <code>
        /// var requiredFields = formConfiguration.GetRequiredFields();
        /// foreach (var fieldName in requiredFields)
        /// {
        ///     Console.WriteLine($"Required field: {fieldName}");
        /// }
        /// </code>
        /// </example>
        public IEnumerable<string> GetRequiredFields()
        {
            return configuration.Fields
                .Where(f => f.IsRequired)
                .Select(f => f.FieldName);
        }

        /// <summary>
        /// Gets all visible field names from the form configuration for a given model instance.
        /// This method evaluates visibility conditions and returns only fields that should be displayed.
        /// </summary>
        /// <param name="model">The model instance to evaluate visibility conditions against.</param>
        /// <returns>An enumerable of field names that should be visible for the given model state.</returns>
        /// <example>
        /// <code>
        /// var visibleFields = formConfiguration.GetVisibleFields(myModel);
        /// foreach (var fieldName in visibleFields)
        /// {
        ///     Console.WriteLine($"Visible field: {fieldName}");
        /// }
        /// </code>
        /// </example>
        public IEnumerable<string> GetVisibleFields(TModel model)
        {
            return configuration.Fields
                .Where(f => f.VisibilityCondition?.Invoke(model) ?? f.IsVisible)
                .Select(f => f.FieldName);
        }
    }
}