using Markdig;

namespace FormCraft.DemoBlazorApp.Services;

/// <summary>
/// Provides services for processing Markdown content and integrating with syntax highlighting.
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Converts Markdown text to HTML with syntax highlighting support.
    /// </summary>
    /// <param name="markdown">The Markdown content to convert.</param>
    /// <returns>HTML content with Prism.js classes applied for syntax highlighting.</returns>
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
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the MarkdownService class.
    /// </summary>
    /// <param name="environment">The web host environment for accessing file paths.</param>
    public MarkdownService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    /// <inheritdoc />
    public string ToHtml(string markdown)
    {
        var html = Markdown.ToHtml(markdown, _pipeline);
        
        // Post-process to add Prism.js classes
        html = AddPrismClasses(html);
        
        return html;
    }
    
    private string AddPrismClasses(string html)
    {
        // Replace code blocks with Prism.js classes
        html = html.Replace("<code class=\"language-csharp\"", "<code class=\"language-csharp line-numbers\"");
        html = html.Replace("<code class=\"language-cs\"", "<code class=\"language-csharp line-numbers\"");
        html = html.Replace("<code class=\"language-razor\"", "<code class=\"language-razor line-numbers\"");
        html = html.Replace("<code class=\"language-html\"", "<code class=\"language-markup line-numbers\"");
        html = html.Replace("<code class=\"language-xml\"", "<code class=\"language-markup line-numbers\"");
        html = html.Replace("<code class=\"language-json\"", "<code class=\"language-json line-numbers\"");
        html = html.Replace("<code class=\"language-javascript\"", "<code class=\"language-javascript line-numbers\"");
        html = html.Replace("<code class=\"language-js\"", "<code class=\"language-javascript line-numbers\"");
        
        // Add Prism class to pre elements
        html = html.Replace("<pre><code class=\"language-", "<pre class=\"line-numbers\"><code class=\"language-");
        
        return html;
    }

    /// <inheritdoc />
    public async Task<string> LoadDocumentAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_environment.WebRootPath, "docs", $"{fileName}.md");
            
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                return content;
            }
            else
            {
                return $"# Document not found\n\nThe document `{fileName}.md` could not be found at path: {filePath}";
            }
        }
        catch (Exception ex)
        {
            return $"# Error loading document\n\nThe document `{fileName}.md` could not be loaded.\n\nError: {ex.Message}";
        }
    }
}