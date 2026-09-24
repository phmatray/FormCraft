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

        // Qualify by the FULL member-access chain (e.g. "Billing.Items"), not just the last segment,
        // so two collection fields bound through different nested paths that happen to share a last
        // segment (x => x.Billing.Items and x => x.Shipping.Items) no longer collide under the one
        // name DynamicFormValidator looks a collection field up by (#428). A single, non-nested path
        // is one segment, so it produces exactly the string it always has. MemberPathResolver already
        // does this walk for #423's security paths (FormSecurityEnforcer.BuildAuditKey, #406, is
        // itself a one-line call to it) - reuse it here rather than a third copy of the same walk.
        FieldName = MemberPathResolver.GetDottedPath(collectionExpression) ?? memberExpression.Member.Name;
        Label = memberExpression.Member.Name;

        // Compile the accessor
        CollectionAccessor = collectionExpression.Compile();

        // Build the setter by assigning through the SAME member-access chain the accessor reads
        // (memberExpression, with every intermediate hop such as `.Details` preserved), rather than
        // rebuilding only the outermost property against TModel directly. Reusing the expression's
        // own parameter here - instead of introducing a second one and substituting it through the
        // chain - is sufficient: every node inside memberExpression already refers to
        // collectionExpression.Parameters[0], so binding the new lambda to that same parameter
        // instance makes the assignment valid without an ExpressionVisitor rewrite (#420).
        var valueParameter = Expression.Parameter(typeof(List<TItem>), "value");
        var assign = Expression.Assign(memberExpression, valueParameter);
        CollectionSetter = Expression.Lambda<Action<TModel, List<TItem>>>(
            assign, collectionExpression.Parameters[0], valueParameter).Compile();
    }
}
