namespace FormCraft.DemoBlazorApp.Models;

public class GuidelineItem
{
    public string Feature { get; set; } = "";
    public string Usage { get; set; } = "";
    public string Example { get; set; } = "";
    public bool IsCode { get; set; } = true;
}

public class ExtendedGuidelineItem : GuidelineItem
{
    public string? AdditionalInfo { get; set; }
    public string? Icon { get; set; }
}