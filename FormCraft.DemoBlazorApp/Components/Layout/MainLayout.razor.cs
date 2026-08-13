using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    private readonly MudTheme _theme = FormCraftTheme.Build();

    private bool _drawerOpen = true;
    private bool _isDarkMode;
    private bool _paletteOpen;
    private string _version = "loading...";
    private DotNetObjectReference<MainLayout>? _selfRef;

    private static readonly string[] _levels =
    [
        Services.DemoRegistry.Levels.Beginner,
        Services.DemoRegistry.Levels.Intermediate,
        Services.DemoRegistry.Levels.Advanced
    ];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _version = await VersionService.GetFormCraftVersionAsync();
        }
        catch
        {
            _version = "latest";
        }
    }

    private bool _isApple;

    /// <summary>
    /// The modifier shown on the search trigger's <c>kbd</c> hint.
    /// </summary>
    /// <remarks>
    /// The registered handler accepts <c>metaKey || ctrlKey</c>, so both are genuinely live; only the
    /// label needed to stop claiming ⌘ on platforms that do not have one. Falls back to "Ctrl K" until
    /// the platform probe resolves, which is the right way round — ⌘ is the minority platform and the
    /// wrong hint is only wrong for one frame.
    /// </remarks>
    private string ShortcutHint => _isApple ? "⌘K" : "Ctrl K";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            // index.html already resolved this before first paint; read the same
            // value back so the C# side agrees with what is on screen.
            _isDarkMode = await JS.InvokeAsync<bool>("formcraftTheme.resolve");
            await JS.InvokeVoidAsync("formcraftTheme.apply", _isDarkMode);

            _selfRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("formcraftShortcuts.register", _selfRef);

            _isApple = await JS.InvokeAsync<bool>("formcraftShortcuts.isApple");

            StateHasChanged();
        }
        catch (JSException)
        {
            // Scripts blocked or unavailable: the site still works, it just starts
            // in light mode and the palette opens from the toolbar button only.
        }
    }

    /// <summary>Opens the command palette. Called from the Cmd/Ctrl+K handler in app.js.</summary>
    [JSInvokable]
    public Task OpenPalette()
    {
        _paletteOpen = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private void OpenPaletteFromToolbar() => _paletteOpen = true;

    private async Task ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;

        try
        {
            await JS.InvokeVoidAsync("formcraftTheme.persist", _isDarkMode);
            await JS.InvokeVoidAsync("formcraftTheme.apply", _isDarkMode);
        }
        catch (JSException)
        {
            // The theme still switches for this session; it just will not be remembered.
        }
    }

    private void NavigateToHome() => Navigation.NavigateTo("home");

    private static string GetLevelSubtitle(string level) => level switch
    {
        Services.DemoRegistry.Levels.Beginner => "Start here",
        Services.DemoRegistry.Levels.Intermediate => "Build better forms",
        Services.DemoRegistry.Levels.Advanced => "Go deeper",
        _ => ""
    };

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("formcraftShortcuts.unregister");
        }
        catch (JSException)
        {
            // Nothing to unregister if scripts never loaded.
        }
        catch (InvalidOperationException)
        {
            // Circuit already gone during teardown.
        }

        _selfRef?.Dispose();
    }
}
