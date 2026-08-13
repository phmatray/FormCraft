using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// The single implementation of "emit a FormCraft diagnostic": resolve an optional logger, write one
/// warning, and never let either step reach the render (#284).
/// </summary>
/// <remarks>
/// <para>
/// Four diagnostics grew this block independently — <see cref="MaskedLinesDiagnostic"/> (#207),
/// <see cref="PasswordAdornmentDiagnostic"/> (#219), <see cref="MaskedValueDiagnostic"/> (#266), and
/// an inline copy in <c>MudBlazorFieldComponentBase</c> — each written by copying its nearest
/// neighbour, down to the identical comment inside the empty <c>catch</c>. That is not independent
/// convergence, it is a shape that had outgrown being retyped. #219's own doc had already called it:
/// <i>"if a fourth appears, the shape is worth extracting."</i> A fourth appeared.
/// </para>
/// <para>
/// What is left at each call site is what actually differs: the category, the message template, and
/// its arguments. A fifth diagnostic writes those and nothing else.
/// </para>
/// <para>
/// ⛔ <b>The resolution belongs inside the guard, not above it.</b> The tempting tidy-up — resolve
/// the logger first, then guard only the log call — breaks the one promise this type makes. On a
/// torn-down Blazor circuit the scope is disposed and <c>GetService</c> <i>throws</i> rather than
/// returning null, and this runs during render, so the exception would take the form down for the
/// sake of a warning nobody asked for. Pinned by
/// <c>DiagnosticLogTests.Warn_Should_Not_Throw_When_Resolving_The_Logger_Throws</c>.
/// </para>
/// <para>
/// <b>Not used by <see cref="ShrinkLabelDiagnosticCollector"/>.</b> Its emitter looks similar but is
/// not the same shape: it needs the resolved logger as a <i>value</i> — to mark a batch as logged
/// even when there is nothing to log to, so a re-render does not re-walk the same set forever — and
/// folding it in here would mean handing back the logger, which is the abstraction this exists to
/// remove. Left alone deliberately.
/// </para>
/// </remarks>
internal static class DiagnosticLog
{
    /// <summary>
    /// Emits one warning under <paramref name="category"/>, degrading silently when no logging stack
    /// is registered and swallowing anything thrown along the way.
    /// </summary>
    /// <param name="services">
    /// Provider used to resolve an optional <see cref="ILoggerFactory"/>. May be <c>null</c> for a
    /// component rendered outside DI, which is a supported state rather than an error.
    /// </param>
    /// <param name="category">
    /// The diagnostic's logger category, e.g. <see cref="MaskedLinesDiagnostic.Category"/>. This is
    /// what a developer mutes, so each diagnostic keeps its own — never a shared one, or muting one
    /// would silence the rest.
    /// </param>
    /// <param name="template">The message template, with named placeholders.</param>
    /// <param name="args">The template's arguments, in order.</param>
    internal static void Warn(
        IServiceProvider? services,
        string category,
        string template,
        params object?[] args)
    {
        // A diagnostic must never break a render, so everything here — including the service
        // resolution, which is the one call that can realistically fail on a torn-down circuit —
        // is inside the guard.
        try
        {
            var logger = services?
                .GetService<ILoggerFactory>()?
                .CreateLogger(category);

            logger?.LogWarning(template, args);
        }
        catch
        {
            // Ignored: a failing diagnostic must not take the form down with it.
        }
    }
}
