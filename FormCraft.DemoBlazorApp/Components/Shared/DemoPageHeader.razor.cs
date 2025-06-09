using Microsoft.AspNetCore.Components;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class DemoPageHeader
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter, EditorRequired]
    public string Icon { get; set; } = "";

    [Parameter, EditorRequired]
    public string Description { get; set; } = "";
}