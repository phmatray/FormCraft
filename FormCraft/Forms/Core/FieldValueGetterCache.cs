using System.Runtime.CompilerServices;

namespace FormCraft;

/// <summary>
/// Caches the compiled value getter of each field configuration, so reading a field's value emits IL
/// for its expression at most once rather than once per render (#269) and once per validation (#312).
/// </summary>
/// <typeparam name="TModel">The model the cached getters read from.</typeparam>
/// <remarks>
/// <para>
/// Keyed by configuration <b>instance</b>, which buys two properties: two configurations over the same
/// property never share an entry, and an entry lives no longer than the configuration it describes, so
/// nothing is held alive artificially.
/// </para>
/// <para>
/// What is cached is the <b>getter</b>, never the value it returns. The delegate takes the model as its
/// parameter, so every caller still reads the model afresh — caching a value here would freeze each
/// field at its first-read content, and would make validation judge a field the user has already
/// corrected.
/// </para>
/// <para>
/// This is deliberately a shared helper rather than a memo on <see cref="FieldConfigurationWrapper{TModel, TValue}" />.
/// Every call site — the renderer service and both validators — holds an
/// <see cref="IFieldConfiguration{TModel, TValue}" />, not the concrete wrapper, because
/// <c>IFormConfiguration.Fields</c> is a list of the interface. A property on the wrapper would
/// therefore be unreachable from all of them without either widening that public interface or a
/// type test against unknown generic arguments; keying off the instance costs neither.
/// </para>
/// <para>
/// The entry is never invalidated, so this assumes a configuration's <c>ValueExpression</c> keeps
/// reading the same member for that configuration's lifetime. That is the fluent builder's
/// immutable-after-<c>Build()</c> contract, and <see cref="FieldConfiguration{TModel, TValue}" />
/// enforces it by assigning the expression in its constructor. This binds **both** paths: a custom
/// <see cref="IFieldConfiguration{TModel, TValue}" /> whose <c>ValueExpression</c> re-targets itself
/// after first use — by a flag, a culture, a discriminator — keeps being read through whichever
/// member was compiled first, whether that first read came from rendering or from validation. Before
/// #312 validation re-read it every pass, so such an implementation happened to work there; it does
/// not any more. Use a separate configuration instance per binding instead.
/// </para>
/// <para>
/// <b>This is the only cache of a compiled value getter — deliberately.</b> #269 introduced an
/// equivalent one private to <see cref="FieldRendererService" />; #312 retired it in favour of this
/// one rather than leave two mechanisms for one concept. Both adapters' custom-template paths route
/// through here too since #330 — before that they read the value with per-render
/// <c>GetProperty</c>/<c>GetValue</c> reflection keyed on <c>FieldName</c>, which is only the
/// expression's last member (e.g. <c>"Value"</c> for <c>x =&gt; x.Nested.Value</c>) and so silently
/// failed to resolve — and therefore rendered nothing — for any binding but a direct top-level
/// property. Because every path keys off the same configuration instance, a field that is rendered
/// <i>and</i> validated now compiles its getter once in total, not once per path.
/// The alternative considered and rejected was widening
/// <see cref="IFieldConfiguration{TModel, TValue}" /> with a compiled-getter member: it would let each
/// configuration own its memo, but it is a public API change binding on every external implementer,
/// for no behaviour a per-instance key does not already provide.
/// </para>
/// <para>
/// <b>Public rather than internal</b> — like <see cref="AdapterRegistration"/> and
/// <see cref="NativeRequired"/>, this is core machinery a first-party adapter assembly needs to call
/// (#330), and this package has no <c>InternalsVisibleTo</c> to either adapter. It stays out of
/// <see cref="IFieldConfiguration{TModel, TValue}" /> itself for the same reason noted above; being
/// public here is a smaller commitment than widening that interface.
/// </para>
/// </remarks>
public static class FieldValueGetterCache<TModel>
{
    private static readonly ConditionalWeakTable<IFieldConfiguration<TModel, object>, Func<TModel, object>> Cache = new();

    /// <summary>
    /// Returns the field's compiled value getter, compiling it on first use and reusing it thereafter.
    /// </summary>
    /// <param name="field">The field configuration whose value expression to compile.</param>
    /// <returns>A delegate reading the field's value from a model instance.</returns>
    public static Func<TModel, object> GetOrCompile(IFieldConfiguration<TModel, object> field)
        => Cache.GetValue(field, static configuration => configuration.ValueExpression.Compile());

    /// <summary>
    /// Invokes the field's compiled value getter against <paramref name="model"/>, returning
    /// <see langword="false"/> instead of throwing when the read fails.
    /// </summary>
    /// <remarks>
    /// The most common failure is a null intermediate in a nested <c>ValueExpression</c> path (e.g.
    /// <c>x =&gt; x.Nested.Value</c> when <c>Nested</c> is <see langword="null"/>) — nothing stops a
    /// caller from binding a field this way, and before this method existed the three callers that did
    /// not already guard their own read (#330 guards the two adapters' custom-template reads) let that
    /// exception escape straight through rendering or validation (#397). Catches <see cref="Exception"/>,
    /// not a narrower type, since the read invokes an arbitrary compiled expression that can fail for
    /// any reason a delegate call can.
    /// </remarks>
    /// <param name="field">The field configuration whose value expression to read.</param>
    /// <param name="model">The model instance to read the value from.</param>
    /// <param name="value">The read value on success; <see langword="null"/> when the read fails.</param>
    /// <returns><see langword="true"/> if the value was read successfully; otherwise <see langword="false"/>.</returns>
    public static bool TryGetValue(IFieldConfiguration<TModel, object> field, TModel model, out object? value)
    {
        try
        {
            value = GetOrCompile(field)(model);
            return true;
        }
        catch (Exception)
        {
            value = null;
            return false;
        }
    }
}
