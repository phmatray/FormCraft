using FormCraft.ForMudBlazor;

// ReSharper disable once CheckNamespace
namespace FormCraft;

/// <summary>
/// Provides MudBlazor-specific extension methods for the FieldBuilder.
/// </summary>
public static class MudBlazorFieldBuilderExtensions
{
    /// <summary>
    /// Configures a string field to use the MudBlazor color picker renderer.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Color)
    ///     .AsColorPicker()
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> AsColorPicker<TModel>(
        this FieldBuilder<TModel, string> builder)
        where TModel : new()
    {
        return builder.WithCustomRenderer<TModel, string, MudBlazorColorPickerRenderer>();
    }

    /// <summary>
    /// Configures an integer field to use the MudBlazor rating renderer.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for an integer field.</param>
    /// <param name="maxRating">Maximum rating value (default: 5).</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Rating)
    ///     .AsRating(maxRating: 10)
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, int> AsRating<TModel>(
        this FieldBuilder<TModel, int> builder,
        int maxRating = 5)
        where TModel : new()
    {
        return builder
            .WithCustomRenderer<TModel, int, MudBlazorRatingRenderer>()
            .WithAttribute("MaxRating", maxRating);
    }

    /// <summary>
    /// Configures a double field to use the MudBlazor slider renderer.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a double field.</param>
    /// <param name="min">Minimum slider value (default: 0).</param>
    /// <param name="max">Maximum slider value (default: 100).</param>
    /// <param name="step">Step increment for the slider (default: 1).</param>
    /// <param name="showTickMarks">Whether to show tick marks on the slider (default: false).</param>
    /// <param name="showValueLabel">Whether to display the current value (default: true).</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Volume)
    ///     .AsSlider(min: 0, max: 100, step: 5, showTickMarks: true)
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, double> AsSlider<TModel>(
        this FieldBuilder<TModel, double> builder,
        double min = 0,
        double max = 100,
        double step = 1,
        bool showTickMarks = false,
        bool showValueLabel = true)
        where TModel : new()
    {
        return builder
            .WithCustomRenderer<TModel, double, MudBlazorSliderRenderer>()
            .WithAttribute("Min", min)
            .WithAttribute("Max", max)
            .WithAttribute("Step", step)
            .WithAttribute("ShowTickMarks", showTickMarks)
            .WithAttribute("ShowValueLabel", showValueLabel);
    }

    /// <summary>
    /// Configures a string field as a password field with an optional visibility toggle.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <param name="enableVisibilityToggle">Whether to show a toggle icon to show/hide the password (default: true).</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Password)
    ///     .AsPassword(enableVisibilityToggle: true)
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> AsPassword<TModel>(
        this FieldBuilder<TModel, string> builder,
        bool enableVisibilityToggle = true)
        where TModel : new()
    {
        builder.WithInputType("password");

        if (enableVisibilityToggle)
        {
            builder.WithAttribute("EnablePasswordToggle", true);
        }

        return builder;
    }

    /// <summary>
    /// Adds an adornment (icon or text) to a text field.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <param name="icon">The MudBlazor icon to display (e.g., Icons.Material.Filled.Email).</param>
    /// <param name="position">The position of the adornment (Start or End, default: Start).</param>
    /// <param name="color">The color of the adornment icon (default: Default).</param>
    /// <param name="onClick">Optional click handler for the adornment.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Email)
    ///     .WithAdornment(Icons.Material.Filled.Email, MudBlazor.Adornment.Start)
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> WithAdornment<TModel>(
        this FieldBuilder<TModel, string> builder,
        string icon,
        MudBlazor.Adornment position = MudBlazor.Adornment.Start,
        MudBlazor.Color color = MudBlazor.Color.Default,
        Action<string?>? onClick = null)
        where TModel : new()
    {
        return builder
            .WithAttribute("Adornment", position)
            .WithAttribute("AdornmentIcon", icon)
            .WithAttribute("AdornmentColor", color);
    }

    /// <summary>
    /// Configures the field as a lookup table with a modal dialog for selecting items from large datasets.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <typeparam name="TItem">The type of the lookup item displayed in the table.</typeparam>
    /// <param name="builder">The FieldBuilder instance.</param>
    /// <param name="dataProvider">An async function that returns paginated lookup results.</param>
    /// <param name="valueSelector">A function that extracts the field value from a selected lookup item.</param>
    /// <param name="displaySelector">A function that extracts the display text from a lookup item.</param>
    /// <param name="configureColumns">An optional action to configure the columns displayed in the lookup table.</param>
    /// <param name="onItemSelected">An optional callback invoked when an item is selected, allowing multi-field mapping.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.CityId)
    ///     .AsLookup&lt;MyModel, int, CityDto&gt;(
    ///         dataProvider: async query => new LookupResult&lt;CityDto&gt; { Items = cities, TotalCount = cities.Count },
    ///         valueSelector: city => city.Id,
    ///         displaySelector: city => city.Name,
    ///         configureColumns: cols =>
    ///         {
    ///             cols.Add(new LookupColumn&lt;CityDto&gt; { Title = "Name", ValueSelector = c => c.Name });
    ///             cols.Add(new LookupColumn&lt;CityDto&gt; { Title = "Country", ValueSelector = c => c.Country });
    ///         },
    ///         onItemSelected: (model, city) => model.CityName = city.Name)
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TValue> AsLookup<TModel, TValue, TItem>(
        this FieldBuilder<TModel, TValue> builder,
        Func<LookupQuery, Task<LookupResult<TItem>>> dataProvider,
        Func<TItem, TValue> valueSelector,
        Func<TItem, string> displaySelector,
        Action<List<LookupColumn<TItem>>>? configureColumns = null,
        Action<TModel, TItem>? onItemSelected = null)
        where TModel : new()
    {
        builder.WithAttribute("LookupDataProvider", dataProvider);
        builder.WithAttribute("LookupValueSelector", valueSelector);
        builder.WithAttribute("LookupDisplaySelector", displaySelector);

        if (configureColumns != null)
        {
            var columns = new List<LookupColumn<TItem>>();
            configureColumns(columns);
            builder.WithAttribute("LookupColumns", columns);
        }

        if (onItemSelected != null)
            builder.WithAttribute("LookupOnItemSelected", onItemSelected);

        return builder;
    }
}