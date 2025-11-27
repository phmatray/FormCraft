using MudBlazor;

namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Structured documentation for a demo page.
/// This model ensures consistent documentation across all demos.
/// </summary>
public record DemoDocumentation
{
    /// <summary>
    /// The unique demo ID (must match the route).
    /// </summary>
    public required string DemoId { get; init; }

    /// <summary>
    /// Display title for the demo.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Brief description of what the demo showcases (minimum 50 characters).
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// MudBlazor icon class for the demo.
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Feature highlights displayed in the sidebar (minimum 4 required).
    /// </summary>
    public required IReadOnlyList<FeatureHighlight> FeatureHighlights { get; init; }

    /// <summary>
    /// API guidelines displayed in the guidelines tab (minimum 4 required).
    /// </summary>
    public required IReadOnlyList<ApiGuideline> ApiGuidelines { get; init; }

    /// <summary>
    /// Code examples displayed in the code tab (minimum 1 required).
    /// </summary>
    public required IReadOnlyList<CodeExampleDefinition> CodeExamples { get; init; }

    /// <summary>
    /// Description of when to use this approach (recommended).
    /// </summary>
    public string? WhenToUse { get; init; }

    /// <summary>
    /// Common mistakes or issues to avoid (recommended).
    /// </summary>
    public IReadOnlyList<string>? CommonPitfalls { get; init; }

    /// <summary>
    /// IDs of related demos for cross-referencing (recommended).
    /// </summary>
    public IReadOnlyList<string>? RelatedDemoIds { get; init; }
}

/// <summary>
/// Represents a feature highlight displayed in the demo sidebar.
/// </summary>
public record FeatureHighlight
{
    /// <summary>
    /// MudBlazor icon class (e.g., Icons.Material.Filled.Extension).
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// MudBlazor color for the icon.
    /// </summary>
    public required Color Color { get; init; }

    /// <summary>
    /// Descriptive text for the feature.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Represents an API guideline row in the guidelines table.
/// </summary>
public record ApiGuideline
{
    /// <summary>
    /// Feature name (e.g., "Text Fields", "Validation").
    /// </summary>
    public required string Feature { get; init; }

    /// <summary>
    /// Brief usage description.
    /// </summary>
    public required string Usage { get; init; }

    /// <summary>
    /// Code example or usage example.
    /// </summary>
    public required string Example { get; init; }

    /// <summary>
    /// Whether the example should be rendered as code.
    /// </summary>
    public bool IsCode { get; init; } = true;
}

/// <summary>
/// Represents a code example definition.
/// </summary>
public record CodeExampleDefinition
{
    /// <summary>
    /// Title for the code section.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Programming language for syntax highlighting (e.g., "csharp", "razor").
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// Function that returns the code string.
    /// </summary>
    public required Func<string> CodeProvider { get; init; }
}
