using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Shared.Navigation;

public partial class DemoBreadcrumb
{
    [Parameter]
    public string? DemoId { get; set; }

    private List<BreadcrumbItem> _items = new();
    private DemoMetadata? _currentDemo;
    private (string Name, string Icon, Color Color)? _levelInfo;

    protected override void OnParametersSet()
    {
        BuildBreadcrumbs();
    }

    private void BuildBreadcrumbs()
    {
        _items.Clear();
        _currentDemo = null;
        _levelInfo = null;

        // Always start with Home
        _items.Add(new BreadcrumbItem("Home", "home", icon: Icons.Material.Filled.Home));

        if (string.IsNullOrEmpty(DemoId))
            return;

        var demo = DemoRegistry.GetDemo(DemoId);
        if (demo == null)
            return;

        _currentDemo = demo;

        // Add category (Demos or Documentation)
        var categoryName = DemoRegistry.GetCategoryDisplayName(demo.Category);
        _items.Add(new BreadcrumbItem(categoryName, null, disabled: true));

        // For form examples, add the level
        if (demo.Category == "form-examples" && !string.IsNullOrEmpty(demo.Level))
        {
            _levelInfo = DemoRegistry.GetLevelInfo(demo.Level);
            _items.Add(new BreadcrumbItem(_levelInfo.Value.Name, null, disabled: true));
        }

        // Add current demo (no link since we're on it)
        _items.Add(new BreadcrumbItem(demo.Title, null, disabled: true));
    }
}
