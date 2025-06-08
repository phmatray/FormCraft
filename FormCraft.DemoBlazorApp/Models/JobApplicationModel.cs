using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Model for demonstrating file upload functionality in job application forms.
/// </summary>
public class JobApplicationModel
{
    /// <summary>
    /// Gets or sets the applicant's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the applicant's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the applicant's phone number.
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// Gets or sets the position being applied for.
    /// </summary>
    public string Position { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the uploaded resume file.
    /// </summary>
    public IBrowserFile? Resume { get; set; }
    
    /// <summary>
    /// Gets or sets the uploaded certificate files.
    /// </summary>
    public IReadOnlyList<IBrowserFile>? Certificates { get; set; }
    
    /// <summary>
    /// Gets or sets the cover letter text.
    /// </summary>
    public string CoverLetter { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether the applicant agrees to terms and conditions.
    /// </summary>
    public bool AgreeToTerms { get; set; }
}