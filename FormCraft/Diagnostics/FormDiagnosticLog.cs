using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FormCraft.Diagnostics;

/// <summary>
/// The single implementation of "emit a FormCraft diagnostic": resolve an optional logger, write one
/// warning, and never let either step reach the render (#284).
/// </summary>
/// <remarks>
/// <para>
/// Originally <c>FormCraft.ForMudBlazor.DiagnosticLog</c> (#284), after four MudBlazor diagnostics had
/// each grown the same resolve-log-swallow block independently. Moved into core under #398 so
/// <c>FormCraft.ForFluentUI</c> could call the same hardened implementation instead of hand-rolling a
/// fifth copy with no guard against a torn-down circuit — the same "shared collaborator, not a copy"
/// precedent #279 set for <c>AdapterRegistration</c>/<c>NativeRequired</c> and #321 applied to the
/// security pipeline. Purely a move: the signature and behaviour are unchanged.
/// </para>
/// <para>
/// What is left at each call site is what actually differs: the category, the message template, and
/// its arguments.
/// </para>
/// <para>
/// ⛔ <b>The resolution belongs inside the guard, not above it.</b> The tempting tidy-up — resolve
/// the logger first, then guard only the log call — breaks the one promise this type makes. On a
/// torn-down Blazor circuit the scope is disposed and <c>GetService</c> <i>throws</i> rather than
/// returning null, and this runs during render, so the exception would take the form down for the
/// sake of a warning nobody asked for. Pinned by
/// <c>FormDiagnosticLogTests.Warn_Should_Not_Throw_When_Resolving_The_Logger_Throws</c>.
/// </para>
/// </remarks>
public static class FormDiagnosticLog
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
    /// The diagnostic's logger category. This is what a developer mutes, so each diagnostic keeps its
    /// own — never a shared one, or muting one would silence the rest.
    /// </param>
    /// <param name="template">The message template, with named placeholders.</param>
    /// <param name="args">The template's arguments, in order.</param>
    public static void Warn(
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
