using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Reports a stored value that a configured mask blanked (#266) or partly discarded (#283).
/// </summary>
/// <remarks>
/// <para>
/// The mechanism is MudBlazor's, not FormCraft's: <c>MudBaseInput</c> builds its display text by
/// running the value through the mask with <c>updateValue: false</c>. Whatever the mask cannot place
/// is dropped, and because <c>updateValue</c> is false the model is never written back. The user
/// submits without touching the field and the non-conforming value survives — with nothing logged
/// and nothing thrown.
/// </para>
/// <para>
/// Two shapes, one consequence. The value collapses to empty, which is at least visibly wrong; or
/// the mask keeps the characters that happen to fit and drops the rest, which is not visibly wrong
/// at all — <c>"+1 555 123 4567"</c> under <c>(000) 000-0000</c> displays <c>(155) 512-3456</c>, a
/// plausible phone number that is not the one on record. #266 reported only the first; #283 widened
/// it to both, on the grounds that the display and the model diverge either way and the silent shape
/// is the harder one to notice.
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
    /// The widened rule reduces both sides to the characters that carry the <i>data</i> — dropping
    /// punctuation and the mask's own decoration — and compares those. A reformat only rearranges
    /// decoration, so the two sides reduce to the same characters; a discard loses characters no
    /// reformatting could restore.
    /// </para>
    /// <para>
    /// "Carries the data" is two conditions, and getting it down to one produces false positives in
    /// opposite directions. Dropping only the <b>mask's literals</b> leaves the stored value's own
    /// punctuation in place, so <c>000-00-0000</c> over a stored <c>"123 45 6789"</c> compares
    /// <c>"123 45 6789"</c> against <c>"123456789"</c> and reports a discard for data that is
    /// completely intact — legacy data carries its own separators, and they are rarely the pattern's.
    /// Dropping only <b>non-alphanumerics</b> fails the other way: a pattern may spell a literal that
    /// is itself alphanumeric (<c>+1 000-0000</c> contributes a <c>1</c>), which the rendered side
    /// then holds and the stored side does not. So both apply: keep a character only if it could
    /// match one of the mask's placeholders <b>and</b> is not decoration the mask contributes.
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
    /// <param name="maskDecoration">
    /// The characters the mask contributes rather than takes from the value, from
    /// <see cref="DecorationOf"/>, or <c>null</c> when they could not be determined — in which case
    /// only the total-collapse test applies.
    /// </param>
    internal static bool Applies(string? configuredValue, string? maskedResult, string? maskDecoration)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return false;
        }

        // #266's rule, unchanged and checked first: something was stored and nothing came out.
        if (RendersEmpty(maskedResult))
        {
            return true;
        }

        // DecorationOf could not answer without guessing (a non-pattern or transforming mask). Report
        // nothing beyond the collapse case above rather than invent a verdict — the cost of a wrong
        // "yes" here is a warning on every value of a correctly configured field.
        if (maskDecoration is null)
        {
            return false;
        }

        return !string.Equals(
            DataCharacters(configuredValue, maskDecoration),
            DataCharacters(maskedResult, maskDecoration),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether the field renders nothing at all — #266's original signal.
    /// </summary>
    /// <remarks>
    /// One predicate with one home, because two callers depend on it: <see cref="Applies"/> decides
    /// whether to report, and <see cref="Warn"/> decides which of the two messages to write. Spelling
    /// it twice would let the rule and its explanation drift — a later refinement to the rule would
    /// leave the emitter telling a developer their field "renders empty" while it is in fact
    /// displaying a wrong value, which is the confusion the two messages exist to prevent.
    /// </remarks>
    /// <param name="maskedResult">What the field displays.</param>
    internal static bool RendersEmpty([NotNullWhen(false)] string? maskedResult) =>
        string.IsNullOrWhiteSpace(maskedResult);

    /// <summary>
    /// <paramref name="value"/> reduced to the characters that carry data rather than format it.
    /// </summary>
    /// <remarks>
    /// Applied to the stored value and the rendered text alike, which is the whole point: it puts
    /// both in the same terms so the comparison sees data rather than punctuation.
    /// <para>
    /// A character survives only if it could match one of the mask's placeholders — approximated by
    /// <see cref="char.IsLetterOrDigit(char)"/>, the union of the default <c>0</c>/<c>a</c>/<c>*</c>
    /// alphabet — <b>and</b> is not something the mask itself contributes. A caller who narrows
    /// <c>MaskChars</c> to a punctuation class through the #265 factory therefore has a character
    /// treated as noise that the mask considers significant; that direction under-reports, which is
    /// the side to err on.
    /// </para>
    /// <para>
    /// Ordinal throughout — these are pattern characters, not text being collated.
    /// </para>
    /// </remarks>
    /// <param name="value">The stored value or the rendered text.</param>
    /// <param name="decoration">What the mask contributes, from <see cref="DecorationOf"/>.</param>
    private static string DataCharacters(string value, string decoration) =>
        new(value.Where(c => char.IsLetterOrDigit(c) && !decoration.Contains(c)).ToArray());

    /// <summary>
    /// The characters <paramref name="mask"/> contributes rather than takes from the value, or
    /// <c>null</c> when they cannot be determined (#283).
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
    /// Two sources, both read off the instance rather than hardcoded. The pattern's <b>literals</b>
    /// are its characters that are not one of the mask's own placeholders, and <c>MaskChars</c> is
    /// where those placeholders live — so a caller supplying custom <c>MaskChars</c> through the #265
    /// factory is handled, which a hardcoded <c>0</c>/<c>a</c>/<c>*</c> list would get wrong. The
    /// <b>placeholder</b> is what <c>PatternMask</c> pads unfilled positions with: a mask configured
    /// with one renders <c>(555) 123-45__</c> for a short value, and counting those pad characters as
    /// data would report a discard for characters the mask <i>added</i> — every value shorter than
    /// the pattern, on every render.
    /// </para>
    /// <para>
    /// Returns <c>null</c> — meaning "no opinion", which <see cref="Applies"/> reads as a fall back to
    /// the #266 total-collapse rule — in the two cases where an answer would be a guess:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// a mask that is not a <see cref="PatternMask"/>. The #265 factory can supply any
    /// <see cref="IMask"/>, and a <c>RegexMask</c>'s <c>Mask</c> is a regular expression whose
    /// non-placeholder characters are metacharacters, not decoration. Note the hierarchy on 9.8.0:
    /// <c>RegexMask</c> and <c>BlockMask</c> land here, while <c>DateMask</c> and <c>MultiMask</c>
    /// derive from <see cref="PatternMask"/> and take the path below — correctly, since both spell a
    /// real pattern. <c>MultiMask</c> rewrites its own <c>Mask</c> as it matches, which is why the
    /// caller reads the pattern and the decoration <i>after</i> feeding the mask its value.
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
    internal static string? DecorationOf(IMask mask)
    {
        if (mask is not PatternMask pattern
            || pattern.Mask is null
            || pattern.MaskChars is null
            || pattern.Transformation is not null)
        {
            return null;
        }

        var placeholders = pattern.MaskChars.Select(maskChar => maskChar.Char).ToHashSet();
        var decoration = pattern.Mask.Where(c => !placeholders.Contains(c));

        if (pattern.Placeholder is { } pad)
        {
            decoration = decoration.Append(pad);
        }

        return new string(decoration.Distinct().ToArray());
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
        var field = string.IsNullOrWhiteSpace(label) ? fieldName : label;

        // The shared half of both messages: WHY the divergence persists, and what to do about it.
        const string Mechanism =
            "MudBlazor builds the display text by running the value through the mask and does " +
            "not write the result back, so a user who submits without touching this field " +
            "leaves the stored value unchanged. Correct the data, widen the mask, or drop the " +
            "mask if the stored format is intentional.";

        // Resolving the logger, emitting, and swallowing all belong to DiagnosticLog (#284), so
        // what is left here is this diagnostic's own two messages. Nothing above needs the guard:
        // RendersEmpty and IsNullOrWhiteSpace are total, so the never-throws promise is unchanged.
        //
        // The SAME predicate Applies used to reach its verdict, not a second copy of it: the
        // wording must follow from the decision rather than re-derive it.
        if (RendersEmpty(maskedResult))
        {
            DiagnosticLog.Warn(
                services,
                Category,
                "Field '{Field}' holds a value that its mask '{Mask}' rejects, so the field " +
                "renders empty while the model keeps the original. " + Mechanism,
                field,
                pattern);

            return;
        }

        // The #283 case. It names what is on screen because that is the only way the developer
        // can recognise it: nothing looks wrong, so "your field shows (155) 512-3456" is the
        // fact that turns an abstract warning into something checkable against the record.
        DiagnosticLog.Warn(
            services,
            Category,
            "Field '{Field}' holds a value that its mask '{Mask}' only partly accepts, so the " +
            "field displays '{Displayed}' — which is not what the model holds. " + Mechanism,
            field,
            pattern,
            maskedResult);
    }
}
