using System;

namespace FormCraft;

/// <summary>
/// Specifies that a string property should be rendered as a text field
/// when generating a form from model attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TextFieldAttribute : Attribute
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
    /// Initializes a new instance of the <see cref="TextFieldAttribute"/> class.
    /// </summary>
    /// <param name="label">The label to display for the field.</param>
    /// <param name="placeholder">Optional placeholder text.</param>
    public TextFieldAttribute(string label, string? placeholder = null)
    {
        Label = label;
        Placeholder = placeholder;
    }
}
