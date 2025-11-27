namespace FormCraft;

/// <summary>
/// Extension methods for configuring LOV (List of Values) fields on FieldBuilder.
/// </summary>
public static class LovFieldBuilderExtensions
{
    /// <summary>
    /// Configures the field as a List of Values (LOV) with modal table selection.
    /// Use this for lookup fields that allow users to select from a searchable list.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TValue">The type of the selected value.</typeparam>
    /// <typeparam name="TItem">The type of items in the LOV.</typeparam>
    /// <param name="builder">The field builder.</param>
    /// <param name="configure">Action to configure the LOV settings.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.CustomerId, field => field
    ///     .WithLabel("Customer")
    ///     .AsLov&lt;OrderModel, int, Customer&gt;(lov => lov
    ///         .WithKey(c => c.Id)
    ///         .WithDisplay(c => c.Name)
    ///         .WithDataSource(() => customers)
    ///         .AddColumn(c => c.Code, "Code")
    ///         .AddColumn(c => c.Name, "Name")))
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TValue> AsLov<TModel, TValue, TItem>(
        this FieldBuilder<TModel, TValue> builder,
        Action<LovBuilder<TModel, TValue, TItem>> configure)
        where TModel : new()
    {
        var lovBuilder = new LovBuilder<TModel, TValue, TItem>(builder);
        configure(lovBuilder);
        return lovBuilder.Build();
    }

    /// <summary>
    /// Configures the field as a multi-select LOV where multiple items can be selected.
    /// The field value should be IEnumerable&lt;TValue&gt;.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TValue">The type of the selected value.</typeparam>
    /// <typeparam name="TItem">The type of items in the LOV.</typeparam>
    /// <param name="builder">The field builder.</param>
    /// <param name="configure">Action to configure the LOV settings.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.AssignedEmployeeIds, field => field
    ///     .WithLabel("Assigned Employees")
    ///     .AsMultiSelectLov&lt;TaskModel, int, Employee&gt;(lov => lov
    ///         .WithKey(e => e.Id)
    ///         .WithDisplay(e => e.Name)
    ///         .WithDataService&lt;IEmployeeLovService&gt;()
    ///         .AddColumn(e => e.Code, "Code")
    ///         .AddColumn(e => e.Name, "Name")
    ///         .AddColumn(e => e.Department, "Department")))
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, IEnumerable<TValue>> AsMultiSelectLov<TModel, TValue, TItem>(
        this FieldBuilder<TModel, IEnumerable<TValue>> builder,
        Action<LovBuilder<TModel, IEnumerable<TValue>, TItem>> configure)
        where TModel : new()
    {
        var lovBuilder = new LovBuilder<TModel, IEnumerable<TValue>, TItem>(builder);
        lovBuilder.AllowMultipleSelection();
        configure(lovBuilder);
        return lovBuilder.Build();
    }
}
