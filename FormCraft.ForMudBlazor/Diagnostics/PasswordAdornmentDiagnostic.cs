using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Reports an adornment displaced by the password visibility toggle (#219).
/// </summary>
/// <remarks>
/// A field has exactly one adornment slot. <c>.AsPassword(enableVisibilityToggle: true)</c> claims it
/// for the show/hide eye, so an adornment configured alongside it — and any <c>onClick</c> handler
/// with it — is discarded. #192 made that handler live on both render paths, which left this the one
/// combination where it is still dropped, silently.
/// <para>
/// Nothing here changes what renders: one slot cannot hold both, and the toggle keeping it is what
/// #216 and #192 settled. This only tells the developer, and names the two ways out — drop the
/// adornment, or pass <c>enableVisibilityToggle: false</c>.
/// </para>
/// <para>
/// Third diagnostic in the same shape (<c>ShrinkLabelDiagnostic</c>, <c>MaskedLinesDiagnostic</c>,
/// this): resolve an optional <see cref="ILoggerFactory"/> inside a guard that swallows, and emit at
/// most once per component instance. A fourth appeared (<see cref="MaskedValueDiagnostic"/>, #266),
/// so the shape was extracted as promised — emission now goes through <see cref="DiagnosticLog"/>
/// and the latch through <c>MudBlazorFieldComponentBase.ShouldReport</c> (#284). What is left here
/// is this diagnostic's own category and message.
/// </para>
/// </remarks>
internal static class PasswordAdornmentDiagnostic
{
    /// <summary>Logger category for the password-adornment diagnostic.</summary>
    internal const string Category = "FormCraft.ForMudBlazor.PasswordAdornment";

    /// <summary>
    /// Emits the warning, degrading silently when no logging stack is registered.
    /// </summary>
    /// <param name="services">Provider used to resolve an optional <see cref="ILoggerFactory"/>.</param>
    /// <param name="fieldName">The field's name, used when it has no label.</param>
    /// <param name="label">Display name for the message.</param>
    internal static void Warn(IServiceProvider? services, string fieldName, string? label) =>
        DiagnosticLog.Warn(
            services,
            Category,
            "Field '{Field}' configures an adornment, but .AsPassword() installs a visibility " +
            "toggle in the same slot and a field has only one. The configured adornment — and " +
            "its click handler, if any — is not rendered. Remove the adornment, or pass " +
            "enableVisibilityToggle: false to keep it and drop the toggle.",
            string.IsNullOrWhiteSpace(label) ? fieldName : label);
}
