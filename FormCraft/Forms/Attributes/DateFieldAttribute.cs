namespace FormCraft;

/// <summary>
/// Specifies that a DateTime property should be rendered as a date picker field
/// when generating a form from model attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DateFieldAttribute : Attribute
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
    /// Gets or sets the minimum allowed date.
    /// </summary>
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed date.
    /// </summary>
    public DateTime? MaxDate { get; set; }

    /// <summary>
    /// Gets or sets the date format string for display.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateFieldAttribute"/> class.
    /// </summary>
    /// <param name="label">The label to display for the field.</param>
    /// <param name="placeholder">Optional placeholder text.</param>
    public DateFieldAttribute(string label, string? placeholder = null)
    {
        Label = label;
        Placeholder = placeholder;
    }
}