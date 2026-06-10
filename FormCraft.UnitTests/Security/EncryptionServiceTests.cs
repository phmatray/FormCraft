using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace FormCraft.UnitTests.Security;

public class EncryptionServiceTests
{
    private readonly IEncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _encryptionService = new DefaultEncryptionService(BuildConfiguration("TestKey123456789TestKey123456789"));
    }

    private static IConfiguration BuildConfiguration(string? key)
    {
        var values = new Dictionary<string, string?>();
        if (key != null)
        {
            values["FormCraft:Encryption:Key"] = key;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void Should_Encrypt_And_Decrypt_String_Successfully()
    {
        // Arrange
        const string originalValue = "Sensitive Data 123!@#";

        // Act
        var encrypted = _encryptionService.Encrypt(originalValue);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        encrypted.ShouldNotBe(originalValue);
        encrypted.ShouldNotBeNullOrEmpty();
        decrypted.ShouldBe(originalValue);
    }

    [Fact]
    public void Should_Return_Null_When_Encrypting_Null()
    {
        // Act
        var result = _encryptionService.Encrypt(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Should_Return_Empty_When_Encrypting_Empty_String()
    {
        // Act
        var result = _encryptionService.Encrypt("");

        // Assert
        result.ShouldBe("");
    }

    [Fact]
    public void Should_Return_Null_When_Decrypting_Null()
    {
        // Act
        var result = _encryptionService.Decrypt(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Should_Throw_When_Decrypting_Invalid_Data()
    {
        // Arrange
        const string invalidData = "NotAValidBase64String!@#";

        // Act & Assert
        var exception = Should.Throw<FormCraftDecryptionException>(() => _encryptionService.Decrypt(invalidData));
        exception.Message.ShouldContain("Failed to decrypt");
    }

    [Fact]
    public void Should_Throw_When_Decrypting_Valid_Base64_That_Is_Not_Encrypted_Payload()
    {
        // Arrange - valid Base64 but far too short to contain an IV + ciphertext
        var notAPayload = Convert.ToBase64String(new byte[] { 1, 2, 3 });

        // Act & Assert
        Should.Throw<FormCraftDecryptionException>(() => _encryptionService.Decrypt(notAPayload));
    }

    [Fact]
    public void Should_Throw_When_Ciphertext_Is_Tampered()
    {
        // Arrange - "Sensitive Data" is 14 bytes, so PKCS7 pads the single block with 0x02 bytes.
        // Corrupting the IV's last byte turns the padding byte into an invalid value, which makes
        // decryption fail deterministically.
        var encrypted = _encryptionService.Encrypt("Sensitive Data");
        var payload = Convert.FromBase64String(encrypted!);
        payload[15] ^= 0xFF;
        var tampered = Convert.ToBase64String(payload);

        // Act & Assert
        Should.Throw<FormCraftDecryptionException>(() => _encryptionService.Decrypt(tampered));
    }

    [Fact]
    public void Should_Never_Return_Plaintext_Or_Ciphertext_When_Decrypting_With_Different_Key()
    {
        // Arrange
        const string originalValue = "Sensitive Data";
        var otherService = new DefaultEncryptionService(BuildConfiguration("AnotherKey9876543210AnotherKey98"));
        var encrypted = _encryptionService.Encrypt(originalValue);

        // Act - a wrong key throws in virtually all cases (CBC padding validation); in the rare
        // case the padding happens to be valid, the output is garbage but never the plaintext
        // and never the raw ciphertext (the old behavior this guards against).
        string? result = null;
        var exception = Record.Exception(() => result = otherService.Decrypt(encrypted));

        // Assert
        if (exception != null)
        {
            exception.ShouldBeOfType<FormCraftDecryptionException>();
        }
        else
        {
            result.ShouldNotBe(originalValue);
            result.ShouldNotBe(encrypted);
        }
    }

    [Fact]
    public void Should_Use_Random_IV_So_Same_Plaintext_Produces_Different_Ciphertexts()
    {
        // Arrange
        const string value = "123-45-6789";

        // Act
        var first = _encryptionService.Encrypt(value);
        var second = _encryptionService.Encrypt(value);

        // Assert - deterministic ciphertext would enable dictionary attacks on encrypted SSNs
        first.ShouldNotBe(second);
        _encryptionService.Decrypt(first).ShouldBe(value);
        _encryptionService.Decrypt(second).ShouldBe(value);
    }

    [Fact]
    public void Should_Throw_When_Configured_Key_Has_Invalid_Length()
    {
        // Arrange - keys must never be silently padded or truncated
        var configuration = BuildConfiguration("TooShortKey");

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => new DefaultEncryptionService(configuration));
        exception.Message.ShouldContain("FormCraft:Encryption:Key");
    }

    [Fact]
    public void Should_Throw_When_Configured_Key_Is_Longer_Than_32_Bytes()
    {
        // Arrange - previously the key was silently truncated to its first 32 chars
        var configuration = BuildConfiguration(new string('k', 40));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => new DefaultEncryptionService(configuration));
    }

    [Fact]
    public void Should_Accept_Base64_Encoded_32_Byte_Key()
    {
        // Arrange
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var service = new DefaultEncryptionService(BuildConfiguration(key));

        // Act
        var encrypted = service.Encrypt("Sensitive Data");
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.ShouldBe("Sensitive Data");
    }

    [Fact]
    public void Should_Use_Ephemeral_Key_When_No_Key_Is_Configured()
    {
        // Arrange - two instances without configuration share the per-process ephemeral key
        var first = new DefaultEncryptionService(BuildConfiguration(null));
        var second = new DefaultEncryptionService();

        // Act
        var encrypted = first.Encrypt("Sensitive Data");
        var decrypted = second.Decrypt(encrypted);

        // Assert
        decrypted.ShouldBe("Sensitive Data");
    }

    [Theory]
    [InlineData("Simple text")]
    [InlineData("Text with special chars: !@#$%^&*()")]
    [InlineData("Multi\nLine\nText")]
    [InlineData("Unicode: 你好世界 🌍")]
    public void Should_Handle_Various_String_Types(string testValue)
    {
        // Act
        var encrypted = _encryptionService.Encrypt(testValue);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        decrypted.ShouldBe(testValue);
    }
}

public class BlazorEncryptionServiceTests
{
    private static IConfiguration BuildConfiguration(string key)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FormCraft:Encryption:Key"] = key
            })
            .Build();

    [Fact]
    public void Should_Encrypt_And_Decrypt_String_Successfully()
    {
        // Arrange
        var service = new BlazorEncryptionService(BuildConfiguration("TestKey123456789TestKey123456789"));

        // Act
        var encrypted = service.Encrypt("Sensitive Data");
        var decrypted = service.Decrypt(encrypted);

        // Assert
        encrypted.ShouldNotBe("Sensitive Data");
        decrypted.ShouldBe("Sensitive Data");
    }

    [Fact]
    public void Should_Throw_When_Decrypting_Invalid_Base64()
    {
        // Arrange
        var service = new BlazorEncryptionService(BuildConfiguration("TestKey123456789TestKey123456789"));

        // Act & Assert
        Should.Throw<FormCraftDecryptionException>(() => service.Decrypt("NotAValidBase64String!@#"));
    }

    [Fact]
    public void Should_Throw_When_Configured_Key_Has_Invalid_Length()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => new BlazorEncryptionService(BuildConfiguration("TooShortKey")));
    }

    [Fact]
    public void Should_Use_Ephemeral_Key_When_No_Key_Is_Configured()
    {
        // Arrange
        var service = new BlazorEncryptionService();

        // Act
        var encrypted = service.Encrypt("Sensitive Data");
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.ShouldBe("Sensitive Data");
    }
}
