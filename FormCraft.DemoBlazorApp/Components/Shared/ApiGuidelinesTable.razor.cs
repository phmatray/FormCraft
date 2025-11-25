using FormCraft.DemoBlazorApp.Models;
using Microsoft.AspNetCore.Components;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class ApiGuidelinesTable
{
    private readonly List<string> _columns = ["Feature", "Usage", "Example"];

    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter, EditorRequired]
    public List<GuidelineItem> Items { get; set; } = [];

    [Parameter]
    public RenderFragment? AdditionalContent { get; set; }
}