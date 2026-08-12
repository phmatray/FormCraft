using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    /// Whether the mask rejected the stored value outright, rather than reformatting it.
    /// </summary>
    /// <remarks>
    /// Total collapse is the signal, not mere difference. A mask that turns <c>5551234567</c> into
    /// <c>(555) 123-4567</c> is doing precisely its job; warning whenever the two strings differ
    /// would fire on every correctly-masked field in every form, and a diagnostic that fires on the
    /// happy path is one the developer mutes. Only "a value went in and nothing came out" means the
    /// value was rejected and is about to disappear from view while surviving in the model.
    /// <para>
    /// Both tests are <see cref="string.IsNullOrWhiteSpace"/>. On the value side, a whitespace-only
    /// string is a blank field wearing a different hat — a padded column, a trimmed-to-blank setting
    /// — and there is nothing to lose; the same reading <see cref="TextMaskMap.Resolve"/> gives a
    /// whitespace-only *pattern*. On the result side, a mask whose surviving output is only its own
    /// literal spacing has kept nothing of what the user stored, which is the reported case.
    /// </para>
    /// </remarks>
    /// <param name="configuredValue">The value held by the model before the mask was applied.</param>
    /// <param name="maskedResult">What that value renders as once run through the mask.</param>
    internal static bool Applies(string? configuredValue, string? maskedResult) =>
        !string.IsNullOrWhiteSpace(configuredValue) && string.IsNullOrWhiteSpace(maskedResult);

    /// <summary>
    /// Emits the warning, degrading silently when no logging stack is registered.
    /// </summary>
    /// <param name="services">Provider used to resolve an optional <see cref="ILoggerFactory"/>.</param>
    /// <param name="fieldName">The field's name, used when it has no label.</param>
    /// <param name="label">Display name for the message.</param>
    /// <param name="pattern">The configured mask, quoted back so the message is concrete.</param>
    internal static void Warn(
        IServiceProvider? services,
        string fieldName,
        string? label,
        string? pattern)
    {
        // A diagnostic must never break a render, so everything here — including the service
        // resolution, which is the one call that can realistically fail on a torn-down circuit —
        // is inside the guard.
        try
        {
            var logger = services?
                .GetService<ILoggerFactory>()?
                .CreateLogger(Category);

            logger?.LogWarning(
                "Field '{Field}' holds a value that its mask '{Mask}' rejects, so the field renders " +
                "empty while the model keeps the original. MudBlazor builds the display text by " +
                "running the value through the mask and does not write the result back, so a user " +
                "who submits without touching this field leaves the stored value unchanged. Correct " +
                "the data, widen the mask, or drop the mask if the stored format is intentional.",
                string.IsNullOrWhiteSpace(label) ? fieldName : label,
                pattern);
        }
        catch
        {
            // Ignored: a failing diagnostic must not take the form down with it.
        }
    }
}
