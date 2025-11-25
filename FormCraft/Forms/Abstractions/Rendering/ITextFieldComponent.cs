namespace FormCraft;

/// <summary>
/// Defines the contract for text field components across different UI frameworks.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
public interface ITextFieldComponent<TModel> : IFieldComponent<TModel>
{
    /// <summary>
    /// Gets or sets the number of lines for multiline text input.
    /// </summary>
    int Lines { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the text.
    /// </summary>
    int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the input type (text, email, tel, etc.).
    /// </summary>
    string InputType { get; set; }

    /// <summary>
    /// Gets or sets the input mask pattern.
    /// </summary>
    string? Mask { get; set; }
}