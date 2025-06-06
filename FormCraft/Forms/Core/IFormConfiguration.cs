namespace FormCraft.Forms.Core;

/// <summary>
/// Represents the complete configuration for a dynamic form, including all fields, layout settings, and validation options.
/// </summary>
/// <typeparam name="TModel">The model type that the form will bind to.</typeparam>
public interface IFormConfiguration<TModel>
{
    /// <summary>
    /// Gets the collection of field configurations that define the form's structure.
    /// </summary>
    List<IFieldConfiguration<TModel, object>> Fields { get; }
    
    /// <summary>
    /// Gets the dictionary mapping field names to their dependencies for handling field interactions.
    /// </summary>
    Dictionary<string, List<IFieldDependency<TModel>>> FieldDependencies { get; }
    
    /// <summary>
    /// Gets or sets the layout style for the form (Vertical, Horizontal, Inline, or Grid).
    /// </summary>
    FormLayout Layout { get; set; }
    
    /// <summary>
    /// Gets or sets an optional CSS class to apply to the form container.
    /// </summary>
    string? CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets whether to display a validation summary showing all form errors.
    /// </summary>
    bool ShowValidationSummary { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show indicators (like asterisks) next to required fields.
    /// </summary>
    bool ShowRequiredIndicator { get; set; }
    
    /// <summary>
    /// Gets or sets the text/symbol to display for required field indicators.
    /// </summary>
    string RequiredIndicator { get; set; }
}

/// <summary>
/// Defines the available layout options for form rendering.
/// </summary>
public enum FormLayout
{
    /// <summary>
    /// Fields are stacked vertically in a single column.
    /// </summary>
    Vertical,
    
    /// <summary>
    /// Fields are arranged in two columns with labels on the left and inputs on the right.
    /// </summary>
    Horizontal,
    
    /// <summary>
    /// Fields and labels are arranged inline horizontally.
    /// </summary>
    Inline,
    
    /// <summary>
    /// Fields are arranged in a responsive grid layout with multiple columns.
    /// </summary>
    Grid
}

/// <summary>
/// Default implementation of IFormConfiguration that stores form settings and field collections.
/// </summary>
/// <typeparam name="TModel">The model type that the form will bind to.</typeparam>
public class FormConfiguration<TModel> : IFormConfiguration<TModel>
{
    /// <inheritdoc />
    public List<IFieldConfiguration<TModel, object>> Fields { get; } = new();
    
    /// <inheritdoc />
    public Dictionary<string, List<IFieldDependency<TModel>>> FieldDependencies { get; } = new();
    
    /// <inheritdoc />
    public FormLayout Layout { get; set; } = FormLayout.Vertical;
    
    /// <inheritdoc />
    public string? CssClass { get; set; }
    
    /// <inheritdoc />
    public bool ShowValidationSummary { get; set; } = true;
    
    /// <inheritdoc />
    public bool ShowRequiredIndicator { get; set; } = true;
    
    /// <inheritdoc />
    public string RequiredIndicator { get; set; } = "*";
}