using Microsoft.AspNetCore.Components.Forms;

namespace DynamicFormBlazor.Models;

public class ContactModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int Age { get; set; }
    public DateTime? BirthDate { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? City { get; set; }
    public bool SubscribeToNewsletter { get; set; }
    public IEnumerable<string> Interests { get; set; } = new List<string>();
    public string? Message { get; set; }
    public IBrowserFile? Resume { get; set; }
    public int ExperienceLevel { get; set; } = 1;
}