using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace FormCraft;

/// <summary>
/// Default implementation of encryption service using AES-256-CBC.
/// </summary>
/// <remarks>
/// <para>
/// The encryption key is read from the <c>FormCraft:Encryption:Key</c> configuration value and must be
/// either a Base64-encoded 32-byte value or a string whose UTF-8 representation is exactly 32 bytes.
/// Any other key is rejected with an <see cref="InvalidOperationException"/> — keys are never padded
/// or truncated.
/// </para>
/// <para>
/// When no key is configured, a cryptographically random key is generated once per process.
/// Values encrypted with this ephemeral key CANNOT be decrypted after an application restart
/// (or by another process), so a key MUST be configured for any data that needs to be persisted.
/// </para>
/// <para>
/// A fresh random IV is generated for every encryption operation and prepended to the ciphertext,
/// so encrypting the same plaintext twice produces different ciphertexts.
/// </para>
/// </remarks>
public class DefaultEncryptionService : IEncryptionService
{
    private const string KeyConfigurationPath = "FormCraft:Encryption:Key";
    private const int KeySizeInBytes = 32;
    private const int IvSizeInBytes = 16;

    private static readonly Lazy<byte[]> EphemeralKey = new(() => RandomNumberGenerator.GetBytes(KeySizeInBytes));

    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEncryptionService"/> class.
    /// </summary>
    /// <param name="configuration">
    /// The application configuration. The encryption key is read from <c>FormCraft:Encryption:Key</c>.
    /// When <c>null</c> or when no key is configured, an ephemeral per-process key is used
    /// (encrypted values will not survive an application restart).
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a key is configured but is not a Base64-encoded 32-byte value or a string whose
    /// UTF-8 representation is exactly 32 bytes.
    /// </exception>
    public DefaultEncryptionService(IConfiguration? configuration = null)
    {
        var keyString = configuration?[KeyConfigurationPath];
        _key = string.IsNullOrEmpty(keyString)
            ? EphemeralKey.Value
            : ParseKey(keyString);
    }

    /// <inheritdoc />
    [UnsupportedOSPlatform("browser")]
    public string? Encrypt(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintext = Encoding.UTF8.GetBytes(value);
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        // Prepend the random IV to the ciphertext so Decrypt can recover it.
        var payload = new byte[aes.IV.Length + ciphertext.Length];
        aes.IV.CopyTo(payload, 0);
        ciphertext.CopyTo(payload, aes.IV.Length);

        return Convert.ToBase64String(payload);
    }

    /// <inheritdoc />
    /// <exception cref="FormCraftDecryptionException">
    /// Thrown when the value cannot be decrypted (corrupted data, non-encrypted input,
    /// or a different encryption key).
    /// </exception>
    [UnsupportedOSPlatform("browser")]
    public string? Decrypt(string? encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
        {
            return encryptedValue;
        }

        try
        {
            var payload = Convert.FromBase64String(encryptedValue);
            if (payload.Length <= IvSizeInBytes)
            {
                throw new FormatException(
                    "The encrypted payload is too short to contain an IV and ciphertext.");
            }

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = payload[..IvSizeInBytes];

            using var decryptor = aes.CreateDecryptor();
            var plaintext = decryptor.TransformFinalBlock(payload, IvSizeInBytes, payload.Length - IvSizeInBytes);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new FormCraftDecryptionException(
                "Failed to decrypt the value. The data may be corrupted, may not have been encrypted by FormCraft, " +
                "or may have been encrypted with a different key (e.g., an ephemeral key from a previous process).",
                ex);
        }
    }

    private static byte[] ParseKey(string keyString)
    {
        // Prefer a Base64-encoded 32-byte key.
        var base64Buffer = new byte[keyString.Length];
        if (Convert.TryFromBase64String(keyString, base64Buffer, out var bytesWritten) && bytesWritten == KeySizeInBytes)
        {
            return base64Buffer[..bytesWritten];
        }

        // Fall back to a raw string whose UTF-8 representation is exactly 32 bytes.
        var utf8Key = Encoding.UTF8.GetBytes(keyString);
        if (utf8Key.Length == KeySizeInBytes)
        {
            return utf8Key;
        }

        throw new InvalidOperationException(
            $"The configured '{KeyConfigurationPath}' is invalid. Provide a Base64-encoded {KeySizeInBytes}-byte key " +
            $"or a string whose UTF-8 representation is exactly {KeySizeInBytes} bytes " +
            $"(the configured value is {utf8Key.Length} bytes). FormCraft never pads or truncates encryption keys.");
    }
}
