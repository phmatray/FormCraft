namespace FormCraft;

/// <summary>
/// Defines the selection mode for LOV fields.
/// </summary>
public enum LovSelectionMode
{
    /// <summary>
    /// Only one item can be selected at a time.
    /// </summary>
    Single,

    /// <summary>
    /// Multiple items can be selected.
    /// </summary>
    Multiple
}

/// <summary>
/// Defines the size of the LOV modal dialog.
/// </summary>
public enum LovModalSize
{
    /// <summary>
    /// Small modal (400px max width).
    /// </summary>
    Small,

    /// <summary>
    /// Medium modal (600px max width).
    /// </summary>
    Medium,

    /// <summary>
    /// Large modal (900px max width).
    /// </summary>
    Large,

    /// <summary>
    /// Extra large modal (1200px max width).
    /// </summary>
    ExtraLarge,

    /// <summary>
    /// Full screen modal.
    /// </summary>
    Fullscreen
}

/// <summary>
/// Configuration options for the LOV modal dialog.
/// </summary>
public class LovModalOptions
{
    /// <summary>
    /// Gets or sets the title displayed in the modal header.
    /// Default is "Select Item".
    /// </summary>
    public string Title { get; set; } = "Select Item";

    /// <summary>
    /// Gets or sets the size of the modal dialog.
    /// Default is Large.
    /// </summary>
    public LovModalSize Size { get; set; } = LovModalSize.Large;

    /// <summary>
    /// Gets or sets the height of the data grid in the modal.
    /// Default is "400px".
    /// </summary>
    public string GridHeight { get; set; } = "400px";

    /// <summary>
    /// Gets or sets whether clicking the backdrop closes the modal.
    /// Default is true.
    /// </summary>
    public bool CloseOnBackdropClick { get; set; } = true;

    /// <summary>
    /// Gets or sets whether pressing Escape closes the modal.
    /// Default is true.
    /// </summary>
    public bool CloseOnEscapeKey { get; set; } = true;

    /// <summary>
    /// Gets or sets the row height for virtualization calculations.
    /// Should match the actual rendered row height for accurate scrolling.
    /// Default is 52.0f (standard MudDataGrid row height).
    /// </summary>
    public float ItemSize { get; set; } = 52.0f;
}

/// <summary>
/// Configuration options for the LOV search functionality.
/// </summary>
public class LovSearchOptions
{
    /// <summary>
    /// Gets or sets whether the search box is enabled.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the placeholder text for the search box.
    /// Default is "Search...".
    /// </summary>
    public string Placeholder { get; set; } = "Search...";

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds before triggering a search.
    /// This prevents excessive API calls while the user is typing.
    /// Default is 300ms.
    /// </summary>
    public int DebounceMs { get; set; } = 300;

    /// <summary>
    /// Gets or sets the minimum number of characters required before triggering a search.
    /// Set to 0 to search immediately.
    /// Default is 0.
    /// </summary>
    public int MinSearchLength { get; set; } = 0;

    /// <summary>
    /// Gets or sets the list of property names to search across.
    /// If empty, the search will use the default behavior of the data provider.
    /// </summary>
    public List<string> SearchFields { get; set; } = [];
}
