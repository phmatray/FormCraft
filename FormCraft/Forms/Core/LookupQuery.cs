namespace FormCraft;

/// <summary>
/// Query parameters for lookup table data provider.
/// </summary>
public class LookupQuery
{
    /// <summary>
    /// Gets or sets the search text to filter results.
    /// </summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based page index.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the field name to sort by, or null for default ordering.
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    /// Gets or sets whether to sort in descending order.
    /// </summary>
    public bool SortDescending { get; set; }
}
