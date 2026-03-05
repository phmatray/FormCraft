namespace FormCraft;

/// <summary>
/// Validates collection fields by checking item count constraints and recursively validating each item
/// using the item form configuration's validators.
/// </summary>
/// <typeparam name="TModel">The parent model type.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
public class CollectionFieldValidator<TModel, TItem>
    where TModel : new()
    where TItem : new()
{
    private readonly ICollectionFieldConfiguration<TModel, TItem> _configuration;

    /// <summary>
    /// Initializes a new instance of the CollectionFieldValidator class.
    /// </summary>
    /// <param name="configuration">The collection field configuration to validate against.</param>
    public CollectionFieldValidator(ICollectionFieldConfiguration<TModel, TItem> configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Validates the collection field including item count and per-item field validation.
    /// </summary>
    /// <param name="model">The parent model instance.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>A list of validation error messages. Empty if validation passed.</returns>
    public async Task<List<string>> ValidateAsync(TModel model, IServiceProvider services)
    {
        var errors = new List<string>();
        var items = _configuration.CollectionAccessor(model);
        var itemCount = items?.Count ?? 0;

        // Validate min items
        if (_configuration.MinItems > 0 && itemCount < _configuration.MinItems)
        {
            errors.Add($"{_configuration.Label ?? _configuration.FieldName} requires at least {_configuration.MinItems} item(s).");
        }

        // Validate max items
        if (_configuration.MaxItems > 0 && itemCount > _configuration.MaxItems)
        {
            errors.Add($"{_configuration.Label ?? _configuration.FieldName} allows at most {_configuration.MaxItems} item(s).");
        }

        // Validate individual items using the item form configuration
        if (items != null && _configuration.ItemFormConfiguration != null)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                foreach (var field in _configuration.ItemFormConfiguration.Fields)
                {
                    var getter = field.ValueExpression.Compile();
                    var value = getter(item);

                    foreach (var validator in field.Validators)
                    {
                        var result = await validator.ValidateAsync(item, value, services);
                        if (!result.IsValid)
                        {
                            errors.Add($"{_configuration.Label ?? _configuration.FieldName} [{i + 1}] - {field.Label ?? field.FieldName}: {result.ErrorMessage}");
                        }
                    }
                }
            }
        }

        return errors;
    }
}
