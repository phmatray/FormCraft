using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Layout;

public partial class FormPageLayout
{
    [Parameter] public string PageTitle { get; set; } = "";
    [Parameter] public string PageDescription { get; set; } = "";
    [Parameter] public string PageIcon { get; set; } = Icons.Material.Filled.Assignment;
    [Parameter] public string FormTitle { get; set; } = "";
    [Parameter] public string FormIcon { get; set; } = Icons.Material.Filled.Person;
    [Parameter] public RenderFragment FormContent { get; set; } = null!;
    [Parameter] public RenderFragment SidebarContent { get; set; } = null!;
    [Parameter] public RenderFragment? SuccessContent { get; set; }
}