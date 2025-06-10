using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class Home
{
    private bool _copiedPM;
    private bool _copiedCLI;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Apply Prism.js syntax highlighting
            await JS.InvokeVoidAsync("Prism.highlightAll");
        }
    }

    private async Task CopyToClipboard(string text, bool isPM)
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);

            if (isPM)
            {
                _copiedPM = true;
                StateHasChanged();
                await Task.Delay(2000);
                _copiedPM = false;
                StateHasChanged();
            }
            else
            {
                _copiedCLI = true;
                StateHasChanged();
                await Task.Delay(2000);
                _copiedCLI = false;
                StateHasChanged();
            }
        }
        catch (Exception)
        {
            // Fallback if clipboard API is not available
        }
    }
}