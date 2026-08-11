using System.Numerics;
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
    /// Sets the MudBlazor <see cref="MudBlazor.Variant"/> used to render the field's input,
    /// overriding the form-level default (see <c>FormCraftComponent&lt;TModel&gt;.DefaultVariant</c>).
    /// When neither is configured, fields render with <c>Variant.Outlined</c>.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <param name="builder">The FieldBuilder instance.</param>
    /// <param name="variant">The MudBlazor variant to apply (Text, Filled, or Outlined).</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Name, field => field
    ///     .WithLabel("Name")
    ///     .WithVariant(Variant.Filled))
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TValue> WithVariant<TModel, TValue>(
        this FieldBuilder<TModel, TValue> builder,
        MudBlazor.Variant variant)
        where TModel : new()
    {
        return builder.WithAttribute("Variant", variant);
    }

    /// <summary>
    /// Sets MudBlazor's <c>ShrinkLabel</c> for the field's input, overriding the form-level
    /// default (see <c>FormCraftComponent&lt;TModel&gt;.DefaultShrinkLabel</c>). When neither
    /// is configured, fields render with <c>ShrinkLabel="true"</c> — the label stays in its
    /// shrunk position above the input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pass <c>false</c> when using <see cref="MudBlazor.Variant.Text"/>: that variant has no
    /// border for a shrunk label to sit in, so the label should float up from inside the input
    /// on focus instead of being permanently pinned above it.
    /// </para>
    /// <para>
    /// <b>Setting this to <c>false</c> only has a visible effect on an empty field with no
    /// placeholder and no start adornment.</b> MudBlazor combines <c>ShrinkLabel</c> with those
    /// conditions using OR, so a field that has a value, a placeholder (see
    /// <c>WithPlaceholder</c>) or a start adornment (see <c>WithAdornment</c>) keeps its label
    /// pinned regardless. This is MudBlazor's behaviour, not FormCraft's — to get a floating
    /// label on a <see cref="MudBlazor.Variant.Text"/> field, leave its placeholder unset.
    /// </para>
    /// </remarks>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <param name="builder">The FieldBuilder instance.</param>
    /// <param name="shrinkLabel">Whether the label stays shrunk (default: true).</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Name, field => field
    ///     .WithLabel("Name")
    ///     .WithVariant(Variant.Text)
    ///     .WithShrinkLabel(false))
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TValue> WithShrinkLabel<TModel, TValue>(
        this FieldBuilder<TModel, TValue> builder,
        bool shrinkLabel = true)
        where TModel : new()
    {
        return builder.WithAttribute("ShrinkLabel", shrinkLabel);
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
    /// Adds an adornment (icon or text) to a numeric field.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The numeric type of the field value.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a numeric field.</param>
    /// <param name="icon">The MudBlazor icon to display (e.g., Icons.Material.Filled.Numbers).</param>
    /// <param name="position">The position of the adornment (Start or End, default: Start).</param>
    /// <param name="color">The color of the adornment icon (default: Default).</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Constrained to <see cref="INumber{TSelf}"/> rather than to <c>struct</c> so it is not offered
    /// on <c>bool</c> or <c>DateTime</c> fields, where MudCheckBox has no adornment concept at all
    /// and MudDatePicker keeps its own calendar icon (#184).
    /// </para>
    /// <para>
    /// This <b>narrows</b> the set of calls that compile and then do nothing; it does not eliminate
    /// it. The constraint describes the field's CLR type, not the component it ends up rendering, so
    /// a numeric field routed elsewhere — <c>.AsSlider(...)</c>, <c>.AsRating(...)</c>, or any
    /// <c>.WithOptions(...)</c> field, which render MudSlider, MudRating and MudSelect — still
    /// accepts this call and still draws no adornment. Closing that gap needs a renderer-aware
    /// check, which no builder extension can perform at compile time.
    /// </para>
    /// <para>
    /// Takes no <c>onClick</c>: the string overload's parameter is read by neither render path, so
    /// mirroring it here would add a second dead parameter. Wiring <c>OnAdornmentClick</c> through
    /// is tracked separately.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// .AddField(x => x.Quantity)
    ///     .WithAdornment(Icons.Material.Filled.Numbers, MudBlazor.Adornment.End)
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TValue> WithAdornment<TModel, TValue>(
        this FieldBuilder<TModel, TValue> builder,
        string icon,
        MudBlazor.Adornment position = MudBlazor.Adornment.Start,
        MudBlazor.Color color = MudBlazor.Color.Default)
        where TModel : new()
        where TValue : struct, INumber<TValue>
    {
        return builder
            .WithAttribute("Adornment", position)
            .WithAttribute("AdornmentIcon", icon)
            .WithAttribute("AdornmentColor", color);
    }

    /// <summary>
    /// Adds an adornment (icon or text) to a nullable numeric field.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The underlying numeric type of the field value.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a nullable numeric field.</param>
    /// <param name="icon">The MudBlazor icon to display (e.g., Icons.Material.Filled.Percent).</param>
    /// <param name="position">The position of the adornment (Start or End, default: Start).</param>
    /// <param name="color">The color of the adornment icon (default: Default).</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <remarks>
    /// A separate overload because the <c>struct</c> constraint excludes nullable value types, so
    /// <c>int?</c> and <c>decimal?</c> fields do not fall out of the non-nullable one. The two never
    /// compete: <c>TValue</c> binds to <c>int</c> for an <c>int?</c> receiver and to <c>int</c> for
    /// an <c>int</c> receiver, and only one is applicable in each case.
    /// </remarks>
    /// <example>
    /// <code>
    /// .AddField(x => x.Discount)
    ///     .WithAdornment(Icons.Material.Filled.Percent, MudBlazor.Adornment.End)
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TValue?> WithAdornment<TModel, TValue>(
        this FieldBuilder<TModel, TValue?> builder,
        string icon,
        MudBlazor.Adornment position = MudBlazor.Adornment.Start,
        MudBlazor.Color color = MudBlazor.Color.Default)
        where TModel : new()
        where TValue : struct, INumber<TValue>
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