using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// Configuration for file upload fields including constraints and validation rules.
/// </summary>
public class FileUploadConfiguration
{
    /// <summary>
    /// Gets or sets the accepted file extensions (e.g., ".jpg", ".pdf").
    /// If null or empty, all file types are accepted.
    /// </summary>
    public string[]? AcceptedFileTypes { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum file size in bytes.
    /// If null, no size limit is enforced.
    /// </summary>
    public long? MaxFileSize { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of files that can be uploaded.
    /// Default is 1 for single file upload.
    /// </summary>
    public int MaxFiles { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets whether multiple files can be selected.
    /// </summary>
    public bool Multiple => MaxFiles > 1;
    
    /// <summary>
    /// Gets or sets whether to show a preview for image files.
    /// </summary>
    public bool ShowPreview { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether drag and drop is enabled.
    /// </summary>
    public bool EnableDragDrop { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the accept attribute value for the file input.
    /// </summary>
    public string? Accept => AcceptedFileTypes?.Length > 0 
        ? string.Join(",", AcceptedFileTypes) 
        : null;
    
    /// <summary>
    /// Validates a file against the configuration constraints.
    /// </summary>
    /// <param name="file">The file to validate.</param>
    /// <returns>A validation result indicating success or failure with an error message.</returns>
    public ValidationResult ValidateFile(IBrowserFile file)
    {
        if (file == null)
            return ValidationResult.Success();
            
        // Check file size
        if (MaxFileSize.HasValue && file.Size > MaxFileSize.Value)
        {
            var maxSizeInMB = MaxFileSize.Value / (1024.0 * 1024.0);
            return ValidationResult.Failure($"File size exceeds the maximum allowed size of {maxSizeInMB.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} MB");
        }
        
        // Check file type
        if (AcceptedFileTypes?.Length > 0)
        {
            var fileExtension = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
            if (!AcceptedFileTypes.Any(type => type.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
            {
                var acceptedTypes = string.Join(", ", AcceptedFileTypes);
                return ValidationResult.Failure($"File type '{fileExtension}' is not allowed. Accepted types: {acceptedTypes}");
            }
        }
        
        return ValidationResult.Success();
    }
    
    /// <summary>
    /// Creates a human-readable description of the file constraints.
    /// </summary>
    /// <returns>A string describing the file upload constraints.</returns>
    public string GetConstraintsDescription()
    {
        var parts = new List<string>();
        
        if (AcceptedFileTypes?.Length > 0)
            parts.Add($"Accepted formats: {string.Join(", ", AcceptedFileTypes)}");
            
        if (MaxFileSize.HasValue)
        {
            var sizeInMB = MaxFileSize.Value / (1024.0 * 1024.0);
            parts.Add($"Max size: {sizeInMB.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} MB");
        }
        
        if (MaxFiles > 1)
            parts.Add($"Max files: {MaxFiles}");
            
        return parts.Count > 0 ? string.Join(" • ", parts) : string.Empty;
    }
}