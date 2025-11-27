namespace FormCraft;

/// <summary>
/// Defines the complete configuration for a List of Values (LOV) field,
/// including data source, display settings, columns, and field mappings.
/// </summary>
/// <typeparam name="TItem">The type of items in the LOV.</typeparam>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public interface ILovConfiguration<TItem, TValue>
{
    /// <summary>
    /// Gets the data provider delegate for fetching items.
    /// This is used when data is provided via a lambda function.
    /// </summary>
    Func<LovQuery, CancellationToken, Task<LovDataResult<TItem>>>? DataProvider { get; }

    /// <summary>
    /// Gets the type of the data provider service to resolve from DI.
    /// When set, the data provider is resolved from the service container.
    /// </summary>
    Type? DataProviderServiceType { get; }

    /// <summary>
    /// Gets the JSON configuration ID for loading configuration from external files.
    /// When set, configuration is loaded from the LOV configuration provider.
    /// </summary>
    string? JsonConfigId { get; }

    /// <summary>
    /// Gets the function to extract the value from a selected item.
    /// This value is bound to the model property.
    /// </summary>
    Func<TItem, TValue> ValueSelector { get; }

    /// <summary>
    /// Gets the function to generate the display text for a selected item.
    /// This text is shown in the field when an item is selected.
    /// </summary>
    Func<TItem, string> DisplaySelector { get; }

    /// <summary>
    /// Gets the list of column definitions for the LOV grid.
    /// </summary>
    IReadOnlyList<LovColumnDefinition<TItem>> Columns { get; }

    /// <summary>
    /// Gets the list of field mappings that populate additional model properties
    /// when an item is selected.
    /// </summary>
    IReadOnlyList<ILovFieldMapping> FieldMappings { get; }

    /// <summary>
    /// Gets the selection mode (single or multiple).
    /// </summary>
    LovSelectionMode SelectionMode { get; }

    /// <summary>
    /// Gets the modal dialog configuration options.
    /// </summary>
    LovModalOptions ModalOptions { get; }

    /// <summary>
    /// Gets the search functionality configuration options.
    /// </summary>
    LovSearchOptions SearchOptions { get; }

    /// <summary>
    /// Gets the list of cascading dependencies for this LOV.
    /// </summary>
    IReadOnlyList<LovDependencyInfo> Dependencies { get; }
}

/// <summary>
/// Information about a cascading LOV dependency.
/// </summary>
public class LovDependencyInfo
{
    /// <summary>
    /// Gets or sets the name of the property this LOV depends on.
    /// </summary>
    public string DependentPropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the context key used when querying the data provider.
    /// This key is added to LovQuery.Context with the dependent field's value.
    /// </summary>
    public string ContextKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to clear the selection when the dependent field changes.
    /// Default is true.
    /// </summary>
    public bool ClearOnChange { get; set; } = true;
}
