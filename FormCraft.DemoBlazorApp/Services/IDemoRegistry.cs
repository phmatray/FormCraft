using FormCraft.DemoBlazorApp.Models;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Services;

/// <summary>
/// Service for accessing demo page metadata.
/// </summary>
public interface IDemoRegistry
{
    /// <summary>
    /// Gets all registered demos.
    /// </summary>
    IReadOnlyList<DemoMetadata> GetAllDemos();

    /// <summary>
    /// Gets demos filtered by category.
    /// </summary>
    IReadOnlyList<DemoMetadata> GetDemosByCategory(string category);

    /// <summary>
    /// Gets a specific demo by its ID.
    /// </summary>
    DemoMetadata? GetDemo(string id);

    /// <summary>
    /// Gets the previous and next demos relative to the current demo (within the same category).
    /// </summary>
    (DemoMetadata? Previous, DemoMetadata? Next) GetAdjacentDemos(string currentId);

    /// <summary>
    /// Gets the display name for a category.
    /// </summary>
    string GetCategoryDisplayName(string category);

    /// <summary>
    /// Gets demos filtered by complexity level.
    /// </summary>
    /// <param name="level">The complexity level: "beginner", "intermediate", or "advanced".</param>
    IReadOnlyList<DemoMetadata> GetDemosByLevel(string level);

    /// <summary>
    /// Gets the previous and next demos following the learning path order.
    /// This crosses level boundaries: Beginner → Intermediate → Advanced.
    /// </summary>
    (DemoMetadata? Previous, DemoMetadata? Next) GetLearningPathAdjacentDemos(string currentId);

    /// <summary>
    /// Gets display information for a complexity level.
    /// </summary>
    /// <param name="level">The complexity level: "beginner", "intermediate", or "advanced".</param>
    /// <returns>A tuple containing the display name, icon, and color for the level.</returns>
    (string Name, string Icon, Color Color) GetLevelInfo(string level);

    /// <summary>
    /// Gets the display name for a complexity level.
    /// </summary>
    string GetLevelDisplayName(string level);
}
