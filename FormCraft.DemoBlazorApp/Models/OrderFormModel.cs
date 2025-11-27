namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Model for the order form demonstrating LOV field usage.
/// </summary>
public class OrderFormModel
{
    // Customer selection via LOV
    public int? CustomerId { get; set; }

    // Auto-populated from customer selection
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerCity { get; set; }

    // Order details
    public string? OrderReference { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}
