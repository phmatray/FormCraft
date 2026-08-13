using Microsoft.AspNetCore.Components;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Bridges the non-generic <see cref="ICollectionFieldConfigurationBase"/> to the generic
/// <see cref="FluentUICollectionFieldComponent{TModel, TItem}"/>, which needs the item type as a
/// type argument the caller does not have statically.
/// </summary>
public static class FluentUICollectionFieldRenderer
{
    /// <summary>
    /// Creates a <see cref="RenderFragment"/> that renders the collection field configuration with
    /// the correctly-closed generic component.
    /// </summary>
    /// <typeparam name="TModel">The parent model type.</typeparam>
    /// <param name="model">The parent model instance.</param>
    /// <param name="collectionFieldConfig">The collection field configuration.</param>
    /// <param name="onCollectionChanged">Callback invoked when the collection changes.</param>
    /// <returns>A fragment that renders the collection field.</returns>
    public static RenderFragment Render<TModel>(
        TModel model,
        ICollectionFieldConfigurationBase collectionFieldConfig,
        EventCallback onCollectionChanged)
        where TModel : new()
    {
        return builder =>
        {
            var itemType = collectionFieldConfig.ItemType;
            var componentType = typeof(FluentUICollectionFieldComponent<,>)
                .MakeGenericType(typeof(TModel), itemType);

            builder.OpenComponent(0, componentType);
            builder.AddAttribute(1, "Model", model);
            builder.AddAttribute(2, "Configuration", collectionFieldConfig);
            builder.AddAttribute(3, "OnCollectionChanged", onCollectionChanged);
            builder.CloseComponent();
        };
    }
}
