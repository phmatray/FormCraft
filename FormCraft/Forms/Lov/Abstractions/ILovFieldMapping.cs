namespace FormCraft;

/// <summary>
/// Defines a mapping from a LOV item property to a form model property.
/// When an item is selected in the LOV, these mappings automatically populate
/// related fields in the model.
/// </summary>
/// <example>
/// <code>
/// // Example: Selecting a customer populates name and email
/// .MapField(customer => customer.Name, model => model.CustomerName)
/// .MapField(customer => customer.Email, model => model.CustomerEmail)
/// </code>
/// </example>
public interface ILovFieldMapping
{
    /// <summary>
    /// Gets the name of the source property on the LOV item.
    /// </summary>
    string SourceProperty { get; }

    /// <summary>
    /// Gets the name of the target property on the model.
    /// </summary>
    string TargetProperty { get; }

    /// <summary>
    /// Gets whether this mapping is asynchronous.
    /// </summary>
    bool IsAsync { get; }

    /// <summary>
    /// Applies the mapping synchronously from the selected item to the model.
    /// </summary>
    /// <param name="item">The selected LOV item.</param>
    /// <param name="model">The model to update.</param>
    void Apply(object item, object model);
}

/// <summary>
/// Extends ILovFieldMapping with async support for complex mapping scenarios
/// that require service calls or additional data fetching.
/// </summary>
public interface IAsyncLovFieldMapping : ILovFieldMapping
{
    /// <summary>
    /// Applies the mapping asynchronously from the selected item to the model.
    /// Use this for mappings that need to fetch additional data or call services.
    /// </summary>
    /// <param name="item">The selected LOV item.</param>
    /// <param name="model">The model to update.</param>
    /// <param name="services">Service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ApplyAsync(object item, object model, IServiceProvider services, CancellationToken cancellationToken = default);
}
