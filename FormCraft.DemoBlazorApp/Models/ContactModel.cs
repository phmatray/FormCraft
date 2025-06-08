using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.DemoBlazorApp.Models;

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
    public decimal? ExpectedSalary { get; set; }
    public decimal? HourlyRate { get; set; }
    public double? Rating { get; set; }
}