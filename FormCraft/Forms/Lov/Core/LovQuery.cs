namespace FormCraft;

/// <summary>
/// Represents a query for retrieving List of Values (LOV) data with support for
/// search, pagination, sorting, and filtering.
/// </summary>
public class LovQuery
{
    /// <summary>
    /// Gets or sets the search text entered by the user to filter results.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the starting index for pagination (0-based).
    /// Used with virtualized grids to load data on demand.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the number of items to retrieve per page.
    /// Default is 50 items.
    /// </summary>
    public int Count { get; set; } = 50;

    /// <summary>
    /// Gets or sets the list of sort definitions to apply to the query.
    /// Multiple sorts are applied in order.
    /// </summary>
    public List<LovSortDefinition> SortDefinitions { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of filter definitions to apply to the query.
    /// </summary>
    public List<LovFilterDefinition> FilterDefinitions { get; set; } = [];

    /// <summary>
    /// Gets or sets additional context values for cascading LOV scenarios.
    /// Keys are context parameter names, values are the filter values from parent fields.
    /// </summary>
    /// <example>
    /// <code>
    /// // In a cascading Country -> State scenario:
    /// query.Context["countryId"] = selectedCountryId;
    /// </code>
    /// </example>
    public Dictionary<string, object?> Context { get; set; } = [];
}

/// <summary>
/// Defines a sort operation for LOV data.
/// </summary>
public class LovSortDefinition
{
    /// <summary>
    /// Gets or sets the name of the property to sort by.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to sort in descending order.
    /// Default is false (ascending).
    /// </summary>
    public bool Descending { get; set; }

    /// <summary>
    /// Initializes a new instance of the LovSortDefinition class.
    /// </summary>
    public LovSortDefinition() { }

    /// <summary>
    /// Initializes a new instance of the LovSortDefinition class with specified values.
    /// </summary>
    /// <param name="propertyName">The property to sort by.</param>
    /// <param name="descending">Whether to sort descending.</param>
    public LovSortDefinition(string propertyName, bool descending = false)
    {
        PropertyName = propertyName;
        Descending = descending;
    }
}

/// <summary>
/// Defines a filter operation for LOV data.
/// </summary>
public class LovFilterDefinition
{
    /// <summary>
    /// Gets or sets the name of the property to filter on.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter operator (e.g., "eq", "contains", "startswith").
    /// Default is "contains".
    /// </summary>
    public string Operator { get; set; } = "contains";

    /// <summary>
    /// Gets or sets the value to filter by.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the LovFilterDefinition class.
    /// </summary>
    public LovFilterDefinition() { }

    /// <summary>
    /// Initializes a new instance of the LovFilterDefinition class with specified values.
    /// </summary>
    /// <param name="propertyName">The property to filter on.</param>
    /// <param name="value">The value to filter by.</param>
    /// <param name="operator">The filter operator.</param>
    public LovFilterDefinition(string propertyName, object? value, string @operator = "contains")
    {
        PropertyName = propertyName;
        Value = value;
        Operator = @operator;
    }
}
