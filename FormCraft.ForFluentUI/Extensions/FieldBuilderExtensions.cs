namespace FormCraft.ForFluentUI.Extensions;

/// <summary>
/// Fluent UI-specific <see cref="FieldBuilder{TModel, TValue}"/> extensions.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not in namespace <c>FormCraft</c>. The MudBlazor package publishes
/// <c>MudBlazorFieldBuilderExtensions</c> into that namespace with methods of these names, so
/// declaring these there too would make them ambiguous for any project that referenced both.
/// </para>
/// <para>
/// ⚠️ <b>The separate namespace does not by itself make the two coexist.</b> Extension-method
/// lookup considers every imported namespace at once, so a project that references both packages
/// and writes <c>using FormCraft;</c> (which it must, for <c>FormBuilder</c>) alongside
/// <c>using FormCraft.ForFluentUI.Extensions;</c> still gets <c>CS0121</c> on a
/// <c>.WithNativeRequired(...)</c> or <c>.AsLookup(...)</c> call - the signatures are identical.
/// What the separate namespace buys is that the ambiguity is <i>avoidable</i>: it does not arise at
/// all in the normal case of one adapter per application, and where both are referenced the call
/// can be disambiguated by invoking the method statically
/// (<c>FluentUIFieldBuilderExtensions.WithNativeRequired(field)</c>) or with an
/// <c>extern alias</c>. This library's own test project takes the static route, because it
/// references both adapters in order to prove they refuse to co-register.
/// </para>
/// </remarks>
public static class FluentUIFieldBuilderExtensions
{
    // ⛔ No WithNativeRequired here.
    //
    // #279 moved it into core (`FormCraft.FieldBuilderExtensions.WithNativeRequired`), which is the
    // outcome this issue's plan anticipated: "check whether the shared-machinery issue has moved
    // WithNativeRequired into core - if it has, this task is only a test that Fluent honours it".
    // It has, so a Fluent copy would not merely be redundant: core's lives in namespace `FormCraft`,
    // which every consumer imports for FormBuilder, so two identical signatures in scope would make
    // `.WithNativeRequired(...)` CS0121-ambiguous for EVERY Fluent user rather than only for the
    // rare project referencing both adapters.
    //
    // Fluent honours the core method through NativeRequired.Resolve in
    // FluentUIFieldComponentBase.EffectiveNativeRequired; NativeRequiredBuilderTests pins that.

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
