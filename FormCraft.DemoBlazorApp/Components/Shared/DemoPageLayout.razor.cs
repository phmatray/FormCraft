using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class DemoPageLayout
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter, EditorRequired]
    public string Icon { get; set; } = "";

    [Parameter, EditorRequired]
    public string Description { get; set; } = "";

    [Parameter]
    public string FormDemoIcon { get; set; } = Icons.Material.Filled.Assignment;

    [Parameter, EditorRequired]
    public RenderFragment FormDemoContent { get; set; } = null!;

    [Parameter, EditorRequired]
    public RenderFragment CodeExampleContent { get; set; } = null!;

    [Parameter, EditorRequired]
    public RenderFragment GuidelinesContent { get; set; } = null!;

    [Parameter]
    public RenderFragment? AdditionalTabs { get; set; }

    /// <summary>
    /// The demo ID used for navigation features (breadcrumb, prev/next).
    /// Should match the route without the leading slash (e.g., "simplified", "fluent").
    /// </summary>
    [Parameter]
    public string? DemoId { get; set; }

    /// <summary>
    /// Whether to show the breadcrumb navigation. Defaults to true.
    /// </summary>
    [Parameter]
    public bool ShowBreadcrumb { get; set; } = true;

    /// <summary>
    /// Whether to show the previous/next navigation buttons. Defaults to true.
    /// </summary>
    [Parameter]
    public bool ShowPrevNextNav { get; set; } = true;
}
