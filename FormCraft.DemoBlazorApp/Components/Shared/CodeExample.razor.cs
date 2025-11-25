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
    private bool _copied;

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

    private async Task CopyToClipboard()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", Code);
            _copied = true;
            StateHasChanged();
            await Task.Delay(2000);
            _copied = false;
            StateHasChanged();
        }
        catch (Exception)
        {
            // Fallback if clipboard API is not available
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

    private string GetPreClasses()
    {
        var classes = $"language-{Language}";
        if (ShowLineNumbers)
        {
            classes += " line-numbers";
        }
        return classes;
    }

    private string GetLineNumbersHtml()
    {
        if (string.IsNullOrEmpty(Code))
            return "";

        var lineCount = Code.Split('\n').Length;
        return string.Concat(Enumerable.Repeat("<span></span>", lineCount));
    }
}
