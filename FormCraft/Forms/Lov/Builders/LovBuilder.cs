using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Fluent builder for configuring List of Values (LOV) fields.
/// Provides a comprehensive API for data source, columns, mappings, and display options.
/// </summary>
/// <typeparam name="TModel">The model type that the form binds to.</typeparam>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
/// <typeparam name="TItem">The type of items in the LOV.</typeparam>
/// <example>
/// <code>
/// .AddField(x => x.CustomerId, field => field
///     .AsLov&lt;OrderModel, int, Customer&gt;(lov => lov
///         .WithKey(c => c.Id)
///         .WithDisplay(c => c.Name)
///         .WithDataSource(() => customers)
///         .AddColumn(c => c.Code, "Code")
///         .AddColumn(c => c.Name, "Name")
///         .MapField(c => c.Email, x => x.CustomerEmail)))
/// </code>
/// </example>
public class LovBuilder<TModel, TValue, TItem> where TModel : new()
{
    private readonly FieldBuilder<TModel, TValue> _fieldBuilder;
    private readonly LovConfiguration<TItem, TValue> _configuration = new();

    internal LovBuilder(FieldBuilder<TModel, TValue> fieldBuilder)
    {
        _fieldBuilder = fieldBuilder;
    }

    #region Data Source Configuration

    /// <summary>
    /// Configures the data source using a synchronous collection factory.
    /// Best for small, in-memory datasets.
    /// </summary>
    /// <param name="collectionFactory">Function that returns the collection of items.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithDataSource(Func<IEnumerable<TItem>> collectionFactory)
    {
        _configuration.DataProvider = (query, ct) =>
        {
            var result = LovDataResult<TItem>.FromCollection(collectionFactory(), query);
            return Task.FromResult(result);
        };
        return this;
    }

