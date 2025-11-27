using Microsoft.Extensions.Logging;

namespace FormCraft;

/// <summary>
/// Processes field mappings when an LOV item is selected.
/// Applies both sync and async mappings to populate model properties.
/// </summary>
public interface ILovMappingProcessor
{
    /// <summary>
    /// Applies all field mappings from the selected item to the model.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="model">The model to update.</param>
    /// <param name="selectedItem">The selected LOV item.</param>
    /// <param name="mappings">The list of mappings to apply.</param>
    void ApplyMappings<TModel>(
        TModel model,
        object selectedItem,
        IReadOnlyList<ILovFieldMapping> mappings)
        where TModel : class;

    /// <summary>
    /// Applies all field mappings asynchronously, including async mappings.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="model">The model to update.</param>
    /// <param name="selectedItem">The selected LOV item.</param>
    /// <param name="mappings">The list of mappings to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ApplyMappingsAsync<TModel>(
        TModel model,
        object selectedItem,
        IReadOnlyList<ILovFieldMapping> mappings,
        CancellationToken cancellationToken = default)
        where TModel : class;

    /// <summary>
    /// Clears all mapped fields on the model.
    /// Called when the LOV selection is cleared.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="model">The model to clear.</param>
    /// <param name="mappings">The list of mappings whose target fields to clear.</param>
    void ClearMappings<TModel>(
        TModel model,
        IReadOnlyList<ILovFieldMapping> mappings)
        where TModel : class;
}

/// <summary>
/// Default implementation of ILovMappingProcessor.
/// </summary>
public class LovMappingProcessor : ILovMappingProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LovMappingProcessor>? _logger;

    /// <summary>
    /// Initializes a new instance of the LovMappingProcessor class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for async mappings.</param>
    /// <param name="logger">Optional logger for error tracking.</param>
    public LovMappingProcessor(
        IServiceProvider serviceProvider,
        ILogger<LovMappingProcessor>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public void ApplyMappings<TModel>(
        TModel model,
        object selectedItem,
        IReadOnlyList<ILovFieldMapping> mappings)
        where TModel : class
    {
        if (model == null || selectedItem == null || mappings == null || mappings.Count == 0)
        {
            return;
        }

        foreach (var mapping in mappings)
        {
            try
            {
                mapping.Apply(selectedItem, model);

                _logger?.LogDebug(
                    "Applied LOV mapping: {Source} -> {Target}",
                    mapping.SourceProperty,
                    mapping.TargetProperty);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Failed to apply LOV mapping: {Source} -> {Target}",
                    mapping.SourceProperty,
                    mapping.TargetProperty);
            }
        }
    }

    /// <inheritdoc />
    public async Task ApplyMappingsAsync<TModel>(
        TModel model,
        object selectedItem,
        IReadOnlyList<ILovFieldMapping> mappings,
        CancellationToken cancellationToken = default)
        where TModel : class
    {
        if (model == null || selectedItem == null || mappings == null || mappings.Count == 0)
        {
            return;
        }

        // First, apply all sync mappings
        var syncMappings = mappings.Where(m => !m.IsAsync).ToList();
        ApplyMappings(model, selectedItem, syncMappings);

        // Then, apply async mappings
        var asyncMappings = mappings.Where(m => m.IsAsync).OfType<IAsyncLovFieldMapping>().ToList();

        foreach (var mapping in asyncMappings)
        {
            try
            {
                await mapping.ApplyAsync(selectedItem, model, _serviceProvider, cancellationToken);

                _logger?.LogDebug(
                    "Applied async LOV mapping: {Source} -> {Target}",
                    mapping.SourceProperty,
                    mapping.TargetProperty);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Failed to apply async LOV mapping: {Source} -> {Target}",
                    mapping.SourceProperty,
                    mapping.TargetProperty);
            }
        }
    }

    /// <inheritdoc />
    public void ClearMappings<TModel>(
        TModel model,
        IReadOnlyList<ILovFieldMapping> mappings)
        where TModel : class
    {
        if (model == null || mappings == null || mappings.Count == 0)
        {
            return;
        }

        var modelType = typeof(TModel);

        foreach (var mapping in mappings)
        {
            try
            {
                var property = modelType.GetProperty(mapping.TargetProperty);
                if (property != null && property.CanWrite)
                {
                    var defaultValue = GetDefaultValue(property.PropertyType);
                    property.SetValue(model, defaultValue);

                    _logger?.LogDebug(
                        "Cleared LOV mapped field: {Target}",
                        mapping.TargetProperty);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Failed to clear LOV mapped field: {Target}",
                    mapping.TargetProperty);
            }
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        // For reference types, return null
        return null;
    }
}
