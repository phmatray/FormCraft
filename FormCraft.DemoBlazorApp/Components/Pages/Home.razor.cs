using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class Home
{
    private const string InstallCommand = "dotnet add package FormCraft.ForMudBlazor";

    private bool _copiedInstall;

    private async Task CopyInstall()
    {
        try
        {
            _copiedInstall = await JS.InvokeAsync<bool>("formcraftCopy", InstallCommand);
        }
        catch (JSException)
        {
            _copiedInstall = false;
        }

        if (!_copiedInstall)
        {
            return;
        }

        StateHasChanged();
        await Task.Delay(2000);
        _copiedInstall = false;
        StateHasChanged();
    }
}
