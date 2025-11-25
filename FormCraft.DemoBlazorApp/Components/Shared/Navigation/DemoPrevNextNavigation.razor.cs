using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;

namespace FormCraft.DemoBlazorApp.Components.Shared.Navigation;

public partial class DemoPrevNextNavigation
{
    [Parameter, EditorRequired]
    public string DemoId { get; set; } = "";

    private DemoMetadata? _previous;
    private DemoMetadata? _next;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(DemoId))
        {
            (_previous, _next) = DemoRegistry.GetAdjacentDemos(DemoId);
        }
        else
        {
            _previous = null;
            _next = null;
        }
    }
}
