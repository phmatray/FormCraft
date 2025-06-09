using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class FormGuidelines
{
    [Parameter]
    public string Title { get; set; } = "Form Guidelines";
    
    [Parameter]
    public List<GuidelineItem> Guidelines { get; set; } = [];
    
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public class GuidelineItem
    {
        public string Icon { get; set; } = Icons.Material.Filled.CheckCircle;
        public Color Color { get; set; } = Color.Primary;
        public string Text { get; set; } = "";
    }
}