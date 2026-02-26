namespace FormCraft;

/// <summary>
/// Represents the configuration for a collection (one-to-many) field in a form.
/// Collection fields allow users to add, remove, and edit multiple items of the same type.
/// </summary>
/// <typeparam name="TModel">The parent model type that contains the collection.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
public interface ICollectionFieldConfiguration<TModel, TItem>
    where TModel : new()
    where TItem : new()
{
    /// <summary>
    /// Gets the name of the collection property on the model.
    /// </summary>
    string FieldName { get; }

    /// <summary>
    /// Gets or sets the display label for the collection field.
    /// </summary>
    string? Label { get; set; }

    /// <summary>
    /// Gets or sets the display order of this collection field relative to other fields.
    /// </summary>
    int Order { get; set; }

    /// <summary>
    /// Gets or sets whether users can add new items to the collection.
    /// </summary>
    bool CanAdd { get; set; }

    /// <summary>
    /// Gets or sets whether users can remove items from the collection.
    /// </summary>
    bool CanRemove { get; set; }

    /// <summary>
    /// Gets or sets whether users can reorder items in the collection.
    /// </summary>
    bool CanReorder { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of items required in the collection.
    /// A value of 0 means no minimum is enforced.
    /// </summary>
    int MinItems { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items allowed in the collection.
    /// A value of 0 means no maximum is enforced.
    /// </summary>
    int MaxItems { get; set; }

    /// <summary>
    /// Gets or sets the text to display on the "Add" button.
    /// </summary>
    string AddButtonText { get; set; }

    /// <summary>
    /// Gets or sets the text to display when the collection is empty.
    /// </summary>
    string EmptyText { get; set; }

    /// <summary>
    /// Gets the form configuration for individual items in the collection.
    /// </summary>
    IFormConfiguration<TItem>? ItemFormConfiguration { get; }

    /// <summary>
    /// Gets or sets whether the collection field is visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets the function to retrieve the collection from the model.
    /// </summary>
    Func<TModel, List<TItem>> CollectionAccessor { get; }

    /// <summary>
    /// Gets the action to set the collection on the model.
    /// </summary>
    Action<TModel, List<TItem>> CollectionSetter { get; }
}
