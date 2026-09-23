using FormCraft.DemoBlazorApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class ApiGuidelinesTable
{
    private ElementReference _element;

    /// <summary>Kept for source compatibility; the section heading on the page names the list.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter, EditorRequired]
    public List<GuidelineItem> Items { get; set; } = [];

    [Parameter]
    public RenderFragment? AdditionalContent { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("formcraftCode.highlightUnder", _element);
        }
        catch (JSException)
        {
            // Unhighlighted examples still read fine.
        }
        catch (InvalidOperationException)
        {
            // Interop not available yet, or already torn down.
        }
    }
}
