using Microsoft.Extensions.DependencyInjection;
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
    private readonly Dictionary<string, (string Label, string Conflict)> _conflicts = [];
    private readonly HashSet<string> _logged = [];

    /// <summary>
    /// Records that a field asked for a floating label it will not get.
    /// </summary>
    /// <param name="fieldName">
    /// The field's name — the identity key. Labels are NOT used as the key: two fields can
    /// legitimately share one (a "Name" in a Billing group and another in Shipping), and keying
    /// on the label would silently merge them and under-report the count.
    /// </param>
    /// <param name="label">Display name for the message; falls back to the field name.</param>
    /// <param name="conflict">The property that overrides the setting, e.g. "a Placeholder".</param>
    public void Report(string fieldName, string? label, string conflict) =>
        _conflicts[fieldName] = (string.IsNullOrWhiteSpace(label) ? fieldName : label, conflict);

    /// <summary>
    /// Emits one warning naming every field reported since the last flush.
    /// </summary>
    /// <remarks>
    /// Each field is reported at most once for the lifetime of the form, but the collector is
    /// NOT one-shot: fields that first render later — revealed by a visibility condition, an
    /// expanded group, or a newly added collection row — are picked up by a subsequent flush.
    /// A one-shot latch would drop them silently, which is worse than the noise it avoids.
    /// </remarks>
    /// <param name="services">
    /// Provider used to resolve an optional <see cref="ILoggerFactory"/>. Resolution happens
    /// inside this method's exception guard, so a logging stack that throws (or a disposed
    /// scope on a torn-down circuit) cannot escape into the render loop.
    /// </param>
    public void Flush(IServiceProvider? services)
    {
        if (_conflicts.Count == _logged.Count)
        {
            return;
        }

        // A diagnostic must never break a render, so anything thrown in here is swallowed —
        // including the service resolution, which is the one call that can realistically fail.
        try
        {
            var fresh = _conflicts.Where(c => !_logged.Contains(c.Key)).ToList();
            if (fresh.Count == 0)
            {
                return;
            }

            var logger = services?
                .GetService<ILoggerFactory>()?
                .CreateLogger(ShrinkLabelDiagnostic.Category);

            // Mark as logged either way: with no logger there is nothing to emit, and retrying
            // on every render would just re-walk the same set forever.
            foreach (var (key, _) in fresh)
            {
                _logged.Add(key);
            }

            if (logger is null)
            {
                return;
            }

            var detail = string.Join(", ", fresh.Select(c => $"'{c.Value.Label}' (has {c.Value.Conflict})"));

            logger.LogWarning(
                "ShrinkLabel=false will not take effect on {Count} field(s): {Fields}. MudBlazor " +
                "pins the label whenever a field has a value, a placeholder or a start adornment, " +
                "so the label stays put. Remove the conflicting property to get a floating label, " +
                "or drop ShrinkLabel=false.",
                fresh.Count,
                detail);
        }
        catch
        {
            // Ignored: a failing diagnostic must not take the form down with it.
        }
    }
}
