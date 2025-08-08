namespace FormCraft;

/// <summary>
/// Specifies that a boolean property should be rendered as a checkbox field
/// when generating a form from model attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CheckboxFieldAttribute : Attribute
{
    /// <summary>
    /// Gets the label to display for the field.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the text to display next to the checkbox.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Gets or sets whether the checkbox should be initially checked by default.
    /// </summary>
    public bool DefaultChecked { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckboxFieldAttribute"/> class.
    /// </summary>
    /// <param name="label">The label to display for the field.</param>
    /// <param name="text">Optional text to display next to the checkbox.</param>
    public CheckboxFieldAttribute(string label, string? text = null)
    {
        Label = label;
        Text = text ?? label;
    }
}