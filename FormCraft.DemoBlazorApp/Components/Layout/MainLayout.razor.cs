using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    private readonly MudTheme _theme = FormCraftTheme.Build();

    private bool _paletteOpen;
    private string _version = "loading...";
    private DotNetObjectReference<MainLayout>? _selfRef;

    private enum Section { Home, Demos, Docs, Api }

    protected override async Task OnInitializedAsync()
    {
        Adapter.Changed += OnAdapterChanged;
        Navigation.LocationChanged += OnLocationChanged;

        try
        {
            _version = await VersionService.GetFormCraftVersionAsync();
        }
        catch
        {
            _version = "latest";
        }
    }

    /// <summary>
    /// The top-level section the current route belongs to. The layout re-renders with every
    /// navigation (its <see cref="LayoutComponentBase.Body"/> changes), so reading the URI here is
    /// enough — no LocationChanged subscription to keep and dispose.
    /// </summary>
    private Section Current
    {
        get
        {
            var path = Navigation.ToBaseRelativePath(Navigation.Uri).Split('?', '#')[0].Trim('/');
            return path switch
            {
                "" or "home" => Section.Home,
                "docs/api-reference" => Section.Api,
                _ when path.StartsWith("docs", StringComparison.Ordinal) => Section.Docs,
                _ => Section.Demos
            };
        }
    }

    private string IsPressed(DemoAdapter adapter) => Adapter.Current == adapter ? "true" : "false";

    private void OnAdapterChanged() => InvokeAsync(() =>
    {
        SyncAdapterQuery();
        StateHasChanged();
    });

    private string? ActiveFor(Section section) => Current == section ? "active" : null;

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

        // index.html already set the accent before boot; this only syncs Current to it.
        await Adapter.LoadAsync();
        SyncAdapterQuery();

        try
        {
            _selfRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("formcraftShortcuts.register", _selfRef);
            _isApple = await JS.InvokeAsync<bool>("formcraftShortcuts.isApple");
            StateHasChanged();
        }
        catch (JSException)
        {
            // Scripts blocked or unavailable: the palette still opens from the toolbar button.
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

    private void OpenPaletteFromToolbar() => _paletteOpen = true;

    public async ValueTask DisposeAsync()
    {
        Adapter.Changed -= OnAdapterChanged;
        Navigation.LocationChanged -= OnLocationChanged;

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

    /// <summary>
    /// Keeps <c>?adapter=fluentui</c> in the address bar while Fluent UI is selected, so a copied link
    /// opens on the same adapter (index.html reads it before boot). In-site links carry no query, so
    /// this re-applies it after every navigation; MudBlazor is the default and drops the parameter.
    /// </summary>
    private void SyncAdapterQuery()
    {
        var uri = Navigation.GetUriWithQueryParameter("adapter", Adapter.Current == DemoAdapter.FluentUI ? "fluentui" : null);
        if (uri != Navigation.Uri)
        {
            Navigation.NavigateTo(uri, replace: true);
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => InvokeAsync(SyncAdapterQuery);
}
