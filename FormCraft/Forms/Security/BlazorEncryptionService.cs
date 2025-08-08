using System.Text;
using Microsoft.Extensions.Configuration;

namespace FormCraft;

/// <summary>
/// Blazor-compatible implementation of encryption service.
/// Note: This uses a simple XOR cipher for browser compatibility.
/// For production use with sensitive data, consider server-side encryption.
/// </summary>
public class BlazorEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public BlazorEncryptionService(IConfiguration configuration)
    {
        // Get key from configuration or generate a default one
        var keyString = configuration["FormCraft:Encryption:Key"];
        if (string.IsNullOrEmpty(keyString))
        {
            // In production, this should come from configuration
            keyString = "DefaultKey123456DefaultKey123456"; // 32 bytes
        }

        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32)[..32]);
    }

    public string? Encrypt(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var data = Encoding.UTF8.GetBytes(value);
        var encrypted = new byte[data.Length];

        // Simple XOR encryption for browser compatibility
        for (int i = 0; i < data.Length; i++)
        {
            encrypted[i] = (byte)(data[i] ^ _key[i % _key.Length]);
        }

        return Convert.ToBase64String(encrypted);
    }

    public string? Decrypt(string? encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
            return encryptedValue;

        try
        {
            var encrypted = Convert.FromBase64String(encryptedValue);
            var decrypted = new byte[encrypted.Length];

            // XOR decryption (same as encryption)
            for (int i = 0; i < encrypted.Length; i++)
            {
                decrypted[i] = (byte)(encrypted[i] ^ _key[i % _key.Length]);
            }

            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            // If decryption fails, return the original value
            // In production, this should be logged
            return encryptedValue;
        }
    }
}