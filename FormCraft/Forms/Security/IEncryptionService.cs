namespace FormCraft;

/// <summary>
/// Service for encrypting and decrypting field values.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a value.
    /// </summary>
    /// <param name="value">The value to encrypt.</param>
    /// <returns>The encrypted value as a base64 string.</returns>
    string? Encrypt(string? value);
    
    /// <summary>
    /// Decrypts a value.
    /// </summary>
    /// <param name="encryptedValue">The encrypted value as a base64 string.</param>
    /// <returns>The decrypted value.</returns>
    string? Decrypt(string? encryptedValue);
}