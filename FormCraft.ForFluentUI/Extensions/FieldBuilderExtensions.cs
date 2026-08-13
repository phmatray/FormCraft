namespace FormCraft.ForFluentUI.Extensions;

/// <summary>
/// Fluent UI-specific <see cref="FieldBuilder{TModel, TValue}"/> extensions.
/// </summary>
/// <remarks>
/// ⚠️ <b>Deliberately not in namespace <c>FormCraft</c>.</b> The MudBlazor package publishes
/// <c>MudBlazorFieldBuilderExtensions</c> into that namespace with methods of these names, and a
/// project referencing both packages would get <c>CS0121</c> on every call. Living here means the
/// two can coexist and a consumer opts in with <c>using FormCraft.ForFluentUI.Extensions;</c>.
/// </remarks>
public static class FluentUIFieldBuilderExtensions
{
    /// <summary>
    /// Marks the field as required for presentation purposes, so the Fluent input renders its
    /// required decoration and announces <c>aria-required="true"</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Independent of <c>.Required(...)</c>, which adds the <b>validator</b>. This adds the
    /// <b>decoration</b>, and wins over the validator's answer in <b>both</b> directions: a field
    /// that calls this without <c>.Required(...)</c> is announced as required, and one that calls
    /// <c>.WithNativeRequired(false)</c> stays silent even when <c>.Required(...)</c> is configured
    /// (#199, #204). Only an unconfigured field falls through to the validator.
    /// </para>
    /// <para>
    /// Replaces the magic string <c>.WithAttribute("Required", true)</c> the Fluent README used to
    /// document, which is undiscoverable and one typo away from silently doing nothing. The raw form
    /// still works and writes the same attribute - this is additive, and
    /// <c>FluentUIFieldComponentBase.EffectiveNativeRequired</c> reads the one attribute either way.
    /// </para>
    /// <para>
    /// Forms render <c>novalidate</c> (#206), so the browser enforces nothing here; what this buys
    /// is the semantics for assistive technology and the visual marker, not validation bubbles.
    /// </para>
    /// </remarks>
    /// <typeparam name="TModel">The model type the form binds to.</typeparam>
    /// <typeparam name="TValue">The field's value type.</typeparam>
    /// <param name="builder">The field builder.</param>
    /// <param name="enabled">Whether the decoration is rendered. Defaults to <c>true</c>.</param>
    /// <returns>The field builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Email, field => field
    ///     .Required("Email is required")   // the validation
    ///     .WithNativeRequired())           // the decoration
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, TValue> WithNativeRequired<TModel, TValue>(
        this FieldBuilder<TModel, TValue> builder,
        bool enabled = true)
        where TModel : new()
        => builder.WithAttribute("Required", enabled);

    /// <summary>
    /// Configures the field as a lookup: a read-only display with a browsable grid of candidate
    /// rows, from which the user picks the value.
    /// </summary>
    /// <remarks>
    /// The attribute keys written here are the same ones the MudBlazor adapter's <c>.AsLookup(...)</c>
    /// writes, and the same ones <c>FluentUILookupFieldRenderer</c> matches on. That is deliberate:
    /// the attributes are the wire format between builder and renderer, so a configuration built
    /// with either package's extension renders correctly under either adapter. Changing a key here
    /// without changing it there silently stops the field rendering as a lookup - it would fall
    /// through to the plain type-based renderer instead.
    /// </remarks>
    /// <typeparam name="TModel">The model type the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <typeparam name="TItem">The type of the row displayed in the lookup grid.</typeparam>
    /// <param name="builder">The field builder.</param>
    /// <param name="dataProvider">An async function returning the candidate rows for a query.</param>
    /// <param name="valueSelector">Extracts the field value from a chosen row.</param>
    /// <param name="displaySelector">Extracts the display text from a chosen row.</param>
    /// <param name="configureColumns">Optionally configures the grid's columns.</param>
    /// <param name="onItemSelected">Optionally maps further model properties from the chosen row.</param>
    /// <returns>The field builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.CityId, f => f.AsLookup&lt;MyModel, int, CityDto&gt;(
    ///     dataProvider: query => FetchCitiesAsync(query),
    ///     valueSelector: city => city.Id,
    ///     displaySelector: city => city.Name))
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
        {
            builder.WithAttribute("LookupOnItemSelected", onItemSelected);
        }

        return builder;
    }
}
