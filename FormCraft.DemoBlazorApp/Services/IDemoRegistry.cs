using FormCraft.DemoBlazorApp.Models;

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
}
