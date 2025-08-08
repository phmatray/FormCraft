namespace FormCraft;

/// <summary>
/// Specifies that a string property should be rendered as an email field
/// when generating a form from model attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EmailFieldAttribute : Attribute
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
    /// Gets whether to validate the email format.
    /// </summary>
    public bool ValidateFormat { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailFieldAttribute"/> class.
    /// </summary>
    /// <param name="label">The label to display for the field.</param>
    /// <param name="placeholder">Optional placeholder text.</param>
    public EmailFieldAttribute(string label, string? placeholder = null)
    {
        Label = label;
        Placeholder = placeholder ?? "user@example.com";
    }
}