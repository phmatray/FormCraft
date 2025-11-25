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
}