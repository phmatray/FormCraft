namespace FormCraft;

/// <summary>
/// Specifies that a property should be rendered as a dropdown/select field
/// when generating a form from model attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SelectFieldAttribute : Attribute
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
    /// Gets or sets the available options for the select field.
    /// Can be an array of strings or key-value pairs.
    /// </summary>
    public string[]? Options { get; set; }

    /// <summary>
    /// Gets or sets whether multiple selections are allowed.
    /// </summary>
    public bool AllowMultiple { get; set; }

    /// <summary>
    /// Gets or sets the name of a static method or property that provides the options.
    /// This allows for dynamic options loading.
    /// </summary>
    public string? OptionsProviderName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectFieldAttribute"/> class.
    /// </summary>
    /// <param name="label">The label to display for the field.</param>
    /// <param name="placeholder">Optional placeholder text.</param>
    public SelectFieldAttribute(string label, string? placeholder = null)
    {
        Label = label;
        Placeholder = placeholder ?? "Select an option";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectFieldAttribute"/> class with options.
    /// </summary>
    /// <param name="label">The label to display for the field.</param>
    /// <param name="options">The available options for selection.</param>
    public SelectFieldAttribute(string label, params string[] options)
    {
        Label = label;
        Placeholder = "Select an option";
        Options = options;
    }
}