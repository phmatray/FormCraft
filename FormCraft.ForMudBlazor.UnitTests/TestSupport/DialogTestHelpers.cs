using AngleSharp.Dom;

namespace FormCraft.ForMudBlazor.UnitTests.TestSupport;

/// <summary>
/// Shared DOM lookups for tests that render a MudBlazor dialog through a real
/// <see cref="MudDialogProvider"/>/<see cref="IDialogService"/> round trip
/// (<c>LovSelectionDialogTests</c>, <c>MudBlazorLookupDialogTests</c>). Both dialogs render their
/// rows as <c>&lt;tr class="mud-table-row"&gt;</c> inside a <c>&lt;tbody&gt;</c> (a <c>MudDataGrid</c>
/// and a <c>MudTable</c> share that markup) and their actions as plain <c>MudButton</c>s with no
/// <c>data-testid</c> — pulled out here after code review found the two suites' own copies had
/// already drifted (one matched a button's text with <c>Contains</c>, the other with
/// <c>Trim() == </c>), which a future label change (e.g. "Select (N)") would catch in only one of
/// them.
/// </summary>
internal static class DialogTestHelpers
{
    /// <summary>The number of data rows rendered inside the dialog's table body.</summary>
    public static int RowCount(IRenderedComponent<MudDialogProvider> provider) =>
        provider.FindAll("tbody tr.mud-table-row").Count;

    /// <summary>The single row whose text contains <paramref name="displayText"/>.</summary>
    public static IElement Row(IRenderedComponent<MudDialogProvider> provider, string displayText) =>
        provider.FindAll("tbody tr.mud-table-row").First(r => r.TextContent.Contains(displayText));

    /// <summary>
    /// The single button whose text contains <paramref name="text"/> — <c>Contains</c> rather than
    /// an exact match so a button whose label carries extra state (LOV's "Select (2)") still matches
    /// the same lookup as a plain "Select"/"Cancel".
    /// </summary>
    public static IElement Button(IRenderedComponent<MudDialogProvider> provider, string text) =>
        provider.FindAll("button").First(b => b.TextContent.Contains(text));
}
