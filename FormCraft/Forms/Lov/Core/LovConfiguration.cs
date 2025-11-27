namespace FormCraft;

/// <summary>
/// Concrete implementation of ILovConfiguration that stores all LOV settings.
/// This class is built by LovBuilder and stored in field AdditionalAttributes.
/// </summary>
/// <typeparam name="TItem">The type of items in the LOV.</typeparam>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public class LovConfiguration<TItem, TValue> : ILovConfiguration<TItem, TValue>
{
    /// <inheritdoc />
    public Func<LovQuery, CancellationToken, Task<LovDataResult<TItem>>>? DataProvider { get; set; }

    /// <inheritdoc />
    public Type? DataProviderServiceType { get; set; }

    /// <inheritdoc />
    public string? JsonConfigId { get; set; }

    /// <inheritdoc />
    public Func<TItem, TValue> ValueSelector { get; set; } = _ => default!;

    /// <inheritdoc />
    public Func<TItem, string> DisplaySelector { get; set; } = item => item?.ToString() ?? string.Empty;

    /// <summary>
    /// Internal mutable list of columns.
    /// </summary>
    internal List<LovColumnDefinition<TItem>> ColumnsInternal { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<LovColumnDefinition<TItem>> Columns => ColumnsInternal;

    /// <summary>
    /// Internal mutable list of field mappings.
    /// </summary>
    internal List<ILovFieldMapping> FieldMappingsInternal { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ILovFieldMapping> FieldMappings => FieldMappingsInternal;

    /// <inheritdoc />
    public LovSelectionMode SelectionMode { get; set; } = LovSelectionMode.Single;

    /// <inheritdoc />
    public LovModalOptions ModalOptions { get; set; } = new();

    /// <inheritdoc />
    public LovSearchOptions SearchOptions { get; set; } = new();

    /// <summary>
    /// Internal mutable list of dependencies.
    /// </summary>
    internal List<LovDependencyInfo> DependenciesInternal { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<LovDependencyInfo> Dependencies => DependenciesInternal;
}
