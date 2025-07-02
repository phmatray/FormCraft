using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace FormCraft;

/// <summary>
/// Default implementation of encryption service using AES.
/// </summary>
public class DefaultEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public DefaultEncryptionService(IConfiguration configuration)
    {
        // Get key from configuration or generate a default one
        var keyString = configuration["FormCraft:Encryption:Key"];
        if (string.IsNullOrEmpty(keyString))
        {
            // In production, this should come from configuration
            keyString = "DefaultKey123456DefaultKey123456"; // 32 bytes for AES-256
        }

        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
        _iv = Encoding.UTF8.GetBytes(configuration["FormCraft:Encryption:IV"] ?? "1234567890123456");
    }

    [UnsupportedOSPlatform("browser")]
    public string? Encrypt(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(value);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    [UnsupportedOSPlatform("browser")]
    public string? Decrypt(string? encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
            return encryptedValue;

        try
        {
            var buffer = Convert.FromBase64String(encryptedValue);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var msDecrypt = new MemoryStream(buffer);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch
        {
            // If decryption fails, return the original value
            // In production, this should be logged
            return encryptedValue;
        }
    }
}