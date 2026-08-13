using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.DemoFluentApp;

/// <summary>The model behind the Fluent showcase form.</summary>
public class ShowcaseModel
{
    /// <summary>A required text field.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>A numeric field.</summary>
    public int Age { get; set; }

    /// <summary>An autocompleted field.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>A date field.</summary>
    public DateTime? StartsOn { get; set; }

    /// <summary>A multi-select field.</summary>
    public IEnumerable<string> Categories { get; set; } = [];

    /// <summary>An options-driven select.</summary>
    public string Plan { get; set; } = "free";

    /// <summary>Rendered by the slider custom renderer.</summary>
    public double Volume { get; set; } = 5;

    /// <summary>Rendered by the rating custom renderer.</summary>
    public int Score { get; set; } = 3;

    /// <summary>Rendered by the colour-picker custom renderer.</summary>
    public string Colour { get; set; } = "#0078d4";

    /// <summary>A boolean field.</summary>
    public bool Subscribed { get; set; }

    /// <summary>A required file upload.</summary>
    public IBrowserFile? Resume { get; set; }

    /// <summary>A collection rendered with an item form.</summary>
    public List<ShowcaseLine> Lines { get; set; } = [];
}

/// <summary>One row of the showcase's collection field.</summary>
public class ShowcaseLine
{
    /// <summary>A required item field.</summary>
    public string Product { get; set; } = string.Empty;

    /// <summary>A numeric item field.</summary>
    public int Quantity { get; set; } = 1;
}
