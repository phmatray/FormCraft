using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Default implementation of <see cref="ICollectionFieldConfiguration{TModel, TItem}"/> that stores
/// all configuration for a collection field.
/// </summary>
/// <typeparam name="TModel">The parent model type that contains the collection.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
public class CollectionFieldConfiguration<TModel, TItem> : ICollectionFieldConfiguration<TModel, TItem>, ICollectionFieldConfigurationBase
    where TModel : new()
    where TItem : new()
{
    /// <inheritdoc />
    public Type ItemType => typeof(TItem);
    /// <inheritdoc />
    public string FieldName { get; }

    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public bool CanAdd { get; set; }

    /// <inheritdoc />
    public bool CanRemove { get; set; }

    /// <inheritdoc />
    public bool CanReorder { get; set; }

    /// <inheritdoc />
    public int MinItems { get; set; }

    /// <inheritdoc />
    public int MaxItems { get; set; }

    /// <inheritdoc />
    public string AddButtonText { get; set; } = "Add Item";

    /// <inheritdoc />
    public string EmptyText { get; set; } = "No items added yet. Click 'Add Item' to begin.";

    /// <inheritdoc />
    public IFormConfiguration<TItem>? ItemFormConfiguration { get; set; }

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public Func<TModel, List<TItem>> CollectionAccessor { get; }

    /// <inheritdoc />
    public Action<TModel, List<TItem>> CollectionSetter { get; }

    /// <summary>
    /// Initializes a new instance of the CollectionFieldConfiguration class.
    /// </summary>
    /// <param name="collectionExpression">A lambda expression that identifies the collection property on the model.</param>
    /// <exception cref="ArgumentException">Thrown when the expression does not represent a valid property access.</exception>
    public CollectionFieldConfiguration(Expression<Func<TModel, List<TItem>>> collectionExpression)
    {
        var memberExpression = collectionExpression.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access expression.", nameof(collectionExpression));

        FieldName = memberExpression.Member.Name;
        Label = FieldName;

        // Compile the accessor
        CollectionAccessor = collectionExpression.Compile();

        // Build the setter
        var parameter = Expression.Parameter(typeof(TModel), "model");
        var valueParameter = Expression.Parameter(typeof(List<TItem>), "value");
        var property = Expression.Property(parameter, memberExpression.Member.Name);
        var assign = Expression.Assign(property, valueParameter);
        CollectionSetter = Expression.Lambda<Action<TModel, List<TItem>>>(assign, parameter, valueParameter).Compile();
    }
}
