namespace FormCraft;

/// <summary>
/// Represents a group of fields that should be rendered together in a specific layout.
/// </summary>
/// <typeparam name="TModel">The model type that the form will bind to.</typeparam>
public class FieldGroup<TModel>
{
    /// <summary>
    /// Gets or sets the unique identifier for this field group.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the display name for this group (optional).
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets the collection of field names that belong to this group.
    /// </summary>
    public List<string> FieldNames { get; } = new();
    
    /// <summary>
    /// Gets or sets the number of columns for this group's grid layout.
    /// </summary>
    public int Columns { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the CSS class to apply to this group's container.
    /// </summary>
    public string? CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets the order in which this group should be rendered.
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Gets or sets whether this group should render its fields in a card/panel.
    /// </summary>
    public bool ShowCard { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the elevation for the card if ShowCard is true.
    /// </summary>
    public int CardElevation { get; set; } = 1;
}
