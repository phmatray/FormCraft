namespace FormCraft;

/// <summary>
/// Non-generic surface over <see cref="CollectionFieldValidator{TModel, TItem}"/> that
/// <c>DynamicFormValidator</c> can hold and call directly, without <c>MethodInfo.Invoke</c>.
/// </summary>
/// <remarks>
/// Introduced by #344 in place of a <c>CreateValidator()</c> factory on
/// <see cref="ICollectionFieldConfigurationBase"/>: a factory would put a behavioural method on a
/// configuration interface, a heavier abstraction for the same result. Constructing the concrete
/// <see cref="CollectionFieldValidator{TModel, TItem}"/> still requires reflection — its <c>TItem</c>
/// is unknown at <c>DynamicFormValidator</c>'s own compile time — but every call on it afterwards is
/// now a checked interface dispatch instead of a <see cref="System.Reflection.MethodInfo"/> invoke, so
/// a signature change here is caught by the compiler rather than at runtime.
/// </remarks>
internal interface ICollectionValidator
{
    /// <inheritdoc cref="CollectionFieldValidator{TModel, TItem}.ValidateAllAsync"/>
    Task<CollectionValidationResult> ValidateAllAsync(object model, IServiceProvider services);

    /// <inheritdoc cref="CollectionFieldValidator{TModel, TItem}.ValidateItemFieldAsync"/>
    Task<List<CollectionItemError>> ValidateItemFieldAsync(object model, int itemIndex, string fieldName, IServiceProvider services);

    /// <inheritdoc cref="CollectionFieldValidator{TModel, TItem}.FormatItemMessage"/>
    string FormatItemMessage(CollectionItemError itemError);

    /// <inheritdoc cref="CollectionFieldValidator{TModel, TItem}.FormatItemMessagePrefix"/>
    string FormatItemMessagePrefix(int itemIndex, string fieldName);
}

