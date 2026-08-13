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

            // The interop above is an await of its own, and it completes before any delay token
            // exists — so the component can already be gone by the time we get here.
            if (IsDisposed)
            {
                return;
            }

            _copied = true;
            StateHasChanged();

            if (!await DelayAsync(2000))
            {
                return;
            }

            _copied = false;
            StateHasChanged();
        }
        // Kept deliberately broad. This is the same fallback #285 restored in Home.CopyInstall, and
        // the failure modes are the ones that do NOT derive from JSException: JSDisconnectedException,
        // InvalidOperationException ("interop calls cannot be issued at this time") and
        // TaskCanceledException on the interop timeout. Copying to the clipboard is a convenience —
        // no failure of it should reach the visitor, who can still select the text.
        catch (Exception)
        {
            // Clipboard API unavailable, or the call could not be issued. Leave _copied as it is:
            // if the write never happened there is no "copied" state to clear.
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
        {
            return "";
        }

        var lineCount = Code.Split('\n').Length;
        return string.Concat(Enumerable.Repeat("<span></span>", lineCount));
    }
}
