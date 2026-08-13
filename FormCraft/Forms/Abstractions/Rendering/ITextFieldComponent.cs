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
    /// The pattern is an opaque string rather than a UI framework's mask type, so this contract stays
    /// framework-agnostic: each implementation translates it into whatever its own components take,
    /// and <b>defines the pattern language itself</b>. Consult the UI adapter in use for the
    /// characters it accepts — this interface deliberately does not fix them, because pinning one
    /// framework's syntax here would either bind every other adapter to it or make them silently
    /// violate a documented core contract.
    /// </para>
    /// </remarks>
    string? Mask { get; set; }
}
