using Microsoft.AspNetCore.Components;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class FormSuccessDisplay
{
    [Parameter]
    public string SuccessMessage { get; set; } =
        "Your information has been successfully submitted. We'll be in touch soon!";

    [Parameter]
    public List<DataDisplayItem> DataDisplayItems { get; set; } = [];

    [Parameter]
    public bool ShowResetButton { get; set; } = true;

    [Parameter]
    public string ResetButtonText { get; set; } = "Submit Another Form";

    [Parameter]
    public EventCallback OnReset { get; set; }

    public class DataDisplayItem
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
    }
}