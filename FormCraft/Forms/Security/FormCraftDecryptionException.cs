namespace FormCraft;

/// <summary>
/// Thrown when an <see cref="IEncryptionService"/> fails to decrypt a value.
/// This typically indicates corrupted data, a value that was never encrypted by FormCraft,
/// or a value encrypted with a different key (e.g., after a key rotation or application restart
/// when relying on an ephemeral key).
/// </summary>
public class FormCraftDecryptionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormCraftDecryptionException"/> class.
    /// </summary>
    /// <param name="message">A message describing the decryption failure.</param>
    public FormCraftDecryptionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormCraftDecryptionException"/> class.
    /// </summary>
    /// <param name="message">A message describing the decryption failure.</param>
    /// <param name="innerException">The underlying exception that caused the failure.</param>
    public FormCraftDecryptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
