using Microsoft.AspNetCore.Components;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Helper class that creates the appropriate generic CollectionFieldComponent for a given
/// collection field configuration using reflection. This bridges the non-generic
/// ICollectionFieldConfigurationBase to the generic CollectionFieldComponent.
/// </summary>
public static class CollectionFieldRenderer
{
    /// <summary>
    /// Creates a RenderFragment that renders the appropriate CollectionFieldComponent
    /// for the given collection field configuration.
    /// </summary>
    /// <typeparam name="TModel">The parent model type.</typeparam>
    /// <param name="model">The parent model instance.</param>
    /// <param name="collectionFieldConfig">The collection field configuration (must implement ICollectionFieldConfigurationBase).</param>
    /// <param name="onCollectionChanged">Callback invoked when the collection changes.</param>
    /// <returns>A RenderFragment that renders the collection field.</returns>
    public static RenderFragment Render<TModel>(
        TModel model,
        ICollectionFieldConfigurationBase collectionFieldConfig,
        EventCallback onCollectionChanged)
        where TModel : new()
    {
        return builder =>
        {
            var itemType = collectionFieldConfig.ItemType;
            var componentType = typeof(CollectionFieldComponent<,>).MakeGenericType(typeof(TModel), itemType);

            builder.OpenComponent(0, componentType);
            builder.AddAttribute(1, "Model", model);
            builder.AddAttribute(2, "Configuration", collectionFieldConfig);
            builder.AddAttribute(3, "OnCollectionChanged", onCollectionChanged);
            builder.CloseComponent();
        };
    }
}
