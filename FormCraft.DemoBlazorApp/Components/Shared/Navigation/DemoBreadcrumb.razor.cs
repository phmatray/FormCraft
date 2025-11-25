using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Shared.Navigation;

public partial class DemoBreadcrumb
{
    [Parameter]
    public string? DemoId { get; set; }

    private List<BreadcrumbItem> _items = new();

    protected override void OnParametersSet()
    {
        BuildBreadcrumbs();
    }

    private void BuildBreadcrumbs()
    {
        _items.Clear();

        // Always start with Home
        _items.Add(new BreadcrumbItem("Home", "home", icon: Icons.Material.Filled.Home));

        if (string.IsNullOrEmpty(DemoId))
            return;

        var demo = DemoRegistry.GetDemo(DemoId);
        if (demo == null)
            return;

        // Add category
        var categoryName = DemoRegistry.GetCategoryDisplayName(demo.Category);
        _items.Add(new BreadcrumbItem(categoryName, null, disabled: true));

        // Add current demo (no link since we're on it)
        _items.Add(new BreadcrumbItem(demo.Title, null, disabled: true));
    }
}
