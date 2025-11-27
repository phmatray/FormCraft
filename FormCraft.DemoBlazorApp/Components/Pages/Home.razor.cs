using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class Home
{
    private async Task CopyToClipboard(string text, bool isPM)
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
        catch (Exception)
        {
            // Fallback if clipboard API is not available
        }
    }
}
