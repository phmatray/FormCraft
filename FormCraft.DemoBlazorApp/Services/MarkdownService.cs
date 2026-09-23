using Markdig;

namespace FormCraft.DemoBlazorApp.Services;

/// <summary>
/// Loads the Markdown documents under <c>wwwroot/docs</c> and renders them to HTML.
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Converts Markdown text to HTML. Fenced code keeps Markdig's <c>language-*</c> class, which
    /// <c>wwwroot/js/code.js</c> reads to highlight it.
    /// </summary>
    /// <param name="markdown">The Markdown content to convert.</param>
    /// <returns>The HTML for the document.</returns>
    string ToHtml(string markdown);

    /// <summary>
    /// Loads a Markdown document from the wwwroot/docs directory.
    /// </summary>
    /// <param name="fileName">The filename (without .md extension) to load.</param>
    /// <returns>The raw Markdown content of the document, or an error message if loading fails.</returns>
    Task<string> LoadDocumentAsync(string fileName);
}

/// <summary>
/// Default implementation of the Markdown service using Markdig with advanced extensions.
/// </summary>
public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the MarkdownService class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for loading markdown files.</param>
    public MarkdownService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <inheritdoc />
    public string ToHtml(string markdown) => Markdown.ToHtml(markdown, _pipeline);

    /// <inheritdoc />
    public async Task<string> LoadDocumentAsync(string fileName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"docs/{fileName}.md");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else
            {
                return $"# Document not found\n\nThe document `{fileName}.md` could not be found.";
            }
        }
        catch (Exception ex)
        {
            return $"# Error loading document\n\nThe document `{fileName}.md` could not be loaded.\n\nError: {ex.Message}";
        }
    }
}
