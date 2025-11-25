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
    /// Sort order within the category.
    /// </summary>
    public int Order { get; init; }
}
