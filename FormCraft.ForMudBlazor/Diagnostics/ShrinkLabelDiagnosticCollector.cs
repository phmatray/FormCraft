using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Gathers per-field ShrinkLabel conflicts during a form's render pass so the form can emit a
/// single warning naming every affected field (#181), instead of one warning per field.
/// </summary>
/// <remarks>
/// Cascaded by <see cref="FormCraftComponent{TModel}"/> under
/// <see cref="FormCraftCascadingValues.ShrinkLabelDiagnostics"/>. A field rendered outside a
/// FormCraftComponent sees no collector and logs directly instead, so standalone usage still
/// gets the diagnostic. Not thread-safe by design: Blazor renders a component tree on a single
/// synchronisation context.
/// </remarks>
public sealed class ShrinkLabelDiagnosticCollector
{
    private readonly Dictionary<string, string> _conflicts = [];
    private bool _flushed;

    /// <summary>
    /// Records that <paramref name="field"/> asked for a floating label it will not get.
    /// Repeated reports for the same field collapse into one — a collection of 50 rows must not
    /// list its item field 50 times.
    /// </summary>
    /// <param name="field">The field's label, else its name.</param>
    /// <param name="conflict">The property that overrides the setting, e.g. "a Placeholder".</param>
    public void Report(string field, string conflict) => _conflicts[field] = conflict;

    /// <summary>
    /// Emits one warning naming every reported field, then stops reporting for this form.
    /// </summary>
    /// <remarks>
    /// Flushes at most once: the conflict is a configuration fact, so re-reporting it on every
    /// re-render would flood the console as the user types.
    /// </remarks>
    /// <param name="loggerFactory">Logger factory; when null the diagnostic degrades silently.</param>
    public void Flush(ILoggerFactory? loggerFactory)
    {
        if (_flushed || _conflicts.Count == 0)
        {
            return;
        }

        _flushed = true;

        // A diagnostic must never break a render, so a logger that throws is swallowed.
        try
        {
            var logger = loggerFactory?.CreateLogger(ShrinkLabelDiagnostic.Category);
            if (logger is null)
            {
                return;
            }

            var detail = string.Join(", ", _conflicts.Select(c => $"'{c.Key}' (has {c.Value})"));

            logger.LogWarning(
                "ShrinkLabel=false will not take effect on {Count} field(s): {Fields}. MudBlazor " +
                "pins the label whenever a field has a value, a placeholder or a start adornment, " +
                "so the label stays put. Remove the conflicting property to get a floating label, " +
                "or drop ShrinkLabel=false.",
                _conflicts.Count,
                detail);
        }
        catch
        {
            // Ignored: a failing diagnostic must not take the form down with it.
        }
    }
}
