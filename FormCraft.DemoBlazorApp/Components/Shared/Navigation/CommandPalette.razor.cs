using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Shared.Navigation;

/// <summary>
/// Keyboard-first search across every demo and documentation page.
/// </summary>
/// <remarks>
/// The site has 27 pages behind a nested drawer, which is more than a visitor can scan.
/// This is the fast path: Cmd/Ctrl+K or "/" from anywhere.
/// </remarks>
public partial class CommandPalette
{
    [Inject] private IDemoRegistry DemoRegistry { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Whether the palette is showing.</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>Raised when the palette opens or closes, so the caller can keep its own flag in step.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    private ElementReference _input;
    private string _query = "";
    private int _selectedIndex;
    private bool _wasOpen;

    private IReadOnlyList<DemoMetadata> _all = [];
    private List<DemoMetadata> _results = [];

    protected override void OnInitialized()
    {
        _all = DemoRegistry.GetAllDemos();
        _results = Rank("");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Focus the field the moment the palette appears, not on every re-render.
        if (Open && !_wasOpen)
        {
            _wasOpen = true;
            try
            {
                await _input.FocusAsync();
            }
            catch (JSException)
            {
                // The element is not in the DOM yet on a slow first paint; the visitor
                // can still click into the field.
            }
        }
        else if (!Open)
        {
            _wasOpen = false;
        }
    }

    private void OnQueryChanged(ChangeEventArgs e)
    {
        _query = e.Value?.ToString() ?? "";
        _results = Rank(_query);
        _selectedIndex = 0;
    }

    /// <summary>
    /// Ranks pages against the query. Title matches beat concept matches, which beat
    /// description matches, so typing "upload" puts the File Upload demo first rather
    /// than every page whose prose happens to mention uploading.
    /// </summary>
    private List<DemoMetadata> Rank(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _all.OrderBy(d => d.Category == "documentation" ? 1 : 0)
                       .ThenBy(d => d.LevelOrder)
                       .ThenBy(d => d.Order)
                       .ToList();
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return _all
            .Select(d => (Demo: d, Score: Score(d, terms)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Demo.Title)
            .Select(x => x.Demo)
            // Flattened through the SAME grouping the markup renders, so this list is in DOM order.
            // _selectedIndex indexes it, and the view derives each row's index from it, so the two
            // must agree: score alone can interleave a documentation hit between two demos, which the
            // grouped render then pulls to the bottom — arrowing would jump over a row and wrap early.
            // The empty-query branch above already sorts documentation last for the same reason.
            .GroupBy(d => d.Category)
            .SelectMany(g => g)
            .ToList();
    }

    private static int Score(DemoMetadata demo, string[] terms)
    {
        var total = 0;

        foreach (var term in terms)
        {
            var termScore = 0;

            if (demo.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                termScore += demo.Title.StartsWith(term, StringComparison.OrdinalIgnoreCase) ? 100 : 60;
            }

            if (demo.Id.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                termScore += 40;
            }

            if (demo.Concepts.Any(c => c.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                termScore += 25;
            }

            if (demo.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                termScore += 10;
            }

            // Every term has to land somewhere, so "file upload" does not match a page
            // that only knows about files.
            if (termScore == 0)
            {
                return 0;
            }

            total += termScore;
        }

        return total;
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowDown":
                if (_results.Count > 0)
                {
                    _selectedIndex = (_selectedIndex + 1) % _results.Count;
                }
                break;

            case "ArrowUp":
                if (_results.Count > 0)
                {
                    _selectedIndex = (_selectedIndex - 1 + _results.Count) % _results.Count;
                }
                break;

            case "Enter":
                if (_selectedIndex >= 0 && _selectedIndex < _results.Count)
                {
                    await Go(_results[_selectedIndex]);
                }
                break;

            case "Escape":
                await Close();
                break;
        }
    }

    private async Task Go(DemoMetadata demo)
    {
        await Close();
        Navigation.NavigateTo(demo.Id);
    }

    private async Task Close()
    {
        _query = "";
        _results = Rank("");
        _selectedIndex = 0;

        Open = false;
        await OpenChanged.InvokeAsync(false);
    }

    private static string GroupLabel(string category) =>
        category == "documentation" ? "Documentation" : "Demos";

    /// <summary>
    /// A stable DOM id for a result row, keyed on the demo's route id rather than its position.
    /// </summary>
    /// <remarks>
    /// The id has to follow the demo through a re-filter. An index-based id would be handed to a
    /// different demo as soon as the query changed — and once the list shortens, would name a row that
    /// no longer exists, leaving <c>aria-activedescendant</c> pointing at nothing.
    /// <para>
    /// The '/' replacement matters: the seven documentation ids are route-shaped
    /// (<c>docs/getting-started</c>). A raw slash is legal in an HTML id and works with
    /// <c>getElementById</c>, but it is not a valid CSS identifier — so the obvious next step,
    /// <c>querySelector('#' + id)</c> to scroll the active row into view, would throw on exactly
    /// those seven rows.
    /// </para>
    /// </remarks>
    private static string OptionId(DemoMetadata demo) =>
        $"fc-palette-option-{demo.Id.Replace('/', '-')}";

    /// <summary>
    /// The id of the highlighted row, or <c>null</c> when there is no row to point at.
    /// </summary>
    /// <remarks>
    /// Bound to the search input's <c>aria-activedescendant</c>, which is what makes arrowing through
    /// results announce anything: the <c>is-selected</c> class is a purely visual cue. Returning
    /// <c>null</c> rather than <c>""</c> matters — Blazor omits a null attribute, whereas an empty one
    /// would claim an active descendant with an unresolvable id while the palette shows no results.
    /// </remarks>
    private string? ActiveOptionId =>
        _selectedIndex >= 0 && _selectedIndex < _results.Count
            ? OptionId(_results[_selectedIndex])
            : null;
}
