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
    /// Overrides whether this field renders MudBlazor's native required decoration — the HTML5
    /// <c>required</c> attribute, <c>aria-required</c>, and MudBlazor's required styling (the
    /// asterisk) on the rendered input. Pass <c>false</c> to suppress a decoration that
    /// <c>.Required(...)</c> would otherwise produce.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <param name="builder">The FieldBuilder instance.</param>
    /// <param name="enabled">
    /// <c>true</c> (default) to force the decoration on a field that never called
    /// <c>.Required(...)</c>; <c>false</c> to suppress it on one that did. Either way the explicit
    /// value wins over the inference — this method is an override, not merely an opt-in.
    /// </param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// ⚠️ **This changes no validation.** It is presentation only. <c>.Required("…")</c> is what makes
    /// a field actually required — it registers a validator, and FormCraft's validation is
    /// server-side with messages from the validator you configured. Passing <c>false</c> here
    /// suppresses the decoration and leaves that validation entirely intact.
    /// </para>
    /// <para>
    /// ⛔ **Think twice before passing <c>false</c> on a <c>.Required(...)</c> field.** Since #199 a
    /// required field renders <c>aria-required="true"</c> so assistive technology announces it;
    /// suppressing that puts <c>aria-required="false"</c> back on a genuinely required input, which
    /// states the opposite of the truth to a screen reader and is a WCAG 2.1 3.3.2 (Level A)
    /// failure. If the visible asterisk is what you want gone, restyle the
    /// <c>mud-input-required</c> class instead. Legitimate uses of <c>false</c> are fields whose
    /// requirement is conditional or communicated elsewhere.
    /// </para>
    /// <para>
    /// History: #190 removed the native attribute from <c>.Required(...)</c> because the same call
    /// emitted it inside <c>.WithItemForm(...)</c> and not outside. That divergence is fixed, but
    /// #199 restored the forward on both paths — levelling the two down to silence had left every
    /// required field unannounced. This method is now the per-field override in both directions.
    /// </para>
    /// <para>
    /// Replaces the documented magic string <c>.WithAttribute("Required", true)</c> from #193, which
    /// is undiscoverable and one typo away from silently doing nothing (#204). The raw form still
    /// works and writes the same attribute — this is additive.
    /// </para>
    /// <para>
    /// Forms render <c>novalidate</c> (#206), so the browser does not enforce the attribute on a
    /// FormCraft form; what it buys is the semantics and the styling, not native validation bubbles.
    /// </para>
    /// </remarks>
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
    {
        return builder.WithAttribute("Required", enabled);
    }

    /// <summary>
    /// Configures an input mask on a text field, so the user types into a fixed pattern.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <param name="pattern">
    /// The mask pattern. <c>0</c> accepts a digit, <c>a</c> a letter and <c>*</c> either; every other
    /// character is a literal the mask inserts as the user types. A blank or whitespace-only pattern
    /// configures no mask at all — see the remarks.
    /// </param>
    /// <param name="cleanDelimiters">
    /// <c>false</c> (the default) stores the delimited text on the model — <c>"(555) 123-4567"</c>.
    /// <c>true</c> strips the pattern's literals first, storing <c>"5551234567"</c>.
    /// </param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Replaces the magic string <c>.WithAttribute("Mask", "…")</c> that #211 made functional, for
    /// the reason #204 replaced <c>.WithAttribute("Required", true)</c> with
    /// <see cref="WithNativeRequired{TModel,TValue}"/>: a key the caller spells is undiscoverable and
    /// one typo away from silently doing nothing. The raw form still works and writes the same
    /// attribute — this is additive, not a migration.
    /// </para>
    /// <para>
    /// <b><paramref name="cleanDelimiters"/> changes what the model stores</b>, which is why it is
    /// opt-in and defaults to the behaviour #211 shipped. It maps onto
    /// <c>PatternMask.CleanDelimiters</c>, which decides whether <c>GetCleanText()</c> strips the
    /// literals; FormCraft never set it before #265, so a masked field always wrote the punctuation
    /// to the model and storage or validators keyed to raw input had no way to ask for anything else.
    /// </para>
    /// <para>
    /// A blank or whitespace-only <paramref name="pattern"/> resolves to no mask rather than to an
    /// empty one, and <paramref name="cleanDelimiters"/> does not override that (#211). An empty
    /// <c>PatternMask("")</c> would route an otherwise ordinary text field through <c>MudMask</c> —
    /// which also drops <c>MaxLines</c> — and a whitespace pattern would additionally accept no
    /// input at all.
    /// </para>
    /// <para>
    /// Applies to the text render path only. Numeric and date fields do not consult
    /// <see cref="TextMaskMap"/>, so this call compiles on a string field and is the only place it
    /// has an effect.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// .AddField(x => x.Phone, field => field
    ///     .WithMask("(000) 000-0000"))                       // model stores "(555) 123-4567"
    ///
    /// .AddField(x => x.Phone, field => field
    ///     .WithMask("(000) 000-0000", cleanDelimiters: true)) // model stores "5551234567"
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> WithMask<TModel>(
        this FieldBuilder<TModel, string> builder,
        string pattern,
        bool cleanDelimiters = false)
        where TModel : new()
    {
        return builder
            .WithAttribute(TextMaskMap.AttributeName, pattern)
            .WithAttribute(TextMaskMap.CleanDelimitersAttribute, cleanDelimiters)
            // Clears any factory a previous call configured, so the last WithMask on a field wins.
            // Without this the two overloads would accumulate instead of override, and a caller
            // refining a shared helper's mask would find their own later call silently ignored.
            .WithAttribute(TextMaskMap.MaskFactoryAttribute, null!);
    }

    /// <summary>
    /// Configures a MudBlazor mask of your own construction on a text field — <c>RegexMask</c>,
    /// <c>BlockMask</c>, <c>MultiMask</c>, or a <c>PatternMask</c> you configured yourself.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="maskFactory">
    /// Builds the mask. Called <b>once per rendered field</b>, so every field — and every row of a
    /// collection — gets its own instance. Must return the same implementation type on every call;
    /// see the remarks.
    /// </param>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// <b>A factory rather than an instance, deliberately.</b> A mask is not a value:
    /// <c>BaseMask</c> carries the live <c>Text</c>, <c>CaretPos</c> and <c>Selection</c> of the
    /// input it is attached to. One field configuration is shared by every row of a collection, so
    /// an <see cref="MudBlazor.IMask"/> stored in it would be handed to every row — and
    /// <c>MudMask.SetMask</c> does not defensively copy: it keeps the instance it is given
    /// (<c>_mask = other</c>) whenever the type differs from the <c>PatternMask</c> it seeds itself
    /// with, which is the case for every <c>RegexMask</c>, <c>BlockMask</c> and <c>MultiMask</c> —
    /// exactly the types this overload exists to reach. Two rows would then share one editing state.
    /// Taking a factory makes that unrepresentable, and keeps the built configuration immutable as
    /// <c>CLAUDE.md</c> requires.
    /// </para>
    /// <para>
    /// Before #265 this configuration was unreachable. <c>.WithAttribute("Mask", new RegexMask(…))</c>
    /// — the natural thing to write — compiled, built and rendered while doing nothing at all: both
    /// render paths read that key as <c>string?</c>, and an <see cref="MudBlazor.IMask"/> fails the
    /// <c>value is T</c> test and falls back to <c>null</c>. This overload writes a separate,
    /// correctly-typed key instead.
    /// </para>
    /// <para>
    /// <b>Return the same type every time.</b> <c>MudMask.SetMask</c> preserves the user's text and
    /// caret only when the incoming mask matches the type it already holds; a factory that varied
    /// its return type would reset the field mid-edit, since a render happens on every keystroke.
    /// </para>
    /// <para>
    /// The last <c>WithMask</c> call on a field wins: this one clears a pattern configured earlier,
    /// and the pattern overload clears a factory configured earlier. <c>cleanDelimiters</c> does not
    /// apply here — set the equivalent on the mask you construct.
    /// </para>
    /// <para>
    /// A regex mask is matched against <b>partial</b> input, so its pattern must accept prefixes:
    /// use open-ended quantifiers like <c>^[0-9]{0,5}$</c>. An exact <c>^[0-9]{5}$</c> never matches
    /// a shorter prefix and blocks every keystroke.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// .AddField(x => x.Pin, field => field
    ///     .WithMask(() => new RegexMask("^[0-9]{0,4}$")))
    /// </code>
    /// </example>
    public static FieldBuilder<TModel, string> WithMask<TModel>(
        this FieldBuilder<TModel, string> builder,
        Func<MudBlazor.IMask> maskFactory)
        where TModel : new()
    {
        ArgumentNullException.ThrowIfNull(maskFactory);

        return builder
            .WithAttribute(TextMaskMap.MaskFactoryAttribute, maskFactory)
            // Clears a pattern configured earlier, so the last WithMask on a field wins.
            .WithAttribute(TextMaskMap.AttributeName, null!)
            .WithAttribute(TextMaskMap.CleanDelimitersAttribute, false);
    }

    /// <summary>
    /// Adds an adornment (icon or text) to a text field.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a string field.</param>
    /// <param name="icon">The MudBlazor icon to display (e.g., Icons.Material.Filled.Email).</param>
    /// <param name="position">The position of the adornment (Start or End, default: Start).</param>
    /// <param name="color">The color of the adornment icon (default: Default).</param>
    /// <param name="onClick">
    /// Optional click handler for the adornment icon. It receives the field's current value, and
    /// fires on both render paths — an ordinary field and one inside <c>.WithItemForm(...)</c>.
    /// </param>
    /// <returns>The FieldBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddField(x => x.Email)
    ///     .WithAdornment(Icons.Material.Filled.Email, MudBlazor.Adornment.Start)
    ///
    /// .AddField(x => x.Query)
    ///     .WithAdornment(Icons.Material.Filled.Search, onClick: value => Search(value))
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
            .WithAttribute("AdornmentColor", color)
            // Written unconditionally — a null handler must OVERWRITE one an earlier call left
            // behind, not be skipped. Writing it only when supplied made this method a partial
            // overwrite: a reusable helper that set a handler, refined by a caller asking for a
            // plain decorative icon, would keep firing the helper's handler from an icon the
            // caller believes is inert. Both readers resolve the value with `is Action<string?>`,
            // so a stored null reads back as "no handler" exactly like an absent key would.
            .WithAttribute(AdornmentClickAttribute, onClick!);
    }

    /// <summary>
    /// Attribute key under which <see cref="WithAdornment{TModel}"/> stores its click handler, and
    /// which both render paths read it back from. Shared so the two cannot drift apart (#192).
    /// </summary>
    internal const string AdornmentClickAttribute = "OnAdornmentClick";

    /// <summary>
    /// Adds an adornment (icon or text) to a numeric field.
    /// </summary>
    /// <typeparam name="TModel">The model type that the form binds to.</typeparam>
    /// <typeparam name="TValue">The numeric type of the field value.</typeparam>
    /// <param name="builder">The FieldBuilder instance for a numeric field.</param>
    /// <param name="icon">The MudBlazor icon to display (e.g., Icons.Material.Filled.Numbers).</param>
    /// <param name="position">The position of the adornment (Start or End, default: Start).</param>
    /// <param name="color">The color of the adornment icon (default: Default).</param>
    /// <param name="onClick">
    /// Optional click handler for the adornment icon. It receives the field's current value, typed
    /// to the field's own value type rather than the string overload's <c>string?</c> (#215), and
    /// fires on both render paths — an ordinary field and one inside <c>.WithItemForm(...)</c>.
    /// </param>
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
    /// Takes no <c>onClick</c>. The original reason — that the string overload's parameter was read
    /// by neither render path — expired with #192, which made that parameter live on both. What
    /// keeps it off these overloads now is the shape question #192 deferred: <c>Action&lt;string?&gt;</c>
    /// is right there only because the value happens to be a string, so the numeric counterpart is
    /// <c>Action&lt;TValue?&gt;</c> — which makes these two methods rather than one generic. Decided
    /// once in the follow-up, not copied per overload.
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
        MudBlazor.Color color = MudBlazor.Color.Default,
        Action<TValue?>? onClick = null)
        where TModel : new()
        where TValue : struct, INumber<TValue>
    {
        return builder
            .WithAttribute("Adornment", position)
            .WithAttribute("AdornmentIcon", icon)
            .WithAttribute("AdornmentColor", color)
            // Written unconditionally, for the reason spelled out on the string overload: a null must
            // OVERWRITE a handler an earlier call left behind, not be skipped.
            .WithAttribute(AdornmentClickAttribute, onClick!);
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
    /// <param name="onClick">
    /// Optional click handler for the adornment icon. It receives the field's current value, typed
    /// to the field's own value type rather than the string overload's <c>string?</c> (#215), and
    /// fires on both render paths — an ordinary field and one inside <c>.WithItemForm(...)</c>.
    /// </param>
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
        MudBlazor.Color color = MudBlazor.Color.Default,
        Action<TValue?>? onClick = null)
        where TModel : new()
        where TValue : struct, INumber<TValue>
    {
        return builder
            .WithAttribute("Adornment", position)
            .WithAttribute("AdornmentIcon", icon)
            .WithAttribute("AdornmentColor", color)
            // Unconditional, as on the sibling overloads.
            .WithAttribute(AdornmentClickAttribute, onClick!);
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