namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Model for demonstrating custom field renderers.
/// </summary>
public class ProductModel
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Color { get; set; } = "#0066CC";
    public int Rating { get; set; } = 3;
    public double Volume { get; set; } = 50.0;
    public bool InStock { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public string Category { get; set; } = "";
    public DateTime ReleaseDate { get; set; } = DateTime.Today;
}