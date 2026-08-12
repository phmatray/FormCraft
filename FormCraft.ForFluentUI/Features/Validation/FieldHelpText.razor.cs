using Microsoft.AspNetCore.Components;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a field's help text beneath it, in an element the adapter controls.
/// </summary>
public partial class FieldHelpText : ComponentBase
{
    /// <summary>The help text to display. Nothing renders when it is null or blank.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// The element id, so the input can point at it with <c>aria-describedby</c>.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Builds the help-text element id for a field. Shared with the field components so both sides
    /// of the <c>aria-describedby</c> link derive it the same way rather than by convention.
    /// </summary>
    /// <param name="fieldName">The field's name.</param>
    public static string IdFor(string fieldName) => $"formcraft-help-{fieldName}";
}
