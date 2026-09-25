using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Services;

/// <summary>The UI adapter a visitor has chosen to read setup snippets for.</summary>
public enum DemoAdapter
{
    MudBlazor,
    FluentUI
}

/// <summary>
/// The visitor's adapter choice from the top bar (#490), persisted in <c>localStorage["fc-adapter"]</c>
/// through <c>window.fcAdapter</c> (<c>wwwroot/js/app.js</c>). It only changes which snippets the site
/// shows and its accent colour: the live forms stay MudBlazor, since
/// <c>AdapterRegistration.EnsureSingleAdapter</c> allows one adapter per container.
/// </summary>
public sealed class AdapterPreference(IJSRuntime js)
{
    public DemoAdapter Current { get; private set; }

    public event Action? Changed;

    /// <summary>Syncs <see cref="Current" /> with storage. Missing, unknown or unreadable → MudBlazor.</summary>
    public async Task LoadAsync()
    {
        string? stored = null;
        try
        {
            stored = await js.InvokeAsync<string?>("fcAdapter.get");
        }
        catch (JSException)
        {
        }

        var adapter = stored == "fluentui" ? DemoAdapter.FluentUI : DemoAdapter.MudBlazor;
        if (adapter != Current)
        {
            Current = adapter;
            Changed?.Invoke();
        }
    }

    public async Task SetAsync(DemoAdapter adapter)
    {
        Current = adapter;
        try
        {
            await js.InvokeVoidAsync("fcAdapter.set", adapter == DemoAdapter.FluentUI ? "fluentui" : "mudblazor");
        }
        catch (JSException)
        {
        }

        Changed?.Invoke();
    }
}
