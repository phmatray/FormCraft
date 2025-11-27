namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Represents metadata for a demo page in the application.
/// </summary>
public record DemoMetadata
{
    /// <summary>
    /// The route ID (without leading slash), e.g., "simplified", "fluent".
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display title for the demo.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Brief description of what the demo showcases.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// MudBlazor icon class for the demo.
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Category grouping: "form-examples" or "documentation".
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Sort order within the category (legacy, use LevelOrder for complexity-based ordering).
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Complexity level: "beginner", "intermediate", "advanced".
    /// Only applicable for form-examples category.
    /// </summary>
    public string Level { get; init; } = "";

    /// <summary>
    /// Order within the complexity level (1-based).
    /// </summary>
    public int LevelOrder { get; init; }

    /// <summary>
    /// Demo IDs that should be completed before this one.
    /// </summary>
    public IReadOnlyList<string> Prerequisites { get; init; } = [];

    /// <summary>
    /// Key concepts taught in this demo.
    /// </summary>
    public IReadOnlyList<string> Concepts { get; init; } = [];

    /// <summary>
    /// Estimated time to complete the demo in minutes.
    /// </summary>
    public int EstimatedMinutes { get; init; } = 5;

    /// <summary>
    /// Whether this is the recommended starting point for its level.
    /// </summary>
    public bool IsLevelEntryPoint { get; init; }
}
