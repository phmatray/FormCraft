using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Reports a stored value that a configured mask rendered as blank (#266).
/// </summary>
/// <remarks>
/// <para>
/// The mechanism is MudBlazor's, not FormCraft's: on the first parameter pass <c>MudBaseInput</c>
/// builds its display text by running the value through the mask with <c>updateValue: false</c>. A
/// value the mask rejects collapses to empty, and because <c>updateValue</c> is false the model is
/// never written back. The developer sees a blank field, the user submits without touching it, and
/// the non-conforming value survives — with nothing logged and nothing thrown.
/// </para>
/// <para>
/// Not new MudBlazor behaviour, but newly *reachable*: until #211 the <c>Mask</c> attribute did
/// nothing at all, so no FormCraft field could hit it. Turning masking on turned this on with it,
/// which makes it an upgrade hazard for exactly the forms most likely to hold legacy data — the ones
/// that already wrote <c>.WithAttribute("Mask", …)</c> and got nothing for it.
/// </para>
/// <para>
/// This reports; it never repairs. Writing the masked result back would make the display and the
/// model agree by *destroying* the stored value — <c>"N/A"</c> becomes <c>""</c> in the database
/// because a form was rendered, on read-only views too. Mutating user data as a side effect of
/// display is a worse failure than the one being reported.
/// </para>
/// <para>
/// Modelled on <see cref="MaskedLinesDiagnostic"/>, including keeping the rule for *whether* to warn
/// in <see cref="Applies"/> rather than re-deriving it at the call site.
/// </para>
/// </remarks>
internal static class MaskedValueDiagnostic
{
    /// <summary>Logger category for the masked-value diagnostic.</summary>
    internal const string Category = "FormCraft.ForMudBlazor.MaskedValue";

    /// <summary>
    /// Whether the mask changed what the stored value <i>means</i> — by blanking it, or by discarding
    /// part of it — rather than merely reformatting it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Difference is not the signal. A mask that turns <c>5551234567</c> into <c>(555) 123-4567</c>
    /// is doing precisely its job; warning whenever the two strings differ would fire on every
    /// correctly-masked field in every form, and a diagnostic that fires on the happy path is one the
    /// developer mutes — taking the real signal with it. <b>Loss</b> is the signal.
    /// </para>
    /// <para>
    /// #266 measured loss as total collapse: a value went in and nothing came out. #283 found that
    /// too narrow. <c>"+1 555 123 4567"</c> under <c>(000) 000-0000</c> renders
    /// <c>(155) 512-3456</c> — a perfectly plausible phone number that is not the one on record,
    /// shown to the user while the model keeps the original. That is the same divergence as the blank
    /// case and a worse one to be left undiagnosed, because a blank field is visibly wrong and this
    /// is not.
    /// </para>
    /// <para>
    /// The widened rule strips the mask's own literals from both sides and compares what is left. A
    /// reformat only ever moves literals around, so the two sides reduce to the same characters; a
    /// discard loses characters no reformatting could restore. Stripping <i>both</i> sides is what
    /// keeps stored data that carries its own separators — <c>555 123 4567</c>, <c>555-123-4567</c>,
    /// the normal shape of legacy data — from reading as a discard.
    /// </para>
    /// <para>
    /// The blank test stays an explicit disjunct rather than being folded into that comparison,
    /// because a value made only of the mask's literals (<c>"()-"</c>) reduces to the empty string on
    /// both sides and would otherwise read as a reformat — silently dropping a case #266 reported.
    /// Both blank tests are <see cref="string.IsNullOrWhiteSpace"/>: on the value side a
    /// whitespace-only string is a blank field wearing a different hat — a padded column, a
    /// trimmed-to-blank setting — and there is nothing to lose, the same reading
    /// <see cref="TextMaskMap.Resolve"/> gives a whitespace-only <i>pattern</i>; on the result side a
    /// mask whose surviving output is only its own literal spacing has kept nothing of what was
    /// stored.
    /// </para>
    /// </remarks>
    /// <param name="configuredValue">The value held by the model before the mask was applied.</param>
    /// <param name="maskedResult">What that value renders as once run through the mask.</param>
    /// <param name="maskLiterals">
    /// The characters the mask inserts as decoration, from <see cref="LiteralsOf"/>, or <c>null</c>
    /// when they could not be determined — in which case only the total-collapse test applies.
    /// </param>
    internal static bool Applies(string? configuredValue, string? maskedResult, string? maskLiterals)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return false;
        }

        // #266's rule, unchanged and checked first: something was stored and nothing came out.
        if (string.IsNullOrWhiteSpace(maskedResult))
        {
            return true;
        }

        // LiteralsOf could not answer without guessing (a non-pattern or transforming mask). Report
        // nothing beyond the collapse case above rather than invent a verdict — the cost of a wrong
        // "yes" here is a warning on every value of a correctly configured field.
        if (maskLiterals is null)
        {
            return false;
        }

        return !string.Equals(
            WithoutLiterals(configuredValue, maskLiterals),
            WithoutLiterals(maskedResult, maskLiterals),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// <paramref name="value"/> with every one of the mask's literal characters removed.
    /// </summary>
    /// <remarks>
    /// Applied to the stored value and the rendered text alike, which is the whole point: it reduces
    /// both to the characters the mask treats as significant, so the comparison sees data rather than
    /// punctuation. Ordinal throughout — these are literal characters from a pattern, not text being
    /// collated.
    /// </remarks>
    /// <param name="value">The stored value or the rendered text.</param>
    /// <param name="literals">The mask's literal characters, from <see cref="LiteralsOf"/>.</param>
    private static string WithoutLiterals(string value, string literals) =>
        string.Concat(value.Where(c => !literals.Contains(c)));

    /// <summary>
    /// The characters <paramref name="mask"/> inserts as decoration, or <c>null</c> when they cannot
    /// be determined (#283).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the input the discard rule needs, and it exists because the obvious source does not
    /// work. The issue assumed <c>PatternMask.GetCleanText()</c> returns the mask-significant
    /// characters; measured against MudBlazor 9.8.0 it returns <c>Text</c> verbatim unless
    /// <c>CleanDelimiters</c> is set — which FormCraft leaves <c>false</c> by default — so a rule
    /// built on it would have compared a formatted string against a raw one and fired on every
    /// correctly-masked field. Pinned by <c>MaskedValueDiagnosticTests</c>'s characterisation block.
    /// </para>
    /// <para>
    /// Derived from the pattern rather than hardcoded: a literal is any pattern character that is not
    /// one of the mask's own placeholders, and <c>MaskChars</c> is where those live. Reading them off
    /// the instance keeps this correct for a caller who supplies custom <c>MaskChars</c> through the
    /// #265 factory, which a hardcoded <c>0</c>/<c>a</c>/<c>*</c> list would silently get wrong.
    /// </para>
    /// <para>
    /// Returns <c>null</c> — meaning "no opinion", which <see cref="Applies"/> reads as a fall back to
    /// the #266 total-collapse rule — in the two cases where an answer would be a guess:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// a mask that is not a <see cref="PatternMask"/>. The #265 factory can supply any
    /// <see cref="IMask"/>, and a <c>RegexMask</c>'s <c>Mask</c> is a regular expression whose
    /// non-placeholder characters are metacharacters, not decoration.
    /// </item>
    /// <item>
    /// a mask carrying a <c>Transformation</c>. That hook rewrites characters as they are consumed —
    /// upper-casing, say — so the rendered text legitimately differs from the stored value character
    /// for character, and every value would read as a discard. A diagnostic that fires on every value
    /// of a correctly configured field is the happy-path false positive #266 was shaped to avoid, and
    /// reporting it as "the mask discarded input" would be the wrong explanation as well as the wrong
    /// verdict.
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="mask">The mask that produced the rendered text.</param>
    internal static string? LiteralsOf(IMask mask)
    {
        if (mask is not PatternMask pattern
            || pattern.Mask is null
            || pattern.MaskChars is null
            || pattern.Transformation is not null)
        {
            return null;
        }

        var placeholders = pattern.MaskChars.Select(maskChar => maskChar.Char).ToHashSet();

        return new string(pattern.Mask.Where(c => !placeholders.Contains(c)).Distinct().ToArray());
    }

    /// <summary>
    /// Emits the warning, degrading silently when no logging stack is registered.
    /// </summary>
    /// <param name="services">Provider used to resolve an optional <see cref="ILoggerFactory"/>.</param>
    /// <param name="fieldName">The field's name, used when it has no label.</param>
    /// <param name="label">Display name for the message.</param>
    /// <param name="pattern">The configured mask, quoted back so the message is concrete.</param>
    /// <param name="maskedResult">
    /// What the field actually displays. It selects the wording, and it is the difference between a
    /// useful report and a misleading one: the blank case and the partial-discard case share a cause
    /// but look nothing alike to the person reading the log. Telling someone their field "renders
    /// empty" when it is in fact showing a plausible wrong phone number sends them looking for a
    /// blank field they will never find (#283).
    /// </param>
    internal static void Warn(
        IServiceProvider? services,
        string fieldName,
        string? label,
        string? pattern,
        string? maskedResult)
    {
        // A diagnostic must never break a render, so everything here — including the service
        // resolution, which is the one call that can realistically fail on a torn-down circuit —
        // is inside the guard.
        try
        {
            var logger = services?
                .GetService<ILoggerFactory>()?
                .CreateLogger(Category);

            var field = string.IsNullOrWhiteSpace(label) ? fieldName : label;

            // The shared half of both messages: WHY the divergence persists, and what to do about it.
            const string Mechanism =
                "MudBlazor builds the display text by running the value through the mask and does " +
                "not write the result back, so a user who submits without touching this field " +
                "leaves the stored value unchanged. Correct the data, widen the mask, or drop the " +
                "mask if the stored format is intentional.";

            if (string.IsNullOrWhiteSpace(maskedResult))
            {
                logger?.LogWarning(
                    "Field '{Field}' holds a value that its mask '{Mask}' rejects, so the field " +
                    "renders empty while the model keeps the original. " + Mechanism,
                    field,
                    pattern);

                return;
            }

            // The #283 case. It names what is on screen because that is the only way the developer
            // can recognise it: nothing looks wrong, so "your field shows (155) 512-3456" is the
            // fact that turns an abstract warning into something checkable against the record.
            logger?.LogWarning(
                "Field '{Field}' holds a value that its mask '{Mask}' only partly accepts, so the " +
                "field displays '{Displayed}' — which is not what the model holds. " + Mechanism,
                field,
                pattern,
                maskedResult);
        }
        catch
        {
            // Ignored: a failing diagnostic must not take the form down with it.
        }
    }
}
