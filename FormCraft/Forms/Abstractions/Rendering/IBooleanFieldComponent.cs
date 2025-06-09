namespace FormCraft;

/// <summary>
/// Defines the contract for boolean field components across different UI frameworks.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
public interface IBooleanFieldComponent<TModel> : IFieldComponent<TModel>
{
    /// <summary>
    /// Gets or sets the display style for the boolean field.
    /// </summary>
    BooleanDisplayStyle DisplayStyle { get; set; }
    
    /// <summary>
    /// Gets or sets the text to display when the value is true.
    /// </summary>
    string? TrueText { get; set; }
    
    /// <summary>
    /// Gets or sets the text to display when the value is false.
    /// </summary>
    string? FalseText { get; set; }
    
    /// <summary>
    /// Gets or sets whether the checkbox supports an indeterminate state.
    /// </summary>
    bool AllowIndeterminate { get; set; }
}

/// <summary>
/// Defines how a boolean field should be displayed.
/// </summary>
public enum BooleanDisplayStyle
{
    /// <summary>
    /// Display as a checkbox.
    /// </summary>
    Checkbox,
    
    /// <summary>
    /// Display as a switch/toggle.
    /// </summary>
    Switch,
    
    /// <summary>
    /// Display as radio buttons.
    /// </summary>
    RadioButtons
}