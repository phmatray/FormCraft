using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace DynamicFormBlazor.Services;

public interface IMarkdownService
{
    string ToHtml(string markdown);
    Task<string> LoadDocumentAsync(string fileName);
}

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly IWebHostEnvironment _environment;

    public MarkdownService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

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