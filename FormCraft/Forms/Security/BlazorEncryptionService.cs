using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace FormCraft;

/// <summary>
/// Browser (WebAssembly) compatible implementation of encryption service.
/// </summary>
/// <remarks>
/// <para>
/// WARNING: This implementation uses a simple XOR cipher for browser compatibility and provides
/// obfuscation only — it is NOT cryptographically secure. For sensitive data, perform encryption
/// server-side with <see cref="DefaultEncryptionService"/> (registered by default on non-browser
/// platforms).
/// </para>
/// <para>
/// The key is read from the <c>FormCraft:Encryption:Key</c> configuration value and must be
/// either a Base64-encoded 32-byte value or a string whose UTF-8 representation is exactly 32 bytes;
/// invalid keys are rejected instead of being padded or truncated. When no key is configured, a
/// random key is generated once per process — values obfuscated with that ephemeral key cannot be
/// recovered after an application restart.
/// </para>
/// </remarks>
public class BlazorEncryptionService : IEncryptionService
{
    private const string KeyConfigurationPath = "FormCraft:Encryption:Key";
    private const int KeySizeInBytes = 32;

    private static readonly Lazy<byte[]> EphemeralKey = new(() => RandomNumberGenerator.GetBytes(KeySizeInBytes));

    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorEncryptionService"/> class.
    /// </summary>
    /// <param name="configuration">
    /// The application configuration. The key is read from <c>FormCraft:Encryption:Key</c>.
    /// When <c>null</c> or when no key is configured, an ephemeral per-process key is used
    /// (encrypted values will not survive an application restart).
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a key is configured but is not a Base64-encoded 32-byte value or a string whose
    /// UTF-8 representation is exactly 32 bytes.
    /// </exception>
    public BlazorEncryptionService(IConfiguration? configuration = null)
    {
        var keyString = configuration?[KeyConfigurationPath];
        _key = string.IsNullOrEmpty(keyString)
            ? EphemeralKey.Value
            : ParseKey(keyString);
    }

    /// <inheritdoc />
    public string? Encrypt(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var data = Encoding.UTF8.GetBytes(value);
        var encrypted = new byte[data.Length];

        // Simple XOR obfuscation for browser compatibility (see class remarks).
        for (int i = 0; i < data.Length; i++)
        {
            encrypted[i] = (byte)(data[i] ^ _key[i % _key.Length]);
        }

        return Convert.ToBase64String(encrypted);
    }

    /// <inheritdoc />
    /// <exception cref="FormCraftDecryptionException">
    /// Thrown when the value is not a valid Base64 payload produced by <see cref="Encrypt"/>.
    /// </exception>
    public string? Decrypt(string? encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
            return encryptedValue;

        byte[] encrypted;
        try
        {
            encrypted = Convert.FromBase64String(encryptedValue);
        }
        catch (FormatException ex)
        {
            throw new FormCraftDecryptionException(
                "Failed to decrypt the value. The data is not a valid Base64 payload and was likely " +
                "not encrypted by FormCraft.",
                ex);
        }

        var decrypted = new byte[encrypted.Length];

        // XOR decryption (same as encryption)
        for (int i = 0; i < encrypted.Length; i++)
        {
            decrypted[i] = (byte)(encrypted[i] ^ _key[i % _key.Length]);
        }

        return Encoding.UTF8.GetString(decrypted);
    }

    private static byte[] ParseKey(string keyString)
    {
        // Prefer a Base64-encoded 32-byte key.
        var base64Buffer = new byte[keyString.Length];
        if (Convert.TryFromBase64String(keyString, base64Buffer, out var bytesWritten) && bytesWritten == KeySizeInBytes)
            return base64Buffer[..bytesWritten];

        // Fall back to a raw string whose UTF-8 representation is exactly 32 bytes.
        var utf8Key = Encoding.UTF8.GetBytes(keyString);
        if (utf8Key.Length == KeySizeInBytes)
            return utf8Key;

        throw new InvalidOperationException(
            $"The configured '{KeyConfigurationPath}' is invalid. Provide a Base64-encoded {KeySizeInBytes}-byte key " +
            $"or a string whose UTF-8 representation is exactly {KeySizeInBytes} bytes " +
            $"(the configured value is {utf8Key.Length} bytes). FormCraft never pads or truncates encryption keys.");
    }
}