/// <summary>
/// Validates collection fields by checking item count constraints and recursively validating each item
/// using the item form configuration's validators.
/// </summary>
/// <typeparam name="TModel">The parent model type.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
public class CollectionFieldValidator<TModel, TItem> : ICollectionValidator
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
    /// <remarks>
    /// Superseded in-tree by <see cref="ValidateAllAsync" />, which returns this method's messages
    /// <i>and</i> the structured per-item errors from the same traversal. This wrapper stays for
    /// external callers; asking for both shapes through this method and
    /// <see cref="ValidateItemsAsync(TModel, IServiceProvider)" /> in turn is what ran every item validator twice (#329).
    /// </remarks>
    public async Task<List<string>> ValidateAsync(TModel model, IServiceProvider services)
        => [.. (await ValidateAllAsync(model, services)).Messages];

    /// <summary>
    /// Validates the collection <b>once</b> and returns both shapes its callers need: the flat,
    /// human-formatted messages for the collection's own field identifier, and the structured
    /// per-item errors for the nested <c>Items[i].Field</c> identifiers (#91).
    /// </summary>
    /// <remarks>
    /// Callers needing both used to obtain them by awaiting <see cref="ValidateAsync" /> and then
    /// <see cref="ValidateItemsAsync(TModel, IServiceProvider)" /> — and because the former already awaits the latter, that ran
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
        // Resolved ONCE and threaded into both the traversal and the count rules, rather than each
        // calling the accessor itself - a property that materialises a new list per access (e.g.
        // `=> _set.ToList()`) would otherwise have its count measured against a different snapshot
        // than the one actually validated (#344).
        var items = TryReadCollection(model);
        var (itemErrors, evaluatedItemFields) = await ValidateItemsWithEvaluatedAsync(items, services);
        return new CollectionValidationResult(BuildMessages(items, itemErrors), itemErrors)
        {
            EvaluatedItemFields = evaluatedItemFields
        };
    }

    /// <summary>
    /// <see cref="ICollectionValidator"/>'s untyped entry point — casts once at the boundary and
    /// delegates to <see cref="ValidateAllAsync(TModel, IServiceProvider)"/>.
    /// </summary>
    Task<CollectionValidationResult> ICollectionValidator.ValidateAllAsync(object model, IServiceProvider services)
        => ValidateAllAsync((TModel)model, services);

    /// <summary>
    /// Validates a <b>single</b> item field — the one cell a field-change notification names — rather
    /// than the whole collection.
    /// </summary>
    /// <remarks>
    /// The field-changed path used to call <see cref="ValidateItemsAsync(TModel, IServiceProvider)" /> and discard every result
    /// but the matching cell. Since #203 a keystroke in any row raises that notification, so a
    /// 50-row × 5-field form ran 250 validator invocations per character and used one of them; with
    /// an async validator, that is 250 awaited calls (#329).
    /// </remarks>
    /// <param name="model">The parent model instance.</param>
    /// <param name="itemIndex">Index of the item whose field changed.</param>
    /// <param name="fieldName">Name of the item field that changed.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>The errors for that one cell. Empty if it is valid, or if the cell does not exist.</returns>
    public async Task<List<CollectionItemError>> ValidateItemFieldAsync(
        TModel model,
        int itemIndex,
        string fieldName,
        IServiceProvider services)
    {
        var errors = new List<CollectionItemError>();
        var items = TryReadCollection(model);

        if (items == null || _configuration.ItemFormConfiguration == null)
        {
            return errors;
        }

        // The index can be stale: a row removed between the notification and this call would
        // previously just fail to match during the filter, so an out-of-range index stays a no-op.
        if (itemIndex < 0 || itemIndex >= items.Count)
        {
            return errors;
        }

        var item = items[itemIndex];

        // EVERY field configuration carrying this name, not just the first. An item form can declare
        // more than one configuration for the same property, and the full pass validates them all —
        // so matching only the first would make a keystroke silently erase the others' messages
        // until the next submit put them back.
        foreach (var field in _configuration.ItemFormConfiguration.Fields)
        {
            if (field.FieldName != fieldName)
            {
                continue;
            }

            // TryGetValue rather than GetOrCompile(field)(item) directly (#397): a nested binding with
            // a null intermediate would otherwise throw out of this cell's validation instead of just
            // validating null, the same treatment a genuinely-null leaf value already gets.
            FieldValueGetterCache<TItem>.TryGetValue(field, item, out var value);

            foreach (var validator in field.Validators)
            {
                // A failed read (TryGetValue above) is treated as null, which validators already
                // handle as a legitimate value — the null-forgiving operator matches the interface's
                // non-nullable `object value` parameter, not a claim that value can never be null.
                var result = await validator.ValidateAsync(item, value!, services);
                if (!result.IsValid)
                {
                    errors.Add(new CollectionItemError(itemIndex, field.FieldName, result.ErrorMessage!));
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// <see cref="ICollectionValidator"/>'s untyped entry point — casts once at the boundary and
    /// delegates to <see cref="ValidateItemFieldAsync(TModel, int, string, IServiceProvider)"/>.
    /// </summary>
    Task<List<CollectionItemError>> ICollectionValidator.ValidateItemFieldAsync(
        object model, int itemIndex, string fieldName, IServiceProvider services)
        => ValidateItemFieldAsync((TModel)model, itemIndex, fieldName, services);

    /// <summary>
    /// Projects one traversal's structured errors into the flat, collection-level messages: the
    /// item-count rules first, then one line per item error in the order the traversal produced them.
    /// </summary>
    /// <param name="items">
    /// The same resolved collection <see cref="ValidateAllAsync"/> passed to the item traversal -
    /// never re-resolved here, so the count rules describe the same snapshot that was validated (#344).
    /// </param>
    /// <param name="itemErrors">The structured per-item errors from that same traversal.</param>
    private List<string> BuildMessages(List<TItem>? items, List<CollectionItemError> itemErrors)
    {
        var errors = new List<string>();
        var itemCount = items?.Count ?? 0;

        // Validate min items
        if (_configuration.MinItems > 0 && itemCount < _configuration.MinItems)
        {
            errors.Add(ValidationMessages.CollectionMinItems(_configuration.Label ?? _configuration.FieldName, _configuration.MinItems));
        }

        // Validate max items
        if (_configuration.MaxItems > 0 && itemCount > _configuration.MaxItems)
        {
            errors.Add(ValidationMessages.CollectionMaxItems(_configuration.Label ?? _configuration.FieldName, _configuration.MaxItems));
        }

        foreach (var itemError in itemErrors)
        {
            errors.Add(FormatItemMessage(itemError));
        }

        return errors;
    }

    /// <summary>
    /// Formats one item error into the flat, human-formatted line a <c>ValidationSummary</c> shows
    /// for the collection's own field identifier (e.g. <c>"Items [1] - Product: Product name is
    /// required"</c>) - the same line <see cref="BuildMessages"/> produces for it.
    /// </summary>
    /// <remarks>
    /// Public so <c>DynamicFormValidator</c> can reproduce this exact formatting when a single-cell
    /// edit needs to replace just that cell's own line(s) in the flat set, without re-running every
    /// other row's validators to rebuild the whole set (#329, #342).
    /// </remarks>
    /// <param name="itemError">One structured per-item error from a validation pass.</param>
    /// <returns>The formatted flat message line for this error.</returns>
    public string FormatItemMessage(CollectionItemError itemError)
        => $"{FormatItemLabel(itemError.ItemIndex, itemError.FieldName)}: {itemError.Message}";

    /// <summary>
    /// The prefix every flat message for one item field shares, with no trailing error text -
    /// enough to find and remove that field's own line(s) from the flat set without matching a
    /// different field's line.
    /// </summary>
    /// <remarks>
    /// ⚠️ Built from the item field's <b>label</b>, not its field name: two different item fields
    /// that happen to share a <c>Label</c> collide on this prefix, and a caller removing "by prefix"
    /// would then remove the wrong field's line too. Two configurations declared for the <i>same</i>
    /// property collide correctly instead - both share this one prefix and both get re-added by the
    /// caller, matching what a full pass already produces for them.
    /// </remarks>
    /// <param name="itemIndex">Index of the item row.</param>
    /// <param name="fieldName">Name of the item field.</param>
    /// <returns>The formatted line's prefix, up to and including <c>": "</c>.</returns>
    public string FormatItemMessagePrefix(int itemIndex, string fieldName)
        => $"{FormatItemLabel(itemIndex, fieldName)}: ";

    private string FormatItemLabel(int itemIndex, string fieldName)
    {
        var field = _configuration.ItemFormConfiguration?.Fields
            .FirstOrDefault(f => f.FieldName == fieldName);
        return $"{_configuration.Label ?? _configuration.FieldName} [{itemIndex + 1}] - {field?.Label ?? fieldName}";
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
    /// <remarks>
    /// <c>async</c> historically defended against an exception from a misbehaving accessor escaping
    /// this method call synchronously instead of completing the returned <see cref="Task" /> as
    /// faulted (#344) - a caller awaiting elsewhere in a <c>try</c>/<c>catch</c> (or collecting the
    /// task for <see cref="Task.WhenAll(Task[])" />) would see it differently otherwise.
    /// <see cref="TryReadCollection" /> (#408) absorbs only the null-intermediate case; any other
    /// accessor fault, or a cancellation, propagates out of it (#425) - so that path is live, and
    /// <c>async</c> is what makes it surface as a faulted task rather than a synchronous throw.
    /// </remarks>
    public async Task<List<CollectionItemError>> ValidateItemsAsync(TModel model, IServiceProvider services)
        => (await ValidateItemsWithEvaluatedAsync(TryReadCollection(model), services)).Errors;

    /// <summary>
    /// Reads the collection through <see cref="ICollectionFieldConfiguration{TModel, TItem}.CollectionAccessor"/>,
    /// returning <see langword="null"/> instead of throwing when a nested binding's intermediate is
    /// null (e.g. <c>x => x.Details.Items</c> where <c>Details</c> is null) - the same failure #397
    /// fixed for individual field getters, one level up at the collection binding itself (#408).
    /// </summary>
    /// <remarks>
    /// Routes through <see cref="FieldValueGetterCache{TModel}.TryInvoke"/> rather than adding a new
    /// try/catch: <see cref="ICollectionFieldConfiguration{TModel, TItem}.CollectionAccessor"/> is
    /// <c>Func&lt;TModel, List&lt;TItem&gt;&gt;</c>, which converts to <c>TryInvoke</c>'s
    /// <c>Func&lt;TModel, object&gt;</c> parameter by ordinary delegate covariance (<c>List&lt;TItem&gt;</c>
    /// is a reference type). <c>TryInvoke</c> catches only <see cref="NullReferenceException"/> - the
    /// null-intermediate case (#425 narrowed it from <see cref="Exception"/>). An unreachable
    /// collection comes back as <see langword="null"/>, which every caller here already treats as zero
    /// items via its existing null-coalescing/null-check logic - no new "unreadable collection" error
    /// shape is introduced. Every other exception - a genuinely faulting accessor, a cancellation -
    /// propagates through this method to its callers, rather than validating as an empty, passing
    /// collection. One private helper here rather than the same three lines duplicated at each call
    /// site.
    /// </remarks>
    /// <param name="model">The parent model instance.</param>
    /// <returns>The collection on success; <see langword="null"/> when a null intermediate makes it unreachable.</returns>
    private List<TItem>? TryReadCollection(TModel model)
    {
        FieldValueGetterCache<TModel>.TryInvoke(_configuration.CollectionAccessor, model, out var value);
        return value as List<TItem>;
    }

    /// <summary>
    /// The item traversal itself, given an already-resolved collection. <see cref="ValidateAllAsync"/>
    /// resolves the accessor once and threads the same list in here and into
    /// <see cref="BuildMessages"/>; the public <see cref="ValidateItemsAsync(TModel, IServiceProvider)"/>
    /// overload keeps resolving its own, since external callers rely on that shape (#344).
    /// </summary>
    /// <returns>
    /// The failing cells (unchanged shape) alongside every <c>(ItemIndex, FieldName)</c> pair this
    /// traversal ran a validator against, valid or not (#447) — collected in this same loop, rather
    /// than derived separately afterwards, so the two can never drift apart.
    /// </returns>
    private async Task<(List<CollectionItemError> Errors, List<(int ItemIndex, string FieldName)> Evaluated)> ValidateItemsWithEvaluatedAsync(
        List<TItem>? items, IServiceProvider services)
    {
        var errors = new List<CollectionItemError>();
        var evaluated = new List<(int ItemIndex, string FieldName)>();

        if (items == null || _configuration.ItemFormConfiguration == null)
        {
            return (errors, evaluated);
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
                evaluated.Add((i, field.FieldName));

                // FieldValueGetterCache<TItem>.TryInvoke, not TryGetValue: the getter is already
                // resolved above (getters[f]), and TryGetValue would re-resolve it from the field
                // configuration on every call, undoing the resolve-once hoisting (5 cache lookups
                // instead of 250 for a 50-row × 5-field form).
                FieldValueGetterCache<TItem>.TryInvoke(getters[f], item, out var value);

                foreach (var validator in field.Validators)
                {
                    // Null-forgiving to match the interface's non-nullable `object value` — TryInvoke
                    // above already turned a failed read into null, which validators already treat
                    // as a legitimate value.
                    var result = await validator.ValidateAsync(item, value!, services);
                    if (!result.IsValid)
                    {
                        errors.Add(new CollectionItemError(i, field.FieldName, result.ErrorMessage!));
                    }
                }
            }
        }

        return (errors, evaluated);
    }
}
