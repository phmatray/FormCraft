using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class DemoPageLayout
{
    private static readonly string[] Levels =
    [
        Services.DemoRegistry.Levels.Beginner,
        Services.DemoRegistry.Levels.Intermediate,
        Services.DemoRegistry.Levels.Advanced
    ];

    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter, EditorRequired]
    public string Icon { get; set; } = "";

    [Parameter, EditorRequired]
    public string Description { get; set; } = "";

    /// <summary>Kept for source compatibility with the pages; the stage no longer shows an icon.</summary>
    [Parameter]
    public string FormDemoIcon { get; set; } = Icons.Material.Filled.Assignment;

    [Parameter, EditorRequired]
    public RenderFragment FormDemoContent { get; set; } = null!;

    [Parameter, EditorRequired]
    public RenderFragment CodeExampleContent { get; set; } = null!;

    [Parameter, EditorRequired]
    public RenderFragment GuidelinesContent { get; set; } = null!;

    /// <summary>
    /// For forms too wide for the stage's side column (tabs, steppers): the form takes the full width
    /// above its source instead of sitting beside it.
    /// </summary>
    [Parameter]
    public bool WideForm { get; set; }

    [Parameter]
    public string? DemoId { get; set; }

    [Parameter]
    public bool ShowBreadcrumb { get; set; } = true;

    [Parameter]
    public bool ShowPrevNextNav { get; set; } = true;

    private void GoToDemo(ChangeEventArgs e)
    {
        if (e.Value is string id && !string.IsNullOrEmpty(id))
        {
            Navigation.NavigateTo(id);
        }
    }
}
