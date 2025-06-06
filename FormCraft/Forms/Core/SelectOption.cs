namespace FormCraft;

/// <summary>
/// Represents an option in a select list or dropdown field.
/// </summary>
/// <typeparam name="T">The type of the option value.</typeparam>
public class SelectOption<T>
{
    /// <summary>
    /// Gets or sets the value that will be bound to the model when this option is selected.
    /// </summary>
    public T Value { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the display text shown to the user for this option.
    /// </summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Initializes a new instance of the SelectOption class.
    /// </summary>
    public SelectOption() { }
    
    /// <summary>
    /// Initializes a new instance of the SelectOption class with a value and label.
    /// </summary>
    /// <param name="value">The value for this option.</param>
    /// <param name="label">The display label for this option.</param>
    public SelectOption(T value, string label)
    {
        Value = value;
        Label = label;
    }
}