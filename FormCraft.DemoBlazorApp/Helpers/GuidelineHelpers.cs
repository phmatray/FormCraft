using FormCraft.DemoBlazorApp.Models;

namespace FormCraft.DemoBlazorApp.Helpers;

public static class GuidelineHelpers
{
    public static GuidelineItem CreateGuideline(string feature, string usage, string example, bool isCode = true)
    {
        return new GuidelineItem
        {
            Feature = feature,
            Usage = usage,
            Example = example,
            IsCode = isCode
        };
    }
    
    public static GuidelineItem CreateCodeGuideline(string feature, string usage, string example)
    {
        return CreateGuideline(feature, usage, example, true);
    }
    
    public static GuidelineItem CreateTextGuideline(string feature, string usage, string example)
    {
        return CreateGuideline(feature, usage, example, false);
    }
}