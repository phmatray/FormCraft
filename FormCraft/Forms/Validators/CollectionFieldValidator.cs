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
        => (await ValidateAllAsync(model, services)).Messages;

    /// <summary>
    /// Validates the collection <b>once</b> and returns both shapes its callers need: the flat,
    /// human-formatted messages for the collection's own field identifier, and the structured
    /// per-item errors for the nested <c>Items[i].Field</c> identifiers (#91).
    /// </summary>
    /// <remarks>
    /// Callers needing both used to obtain them by awaiting <see cref="ValidateAsync" /> and then
    /// <see cref="ValidateItemsAsync" /> — and because the former already awaits the latter, that ran
    /// every item field's validators twice per pass. Harmless-looking (each message still lands once,
    /// on its own identifier) and not harmless at all for a validator that calls an API or has any
    /// other side effect (#329). One traversal now feeds both, with the flat messages derived from
    /// the structured errors.
    /// </remarks>
    /// <param name="model">The parent model instance.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>The flat messages and the structured per-item errors from a single traversal.</returns>
    public async Task<CollectionValidationResult> ValidateAllAsync(TModel model, IServiceProvider services)
    {
        var itemErrors = await ValidateItemsAsync(model, services);
        return new CollectionValidationResult(BuildMessages(model, itemErrors), itemErrors);
    }

    /// <summary>
    /// Projects one traversal's structured errors into the flat, collection-level messages: the
    /// item-count rules first, then one line per item error in the order the traversal produced them.
    /// </summary>
    private List<string> BuildMessages(TModel model, List<CollectionItemError> itemErrors)
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

        foreach (var itemError in itemErrors)
        {
            var field = _configuration.ItemFormConfiguration?.Fields
                .FirstOrDefault(f => f.FieldName == itemError.FieldName);
            errors.Add($"{_configuration.Label ?? _configuration.FieldName} [{itemError.ItemIndex + 1}] - {field?.Label ?? itemError.FieldName}: {itemError.Message}");
        }

        return errors;
    }

    /// <summary>
    /// Validates each item of the collection using the item form configuration's validators and
    /// returns structured errors that identify the failing item index and field name. This enables
    /// callers to attach messages to nested Blazor field identifiers (e.g. <c>Items[0].ProductName</c>)
    /// instead of flat, pre-formatted strings.
    /// </summary>
    /// <param name="model">The parent model instance.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>A list of structured per-item validation errors. Empty if validation passed.</returns>
    public async Task<List<CollectionItemError>> ValidateItemsAsync(TModel model, IServiceProvider services)
    {
        var errors = new List<CollectionItemError>();
        var items = _configuration.CollectionAccessor(model);

        if (items == null || _configuration.ItemFormConfiguration == null)
        {
            return errors;
        }

        // Resolve each field's getter once for the whole collection. The loop below is items × fields
        // and, since #203, runs on every keystroke in a row — so compiling here was the dominant cost
        // (#312), and even the cache lookup is worth hoisting: a 50-row × 5-field form probes 5 times
        // instead of 250. The getters take the item as their parameter, so one getter serves every row.
        var fields = _configuration.ItemFormConfiguration.Fields;
        var getters = new Func<TItem, object>[fields.Count];
        for (var f = 0; f < fields.Count; f++)
        {
            getters[f] = FieldValueGetterCache<TItem>.GetOrCompile(fields[f]);
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            for (var f = 0; f < fields.Count; f++)
            {
                var field = fields[f];
                var value = getters[f](item);

                foreach (var validator in field.Validators)
                {
                    var result = await validator.ValidateAsync(item, value, services);
                    if (!result.IsValid)
                    {
                        errors.Add(new CollectionItemError(i, field.FieldName, result.ErrorMessage!));
                    }
                }
            }
        }

        return errors;
    }
}
