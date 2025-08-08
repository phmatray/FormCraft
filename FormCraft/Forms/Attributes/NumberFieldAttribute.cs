namespace FormCraft;

/// <summary>
/// Specifies that a numeric property should be rendered as a number field
/// when generating a form from model attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NumberFieldAttribute : Attribute
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
    /// Gets the minimum allowed value for the field.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Gets the maximum allowed value for the field.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Gets the step value for the numeric input.
    /// </summary>
    public double? Step { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NumberFieldAttribute"/> class.
    /// </summary>
    /// <param name="label">The label to display for the field.</param>
    /// <param name="placeholder">Optional placeholder text.</param>
    public NumberFieldAttribute(string label, string? placeholder = null)
    {
        Label = label;
        Placeholder = placeholder;
    }
}