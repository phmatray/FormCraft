using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class DocumentationPage
{
    /// <summary>The documentation, in reading order. The ids are the Markdown file names.</summary>
    private static readonly (string Group, (string Id, string Title)[] Pages)[] Pages =
    [
        ("Start", [("getting-started", "Getting started"), ("examples", "Examples")]),
        ("Guides", [("customization", "Customization"), ("fluent-validation", "FluentValidation"), ("security", "Security"), ("troubleshooting", "Troubleshooting")]),
        ("Reference", [("api-reference", "API reference")])
    ];

    private static readonly (string Id, string Title)[] Order = Pages.SelectMany(g => g.Pages).ToArray();

    [Parameter]
    public string DocumentName { get; set; } = "";

    [Parameter]
    public string Title { get; set; } = "Documentation";

    private bool IsApi => DocumentName == "api-reference";

    private int Index => Array.FindIndex(Order, p => p.Id == DocumentName);

    private (string Id, string Title)? Previous => Index > 0 ? Order[Index - 1] : null;

    private (string Id, string Title)? Next => Index >= 0 && Index < Order.Length - 1 ? Order[Index + 1] : null;

    private bool _isLoading = true;
    private string _htmlContent = "";
    private bool _contentChanged;

    private ElementReference _article;
    private ElementReference _toc;
    private ElementReference _apiNav;
    private ElementReference _filter;
    private ElementReference _empty;

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(DocumentName))
        {
            await LoadDocumentAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_contentChanged || _isLoading)
        {
            return;
        }

        _contentChanged = false;
        try
        {
            // Code highlighting, copy buttons, heading anchors, the table of contents, and on the
            // API page the regrouped entries and the filter: all read from the rendered Markdown.
            if (IsApi)
            {
                await JsRuntime.InvokeVoidAsync("formcraftCode.enhanceDoc", _article, null, _apiNav, _filter, _empty);
            }
            else
            {
                await JsRuntime.InvokeVoidAsync("formcraftCode.enhanceDoc", _article, _toc, null, null, null);
            }
        }
        catch (JSException)
        {
            // The document still reads without the enhancements.
        }
        catch (InvalidOperationException)
        {
            // Interop not available yet, or already torn down.
        }
    }

    private async Task LoadDocumentAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            var markdown = await MarkdownService.LoadDocumentAsync(DocumentName);
            _htmlContent = MarkdownService.ToHtml(markdown);
            _contentChanged = true;
        }
        catch (Exception ex)
        {
            _htmlContent = $"<p>This page could not be loaded: {System.Net.WebUtility.HtmlEncode(ex.Message)}</p>";
        }
        finally
        {
            _isLoading = false;

            // Surfaced by #315's audit rather than by its Task.Delay sweep: the await above is a real
            // document load, not a simulated delay, but the shape is the one SecurityDemo has — a
            // render inside a `finally`, which runs even when the try returned early. Loading a large
            // markdown page is exactly when a visitor clicks away.
            if (!IsDisposed)
            {
                StateHasChanged();
            }
        }
    }

    private void GoToDoc(ChangeEventArgs e)
    {
        if (e.Value is string id && !string.IsNullOrEmpty(id))
        {
            Navigation.NavigateTo($"docs/{id}");
        }
    }
}
