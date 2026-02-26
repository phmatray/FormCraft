namespace FormCraft;

/// <summary>
/// Provides a fluent API for configuring collection (one-to-many) fields in a form.
/// </summary>
/// <typeparam name="TModel">The parent model type that contains the collection.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <example>
/// <code>
/// .AddCollectionField(x => x.Items, collection => collection
///     .AllowAdd()
///     .AllowRemove()
///     .WithMinItems(1)
///     .WithItemForm(item => item
///         .AddField(x => x.ProductName, field => field.Required())
///         .AddField(x => x.Quantity, field => field.WithRange(1, 100))))
/// </code>
/// </example>
public class CollectionFieldBuilder<TModel, TItem>
    where TModel : new()
    where TItem : new()
{
    private readonly CollectionFieldConfiguration<TModel, TItem> _configuration;

    internal CollectionFieldBuilder(CollectionFieldConfiguration<TModel, TItem> configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Sets the display label for the collection field.
    /// </summary>
    /// <param name="label">The text to display as the collection label.</param>
    /// <returns>The CollectionFieldBuilder instance for method chaining.</returns>
    public CollectionFieldBuilder<TModel, TItem> WithLabel(string label)
    {
        _configuration.Label = label;
        return this;
    }

    /// <summary>
    /// Allows users to add new items to the collection.
    /// </summary>
    /// <param name="buttonText">Optional text for the add button.</param>
    /// <returns>The CollectionFieldBuilder instance for method chaining.</returns>
    public CollectionFieldBuilder<TModel, TItem> AllowAdd(string? buttonText = null)
    {
        _configuration.CanAdd = true;
        if (buttonText != null)
        {
            _configuration.AddButtonText = buttonText;
        }
        return this;
    }

    /// <summary>
    /// Allows users to remove items from the collection.
    /// </summary>
    /// <returns>The CollectionFieldBuilder instance for method chaining.</returns>
    public CollectionFieldBuilder<TModel, TItem> AllowRemove()
    {
        _configuration.CanRemove = true;
        return this;
    }

    /// <summary>
    /// Allows users to reorder items in the collection using up/down buttons.
    /// </summary>
    /// <returns>The CollectionFieldBuilder instance for method chaining.</returns>
    public CollectionFieldBuilder<TModel, TItem> AllowReorder()
    {
        _configuration.CanReorder = true;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of items required in the collection.
    /// </summary>
    /// <param name="min">The minimum number of items.</param>
    /// <returns>The CollectionFieldBuilder instance for method chaining.</returns>
    public CollectionFieldBuilder<TModel, TItem> WithMinItems(int min)
    {
        _configuration.MinItems = min;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of items allowed in the collection.
    /// </summary>
    /// <param name="max">The maximum number of items.</param>
    /// <returns>The CollectionFieldBuilder instance for method chaining.</returns>
    public CollectionFieldBuilder<TModel, TItem> WithMaxItems(int max)
    {
        _configuration.MaxItems = max;
        return this;
    }

    /// <summary>
    /// Sets the text to display when the collection is empty.
    /// </summary>
    /// <param name="text">The empty state message.</param>
    /// <returns>The CollectionFieldBuilder instance for method chaining.</returns>
    public CollectionFieldBuilder<TModel, TItem> WithEmptyText(string text)
    {
        _configuration.EmptyText = text;
        return this;
    }

    /// <summary>
    /// Configures the form for individual items in the collection using a nested FormBuilder.
    /// </summary>
    /// <param name="itemFormConfig">A lambda that configures the item form using a FormBuilder.</param>
    /// <returns>The CollectionFieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .WithItemForm(item => item
    ///     .AddField(x => x.ProductName, field => field.Required())
    ///     .AddField(x => x.Quantity, field => field.WithRange(1, 100))
    ///     .AddField(x => x.UnitPrice, field => field.WithRange(0.01m, 10000m)))
    /// </code>
    /// </example>
    public CollectionFieldBuilder<TModel, TItem> WithItemForm(Action<FormBuilder<TItem>> itemFormConfig)
    {
        var itemBuilder = FormBuilder<TItem>.Create();
        itemFormConfig(itemBuilder);
        _configuration.ItemFormConfiguration = itemBuilder.Build();
        return this;
    }
}
