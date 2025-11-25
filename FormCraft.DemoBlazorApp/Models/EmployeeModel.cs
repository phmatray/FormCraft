namespace FormCraft.DemoBlazorApp.Models;

public class EmployeeModel
{
    // Personal Information
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }

    // Contact Information
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // Professional Information
    public string? Department { get; set; }
    public string? Position { get; set; }
    public DateTime? StartDate { get; set; }
    public bool IsRemote { get; set; }

    // Additional
    public string? Biography { get; set; }
    public string? Notes { get; set; }
}