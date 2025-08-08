using System.ComponentModel.DataAnnotations;
using FormCraft;

namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Demo model showcasing all available field attributes for form generation.
/// </summary>
public class UserRegistrationModel
{
    [TextField("First Name", "Enter your first name")]
    [Required(ErrorMessage = "First name is required")]
    [MinLength(2, ErrorMessage = "First name must be at least 2 characters")]
    [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    [TextField("Last Name", "Enter your last name")]
    [Required(ErrorMessage = "Last name is required")]
    [MinLength(2, ErrorMessage = "Last name must be at least 2 characters")]
    [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string LastName { get; set; } = string.Empty;

    [EmailField("Email Address")]
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = string.Empty;

    [NumberField("Age", "Your age in years")]
    [Required(ErrorMessage = "Age is required")]
    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int Age { get; set; }

    [DateField("Date of Birth")]
    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-25);

    [SelectField("Country", "United States", "Canada", "United Kingdom", "Australia", "Germany", "France", "Japan", "Other")]
    [Required(ErrorMessage = "Please select a country")]
    public string Country { get; set; } = string.Empty;

    [SelectField("Preferred Language", "English", "Spanish", "French", "German", "Chinese", "Japanese", "Other")]
    public string PreferredLanguage { get; set; } = "English";

    [NumberField("Years of Experience")]
    [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
    public int YearsOfExperience { get; set; }

    [TextArea("Bio", "Tell us about yourself...")]
    [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
    public string Bio { get; set; } = string.Empty;

    [CheckboxField("Subscribe to Newsletter", "I want to receive promotional emails")]
    public bool SubscribeToNewsletter { get; set; }

    [CheckboxField("Accept Terms", "I accept the terms and conditions")]
    [Required(ErrorMessage = "You must accept the terms and conditions")]
    public bool AcceptTerms { get; set; }

    [DateField("Preferred Contact Date")]
    public DateTime? PreferredContactDate { get; set; }

    [NumberField("Expected Salary", "$0.00")]
    [Range(0, 1000000, ErrorMessage = "Salary must be between 0 and 1,000,000")]
    public decimal ExpectedSalary { get; set; }

    [TextArea("Additional Comments", "Any additional information...")]
    public string Comments { get; set; } = string.Empty;
}