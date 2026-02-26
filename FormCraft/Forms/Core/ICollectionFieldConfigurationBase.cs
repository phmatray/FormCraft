namespace FormCraft;

/// <summary>
/// Non-generic base interface for collection field configurations, enabling storage in
/// a single collection regardless of the item type.
/// </summary>
public interface ICollectionFieldConfigurationBase
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
    /// Gets or sets whether users can add new items.
    /// </summary>
    bool CanAdd { get; set; }

    /// <summary>
    /// Gets or sets whether users can remove items.
    /// </summary>
    bool CanRemove { get; set; }

    /// <summary>
    /// Gets or sets whether users can reorder items.
    /// </summary>
    bool CanReorder { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of items required.
    /// </summary>
    int MinItems { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items allowed.
    /// </summary>
    int MaxItems { get; set; }

    /// <summary>
    /// Gets or sets the text for the add button.
    /// </summary>
    string AddButtonText { get; set; }

    /// <summary>
    /// Gets or sets the text displayed when the collection is empty.
    /// </summary>
    string EmptyText { get; set; }

    /// <summary>
    /// Gets or sets whether the collection field is visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets the CLR type of items in the collection.
    /// </summary>
    Type ItemType { get; }
}
