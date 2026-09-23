using System.Globalization;
using System.Resources;

namespace FormCraft;

/// <summary>
/// Looks up default validation messages from the embedded <c>ValidationMessages.resx</c> resource
/// (and its satellite translations) under <see cref="CultureInfo.CurrentUICulture"/>, formatting
/// arguments under <see cref="CultureInfo.CurrentCulture"/>.
/// </summary>
/// <remarks>
/// Internal by design (#354): there is no public override hook yet. Localizing a message means
/// adding a <c>ValidationMessages.&lt;culture&gt;.resx</c> satellite with the same keys, the same
/// way FluentValidation's own <c>LanguageManager</c> is contributed to.
/// </remarks>
internal static class ValidationMessages
{
    // NOT "FormCraft.Resources.ValidationMessages" — the SDK's default EmbeddedResource manifest
    // naming for this project does not prefix the containing folder, so the file embeds as
    // "FormCraft.ValidationMessages.resources". Proven at runtime (a MissingManifestResourceException
    // names the real embedded resource) rather than assumed; ValidationMessagesLocalizationTests
    // pins it via the same ResourceManager base name.
    private static readonly ResourceManager Resources =
        new("FormCraft.ValidationMessages", typeof(ValidationMessages).Assembly);

    private static string Get(string key)
        => Resources.GetString(key, CultureInfo.CurrentUICulture)
           ?? throw new InvalidOperationException($"Missing validation message resource '{key}'.");

    // Nullable object parameters throughout: BuildRangeMessage<TValue> and friends call these with a
    // generic TValue that the compiler cannot prove non-null (e.g. a nullable numeric TValue), and
    // string.Format renders a null argument as an empty string — the same thing `$"{min}"`
    // interpolation on a null did before this change, so behaviour is unchanged.
    public static string Required(object? label)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(Required)), label);

    public static string RequiredDefault()
        => Get(nameof(RequiredDefault));

    public static string RequiredSelect(object? label)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(RequiredSelect)), label);

    public static string RequiredAtLeastOne(object? label)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(RequiredAtLeastOne)), label);

    public static string MinLength(object? n)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(MinLength)), n);

    public static string MinLengthLong(object? n)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(MinLengthLong)), n);

    public static string MaxLength(object? n)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(MaxLength)), n);

    public static string MaxLengthLong(object? n)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(MaxLengthLong)), n);

    public static string RangeBetween(object? min, object? max)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(RangeBetween)), min, max);

    public static string RangeAtLeast(object? min)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(RangeAtLeast)), min);

    public static string RangeAtMost(object? max)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(RangeAtMost)), max);

    public static string InvalidFormat()
        => Get(nameof(InvalidFormat));

    public static string InvalidEmail()
        => Get(nameof(InvalidEmail));

    public static string InvalidPhone()
        => Get(nameof(InvalidPhone));

    public static string AmountPositive()
        => Get(nameof(AmountPositive));

    public static string PercentageRange()
        => Get(nameof(PercentageRange));

    public static string SpecialCharacterRequired()
        => Get(nameof(SpecialCharacterRequired));

    public static string CollectionMinItems(string label, int n)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(CollectionMinItems)), label, n);

    public static string CollectionMaxItems(string label, int n)
        => string.Format(CultureInfo.CurrentCulture, Get(nameof(CollectionMaxItems)), label, n);
}
