using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class DocumentationPage
{
    [Parameter]
    public string DocumentName { get; set; } = "";
    
    [Parameter]
    public string Title { get; set; } = "Documentation";
    
    private bool _isLoading = true;
    private string _htmlContent = "";
    private readonly List<TocSection> _tableOfContents = [];
    private bool _contentChanged;

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(DocumentName))
        {
            await LoadDocumentAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_contentChanged)
        {
            _contentChanged = false;
            try
            {
                // Trigger Prism.js highlighting after content is rendered
                await JsRuntime.InvokeVoidAsync("Prism.highlightAll");
            }
            catch (Exception)
            {
                // Ignore JavaScript interop errors during prerendering
            }
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
            ExtractTableOfContents(markdown);
            _contentChanged = true; // Flag that content has changed for syntax highlighting
        }
        catch (Exception ex)
        {
            _htmlContent = $"<div class='error'>Error loading document: {ex.Message}</div>";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void ExtractTableOfContents(string markdown)
    {
        _tableOfContents.Clear();
        var lines = markdown.Split('\n');

        foreach (var line in lines)
        {
            if (line.StartsWith("## "))
            {
                var title = line[3..].Trim();
                var id = title.ToLower().Replace(" ", "-").Replace(".", "");
                _tableOfContents.Add(new TocSection { Title = title, Id = id });
            }
        }
    }

    private async Task ScrollToSection(string sectionId)
    {
        await JsRuntime.InvokeVoidAsync("scrollToElement", sectionId);
    }

    private class TocSection
    {
        public string Title { get; set; } = "";
        public string Id { get; set; } = "";
    }
}