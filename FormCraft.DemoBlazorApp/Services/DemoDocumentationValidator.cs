using System.Diagnostics;
using FormCraft.DemoBlazorApp.Models;

namespace FormCraft.DemoBlazorApp.Services;

/// <summary>
/// Validates demo documentation to ensure completeness and consistency.
/// </summary>
public interface IDemoDocumentationValidator
{
    /// <summary>
    /// Validates a single demo's documentation.
    /// </summary>
    DocumentationValidationResult Validate(DemoDocumentation documentation);

    /// <summary>
    /// Validates documentation and throws in DEBUG mode if invalid.
    /// </summary>
    void ValidateOrThrow(DemoDocumentation documentation);
}

/// <summary>
/// Result of documentation validation.
/// </summary>
public record DocumentationValidationResult(
    string DemoId,
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Implementation of documentation validator with DEBUG-mode exceptions.
/// </summary>
public class DemoDocumentationValidator : IDemoDocumentationValidator
{
    private const int MinDescriptionLength = 50;
    private const int MinFeatureHighlights = 4;
    private const int MinApiGuidelines = 4;
    private const int MinCodeExamples = 1;

    public DocumentationValidationResult Validate(DemoDocumentation doc)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Required validations (errors)
        if (string.IsNullOrWhiteSpace(doc.DemoId))
            errors.Add("DemoId is required");

        if (string.IsNullOrWhiteSpace(doc.Title))
            errors.Add("Title is required");

        if (string.IsNullOrWhiteSpace(doc.Description))
            errors.Add("Description is required");
        else if (doc.Description.Length < MinDescriptionLength)
            errors.Add($"Description must be at least {MinDescriptionLength} characters (current: {doc.Description.Length})");

        if (string.IsNullOrWhiteSpace(doc.Icon))
            errors.Add("Icon is required");

        if (doc.FeatureHighlights.Count < MinFeatureHighlights)
            errors.Add($"At least {MinFeatureHighlights} feature highlights required (current: {doc.FeatureHighlights.Count})");

        if (doc.ApiGuidelines.Count < MinApiGuidelines)
            errors.Add($"At least {MinApiGuidelines} API guidelines required (current: {doc.ApiGuidelines.Count})");

        if (doc.CodeExamples.Count < MinCodeExamples)
            errors.Add($"At least {MinCodeExamples} code example required (current: {doc.CodeExamples.Count})");

        // Validate feature highlights
        foreach (var (highlight, index) in doc.FeatureHighlights.Select((h, i) => (h, i)))
        {
            if (string.IsNullOrWhiteSpace(highlight.Icon))
                errors.Add($"Feature highlight {index + 1}: Icon is required");
            if (string.IsNullOrWhiteSpace(highlight.Text))
                errors.Add($"Feature highlight {index + 1}: Text is required");
        }

        // Validate API guidelines
        foreach (var (guideline, index) in doc.ApiGuidelines.Select((g, i) => (g, i)))
        {
            if (string.IsNullOrWhiteSpace(guideline.Feature))
                errors.Add($"API guideline {index + 1}: Feature is required");
            if (string.IsNullOrWhiteSpace(guideline.Usage))
                errors.Add($"API guideline {index + 1}: Usage is required");
            if (string.IsNullOrWhiteSpace(guideline.Example))
                errors.Add($"API guideline {index + 1}: Example is required");
        }

        // Validate code examples
        foreach (var (example, index) in doc.CodeExamples.Select((e, i) => (e, i)))
        {
            if (string.IsNullOrWhiteSpace(example.Title))
                errors.Add($"Code example {index + 1}: Title is required");
            if (string.IsNullOrWhiteSpace(example.Language))
                errors.Add($"Code example {index + 1}: Language is required");
            if (example.CodeProvider == null)
                errors.Add($"Code example {index + 1}: CodeProvider is required");
        }

        // Recommended validations (warnings)
        if (string.IsNullOrWhiteSpace(doc.WhenToUse))
            warnings.Add("Consider adding 'WhenToUse' to help users understand when to use this approach");

        if (doc.RelatedDemoIds == null || doc.RelatedDemoIds.Count == 0)
            warnings.Add("Consider adding 'RelatedDemoIds' for cross-referencing");

        if (doc.CommonPitfalls == null || doc.CommonPitfalls.Count == 0)
            warnings.Add("Consider adding 'CommonPitfalls' to help users avoid common mistakes");

        return new DocumentationValidationResult(
            doc.DemoId,
            errors.Count == 0,
            errors,
            warnings);
    }

    public void ValidateOrThrow(DemoDocumentation documentation)
    {
#if DEBUG
        var result = Validate(documentation);

        if (!result.IsValid)
        {
            var errorMessage = $"Demo '{result.DemoId}' has invalid documentation:\n" +
                               string.Join("\n", result.Errors.Select(e => $"  - {e}"));
            throw new InvalidOperationException(errorMessage);
        }

        // Log warnings in debug mode
        if (result.Warnings.Count > 0)
        {
            Debug.WriteLine($"Demo '{result.DemoId}' documentation warnings:");
            foreach (var warning in result.Warnings)
            {
                Debug.WriteLine($"  - {warning}");
            }
        }
#endif
    }
}
