using Markdig;

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
        return Markdown.ToHtml(markdown, _pipeline);
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