using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Reports a multi-line setting that was dropped so a field could stay masked (#207).
/// </summary>
/// <remarks>
/// The render fix in <see cref="TextInputTypeMap.EffectiveLines"/> closes the security hole but is
/// otherwise silent: the developer wrote <c>.AsTextArea(...)</c> and gets a one-line field with
/// nothing saying why. This is the other half — it tells them which of their two settings lost, and
/// that the reason is that a <c>&lt;textarea&gt;</c> cannot mask.
/// <para>
/// Both render paths call this, so the rule for *whether* to warn lives in <see cref="Applies"/>
/// rather than being re-derived at each call site — the same single-implementation discipline as
/// <c>ShrinkLabelDiagnostic.Conflict</c>. Unlike the ShrinkLabel diagnostic there is no aggregating
/// collector: this conflict is rare and specific enough that one warning per field is the clearer
/// signal, where ShrinkLabel's could name a dozen fields at once.
/// </para>
/// </remarks>
internal static class MaskedLinesDiagnostic
{
    /// <summary>Logger category for the password-masking diagnostic.</summary>
    internal const string Category = "FormCraft.ForMudBlazor.PasswordMasking";

    /// <summary>
    /// Whether this field configured a combination that had to be reconciled: masked *and*
    /// multi-line.
    /// </summary>
    /// <param name="resolved">The field's resolved input type.</param>
    /// <param name="configuredLines">The <c>Lines</c> the field asked for, before reconciliation.</param>
    internal static bool Applies(InputType resolved, int configuredLines) =>
        resolved == InputType.Password && configuredLines > 1;

    /// <summary>
    /// Emits the warning, degrading silently when no logging stack is registered.
    /// </summary>
    /// <param name="services">Provider used to resolve an optional <see cref="ILoggerFactory"/>.</param>
    /// <param name="fieldName">The field's name, used when it has no label.</param>
    /// <param name="label">Display name for the message.</param>
    /// <param name="configuredLines">The dropped line count, quoted back so the message is concrete.</param>
    internal static void Warn(
        IServiceProvider? services,
        string fieldName,
        string? label,
        int configuredLines)
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
                "Field '{Field}' is a password field and also asks for {Lines} lines. MudBlazor " +
                "renders a <textarea> past one line, and a textarea has no `type` attribute — so " +
                "honouring the line count would display the value in clear text. The field is " +
                "rendered masked on a single line instead. Drop the multi-line setting to silence " +
                "this, or drop .AsPassword() if the value is not a secret.",
                string.IsNullOrWhiteSpace(label) ? fieldName : label,
                configuredLines);
        }
        catch
        {
            // Ignored: a failing diagnostic must not take the form down with it.
        }
    }
}
