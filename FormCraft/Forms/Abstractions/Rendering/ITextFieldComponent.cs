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
    /// Gets or sets the input mask pattern, or <c>null</c> for an unmasked field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pattern is written one character per input position: <c>0</c> accepts a digit, <c>a</c> a
    /// letter, and <c>*</c> either. Every other character is a literal that the field inserts as the
    /// user types — so <c>"(000) 000-0000"</c> turns <c>5551234567</c> into <c>(555) 123-4567</c>.
    /// </para>
    /// <para>
    /// The pattern is a string rather than a UI framework's mask type so this contract stays
    /// framework-agnostic; each implementation translates it into whatever its own components take.
    /// </para>
    /// </remarks>
    string? Mask { get; set; }
}