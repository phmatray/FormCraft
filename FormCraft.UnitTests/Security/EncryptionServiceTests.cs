using Microsoft.Extensions.Configuration;

namespace FormCraft.UnitTests.Security;

public class EncryptionServiceTests
{
    private readonly IEncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FormCraft:Encryption:Key"] = "TestKey123456789TestKey123456789",
                ["FormCraft:Encryption:IV"] = "1234567890123456"
            })
            .Build();
            
        _encryptionService = new DefaultEncryptionService(configuration);
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
    public void Should_Handle_Invalid_Encrypted_Data_Gracefully()
    {
        // Arrange
        const string invalidData = "NotAValidBase64String!@#";

        // Act
        var result = _encryptionService.Decrypt(invalidData);

        // Assert
        result.ShouldBe(invalidData); // Should return original value on failure
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