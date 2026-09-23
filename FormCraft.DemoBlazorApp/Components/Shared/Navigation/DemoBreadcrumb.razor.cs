using FormCraft.DemoBlazorApp.Models;
using Microsoft.AspNetCore.Components;

namespace FormCraft.DemoBlazorApp.Components.Shared.Navigation;

public partial class DemoBreadcrumb
{
    [Parameter]
    public string? DemoId { get; set; }

    private DemoMetadata? _currentDemo;

    protected override void OnParametersSet()
    {
        _currentDemo = string.IsNullOrEmpty(DemoId) ? null : DemoRegistry.GetDemo(DemoId);
    }
}
