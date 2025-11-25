using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class CodeExample
{
    [Parameter]
    public string Title { get; set; } = "Code Example";

    [Parameter]
    public string Code { get; set; } = "";

    [Parameter]
    public string Language { get; set; } = "csharp";

    [Parameter]
    public string Class { get; set; } = "";

    private ElementReference _element;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!string.IsNullOrEmpty(Code))
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("Prism.highlightAllUnder", _element);
            }
            catch (Exception)
            {
                // Ignore JavaScript interop errors during prerendering
            }
        }
    }
}