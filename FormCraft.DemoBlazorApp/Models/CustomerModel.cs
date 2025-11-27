namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Represents a customer for LOV selection.
/// </summary>
public class CustomerModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; } = true;
}
