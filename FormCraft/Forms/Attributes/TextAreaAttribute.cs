namespace FormCraft;

/// <summary>
/// Specifies that a string property should be rendered as a multiline text area
/// when generating a form from model attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TextAreaAttribute : Attribute
{
    /// <summary>
    /// Gets the label to display for the field.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the optional placeholder text for the field.
    /// </summary>
    public string? Placeholder { get; }

    /// <summary>
    /// Gets or sets the number of visible text rows.
    /// </summary>
    public int Rows { get; set; } = 4;

    /// <summary>
    /// Gets or sets the maximum character length.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets whether the text area should auto-resize based on content.
    /// </summary>
    public bool AutoResize { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextAreaAttribute"/> class.
    /// </summary>
    /// <param name="label">The label to display for the field.</param>
    /// <param name="placeholder">Optional placeholder text.</param>
    public TextAreaAttribute(string label, string? placeholder = null)
    {
        Label = label;
        Placeholder = placeholder;
    }
}