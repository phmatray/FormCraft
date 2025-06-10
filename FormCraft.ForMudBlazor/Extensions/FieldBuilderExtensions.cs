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

}