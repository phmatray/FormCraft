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

    [Parameter]
    public bool ShowLineNumbers { get; set; } = true;

    private ElementReference _element;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (string.IsNullOrEmpty(Code))
        {
            return;
        }

        try
        {
            // Idempotent: only a <code> the highlighter has not processed yet is touched, and @key
            // hands it a fresh one whenever Code changes.
            await JsRuntime.InvokeVoidAsync("formcraftCode.highlightUnder", _element);
        }
        catch (JSException)
        {
            // Scripts unavailable: the code still shows, just uncoloured.
        }
        catch (InvalidOperationException)
        {
            // Interop not available yet (prerender) or already torn down.
        }
    }

    private string GetLanguageLabel() => Language.ToUpperInvariant() switch
    {
        "CSHARP" => "C#",
        "RAZOR" => "Razor",
        "HTML" => "HTML",
        "JSON" => "JSON",
        "CSS" => "CSS",
        "BASH" => "Bash",
        "XML" => "XML",
        _ => Language.ToUpperInvariant()
    };
}
