using System.ComponentModel.DataAnnotations;

namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Experience level options used by the auto-generated form demo.
/// </summary>
public enum ExperienceLevel
{
    Junior,
    MidLevel,
    Senior,
    PrincipalEngineer
}

/// <summary>
/// A plain POCO without any attributes. AddFieldsAuto() generates a complete
/// form from it: labels are humanized, "Email" gets email validation,
/// "Password" gets a password input, enums become selects, and so on.
/// </summary>
public class AccountSignupModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Age { get; set; }
    public ExperienceLevel ExperienceLevel { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public bool AcceptUpdates { get; set; }
}

/// <summary>
/// A model decorated with standard DataAnnotations. AddFieldsAuto() honors
/// them when present: [Required], [Range], [MaxLength], [EmailAddress],
/// [Display(Name = ...)] and [ExcludeField].
/// </summary>
public class SpeakerProfileModel
{
    [Required(ErrorMessage = "Please tell us your name")]
    [Display(Name = "Full Name")]
    [MaxLength(50)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Display(Name = "Years of Experience")]
    [Range(0, 50)]
    public int YearsOfExperience { get; set; }

    [MaxLength(200)]
    public string Biography { get; set; } = string.Empty;

    [ExcludeField]
    public int InternalRating { get; set; }
}