    /// <summary>
    /// Configures the data source using an async data provider function.
    /// Best for server-side data with pagination support.
    /// </summary>
    /// <param name="provider">Async function that retrieves data based on query parameters.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithDataSource(
        Func<LovQuery, CancellationToken, Task<LovDataResult<TItem>>> provider)
    {
        _configuration.DataProvider = provider;
        return this;
    }

    /// <summary>
    /// Configures the data source using a DI-registered service.
    /// The service must implement ILovDataProvider&lt;TItem&gt;.
    /// </summary>
    /// <typeparam name="TService">The service type to resolve from DI.</typeparam>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithDataService<TService>()
        where TService : ILovDataProvider<TItem>
    {
        _configuration.DataProviderServiceType = typeof(TService);
        return this;
    }

    /// <summary>
    /// Configures the data source using a JSON configuration file.
    /// Requires ILovConfigurationProvider to be registered.
    /// </summary>
    /// <param name="lovId">The LOV configuration ID from the JSON file.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithJsonConfig(string lovId)
    {
        _configuration.JsonConfigId = lovId;
        return this;
    }

    #endregion

    #region Value and Display Configuration

    /// <summary>
    /// Configures the function to extract the value from a selected item.
    /// </summary>
    /// <param name="keyExpression">Expression to select the key/value property.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithKey(Expression<Func<TItem, TValue>> keyExpression)
    {
        _configuration.ValueSelector = keyExpression.Compile();
        return this;
    }

    /// <summary>
    /// Configures the function to generate display text for selected items.
    /// </summary>
    /// <param name="displayExpression">Expression to select the display property.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithDisplay(Expression<Func<TItem, string>> displayExpression)
    {
        _configuration.DisplaySelector = displayExpression.Compile();
        return this;
    }

    /// <summary>
    /// Configures the display text using a custom function.
    /// Use this for complex display formatting.
    /// </summary>
    /// <param name="displayFunc">Function to generate display text.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithDisplay(Func<TItem, string> displayFunc)
    {
        _configuration.DisplaySelector = displayFunc;
        return this;
    }

    #endregion

    #region Column Configuration

    /// <summary>
    /// Adds a column to the LOV grid.
    /// </summary>
    /// <typeparam name="TColumn">The type of the column value.</typeparam>
    /// <param name="columnExpression">Expression to select the column property.</param>
    /// <param name="header">The column header text.</param>
    /// <param name="configure">Optional action to configure additional column settings.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> AddColumn<TColumn>(
        Expression<Func<TItem, TColumn>> columnExpression,
        string header,
        Action<LovColumnBuilder<TItem>>? configure = null)
    {
        var propertyName = GetPropertyName(columnExpression);
        var valueSelector = columnExpression.Compile();

        var column = new LovColumnDefinition<TItem>
        {
            Header = header,
            PropertyName = propertyName,
            ValueSelector = item => valueSelector(item)
        };

        if (configure != null)
        {
            var columnBuilder = new LovColumnBuilder<TItem>(column);
            configure(columnBuilder);
        }

        _configuration.ColumnsInternal.Add(column);
        return this;
    }

    /// <summary>
    /// Adds a column with a custom value selector function.
    /// </summary>
    /// <param name="header">The column header text.</param>
    /// <param name="valueSelector">Function to extract the column value.</param>
    /// <param name="configure">Optional action to configure additional column settings.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> AddColumn(
        string header,
        Func<TItem, object?> valueSelector,
        Action<LovColumnBuilder<TItem>>? configure = null)
    {
        var column = new LovColumnDefinition<TItem>
        {
            Header = header,
            PropertyName = header,
            ValueSelector = valueSelector
        };

        if (configure != null)
        {
            var columnBuilder = new LovColumnBuilder<TItem>(column);
            configure(columnBuilder);
        }

        _configuration.ColumnsInternal.Add(column);
        return this;
    }

    #endregion

    #region Field Mapping Configuration

    /// <summary>
    /// Maps a property from the LOV item to a property on the model.
    /// When an item is selected, the target property is automatically populated.
    /// </summary>
    /// <typeparam name="TFieldValue">The type of the mapped value.</typeparam>
    /// <param name="sourceExpression">Expression to select the source property on the item.</param>
    /// <param name="targetExpression">Expression to select the target property on the model.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> MapField<TFieldValue>(
        Expression<Func<TItem, TFieldValue>> sourceExpression,
        Expression<Func<TModel, TFieldValue>> targetExpression)
    {
        var mapping = new LovFieldMapping<TItem, TModel, TFieldValue>(
            sourceExpression,
            targetExpression);

        _configuration.FieldMappingsInternal.Add(mapping);
        return this;
    }

    /// <summary>
    /// Maps a property with an async action for complex mapping scenarios.
    /// </summary>
    /// <typeparam name="TFieldValue">The type of the mapped value.</typeparam>
    /// <param name="sourceExpression">Expression to select the source property on the item.</param>
    /// <param name="targetExpression">Expression to select the target property on the model.</param>
    /// <param name="asyncAction">Async action to perform additional operations.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> MapFieldAsync<TFieldValue>(
        Expression<Func<TItem, TFieldValue>> sourceExpression,
        Expression<Func<TModel, TFieldValue>> targetExpression,
        Func<TItem, TModel, IServiceProvider, CancellationToken, Task> asyncAction)
    {
        var mapping = new AsyncLovFieldMapping<TItem, TModel, TFieldValue>(
            sourceExpression,
            targetExpression,
            asyncAction);

        _configuration.FieldMappingsInternal.Add(mapping);
        return this;
    }

    #endregion

    #region Dependency Configuration

    /// <summary>
    /// Configures this LOV to depend on another field for cascading behavior.
    /// When the dependent field changes, this LOV's data is refreshed.
    /// </summary>
    /// <typeparam name="TDep">The type of the dependent field.</typeparam>
    /// <param name="dependsOnExpression">Expression identifying the field to depend on.</param>
    /// <param name="contextKey">The key to use when passing the value to the data provider.</param>
    /// <param name="clearOnChange">Whether to clear the selection when the dependent field changes.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> DependsOn<TDep>(
        Expression<Func<TModel, TDep>> dependsOnExpression,
        string contextKey,
        bool clearOnChange = true)
    {
        var propertyName = GetPropertyName(dependsOnExpression);

        _configuration.DependenciesInternal.Add(new LovDependencyInfo
        {
            DependentPropertyName = propertyName,
            ContextKey = contextKey,
            ClearOnChange = clearOnChange
        });

        return this;
    }

    #endregion

    #region Selection Mode Configuration

    /// <summary>
    /// Enables multiple selection mode.
    /// </summary>
    /// <param name="maxSelections">Optional maximum number of selections allowed.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> AllowMultipleSelection(int? maxSelections = null)
    {
        _configuration.SelectionMode = LovSelectionMode.Multiple;
        if (maxSelections.HasValue)
        {
            _configuration.ModalOptions.Title = $"Select Items (max {maxSelections})";
        }
        return this;
    }

    #endregion

    #region Modal Options Configuration

    /// <summary>
    /// Sets the title of the modal dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithModalTitle(string title)
    {
        _configuration.ModalOptions.Title = title;
        return this;
    }

    /// <summary>
    /// Sets the size of the modal dialog.
    /// </summary>
    /// <param name="size">The modal size.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithModalSize(LovModalSize size)
    {
        _configuration.ModalOptions.Size = size;
        return this;
    }

    /// <summary>
    /// Sets the height of the data grid in the modal.
    /// </summary>
    /// <param name="height">The grid height (e.g., "400px", "50vh").</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithGridHeight(string height)
    {
        _configuration.ModalOptions.GridHeight = height;
        return this;
    }

    #endregion

    #region Search Options Configuration

    /// <summary>
    /// Disables the search functionality.
    /// </summary>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> DisableSearch()
    {
        _configuration.SearchOptions.Enabled = false;
        return this;
    }

    /// <summary>
    /// Configures the search placeholder text.
    /// </summary>
    /// <param name="placeholder">The placeholder text.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithSearchPlaceholder(string placeholder)
    {
        _configuration.SearchOptions.Placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// Configures the search debounce delay.
    /// </summary>
    /// <param name="milliseconds">The debounce delay in milliseconds.</param>
    /// <returns>The LovBuilder instance for method chaining.</returns>
    public LovBuilder<TModel, TValue, TItem> WithSearchDebounce(int milliseconds)
    {
        _configuration.SearchOptions.DebounceMs = milliseconds;
        return this;
    }

    #endregion

    /// <summary>
    /// Completes the LOV configuration and returns to the field builder.
    /// </summary>
    /// <returns>The FieldBuilder instance for continued configuration.</returns>
    public FieldBuilder<TModel, TValue> Build()
    {
        // Store the configuration in the field's additional attributes
        _fieldBuilder.WithAttribute("LovConfiguration", _configuration);
        _fieldBuilder.WithAttribute("LovItemType", typeof(TItem));
        return _fieldBuilder;
    }

    private static string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression operandMember)
        {
            return operandMember.Member.Name;
        }

        throw new ArgumentException(
            "Expression must be a simple property access expression",
            nameof(expression));
    }
}
