namespace FormCraft.UnitTests.Security;

/// <summary>
/// Tests for the EncryptConfiguredFields convenience extension that lets applications
/// persist the WithSecurity()-configured encrypted field values in one call (#147).
/// </summary>
public class EncryptionServiceExtensionsTests
{
    private readonly IEncryptionService _encryptionService;

    public EncryptionServiceExtensionsTests()
    {
        _encryptionService = A.Fake<IEncryptionService>();
        A.CallTo(() => _encryptionService.Encrypt(A<string?>._))
            .ReturnsLazily((string? value) => $"enc({value})");
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Encrypt_Only_Configured_Fields()
    {
        // Arrange
        var model = new TestModel { Name = "John", Ssn = "123-45-6789", CreditCard = "4111" };
        var security = new FormSecurity();
        security.EncryptedFields.Add(nameof(TestModel.Ssn));
        security.EncryptedFields.Add(nameof(TestModel.CreditCard));

        // Act
        var result = _encryptionService.EncryptConfiguredFields(model, security);

        // Assert
        result.Count.ShouldBe(2);
        result[nameof(TestModel.Ssn)].ShouldBe("enc(123-45-6789)");
        result[nameof(TestModel.CreditCard)].ShouldBe("enc(4111)");
        result.ContainsKey(nameof(TestModel.Name)).ShouldBeFalse();
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Not_Modify_The_Model()
    {
        // Arrange
        var model = new TestModel { Ssn = "123-45-6789" };
        var security = new FormSecurity();
        security.EncryptedFields.Add(nameof(TestModel.Ssn));

        // Act
        _encryptionService.EncryptConfiguredFields(model, security);

        // Assert
        model.Ssn.ShouldBe("123-45-6789");
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Return_Empty_When_Security_Is_Null()
    {
        // Arrange
        var model = new TestModel { Ssn = "123-45-6789" };

        // Act
        var result = _encryptionService.EncryptConfiguredFields(model, (IFormSecurity?)null);

        // Assert
        result.ShouldBeEmpty();
        A.CallTo(() => _encryptionService.Encrypt(A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Pass_Through_Null_And_Empty_Values_Unencrypted()
    {
        // Arrange
        var model = new TestModel { Ssn = null, CreditCard = "" };
        var security = new FormSecurity();
        security.EncryptedFields.Add(nameof(TestModel.Ssn));
        security.EncryptedFields.Add(nameof(TestModel.CreditCard));

        // Act
        var result = _encryptionService.EncryptConfiguredFields(model, security);

        // Assert
        result[nameof(TestModel.Ssn)].ShouldBeNull();
        result[nameof(TestModel.CreditCard)].ShouldBe("");
        A.CallTo(() => _encryptionService.Encrypt(A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Skip_Non_String_Properties()
    {
        // Arrange
        var model = new TestModel { Age = 42 };
        var security = new FormSecurity();
        security.EncryptedFields.Add(nameof(TestModel.Age));

        // Act
        var result = _encryptionService.EncryptConfiguredFields(model, security);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Use_Security_From_Form_Configuration_Overload()
    {
        // Arrange
        var model = new TestModel { Ssn = "123-45-6789" };
        var configuration = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Ssn, field => field.WithLabel("SSN"))
            .WithSecurity(security => security.EncryptField(x => x.Ssn))
            .Build();

        // Act
        var result = _encryptionService.EncryptConfiguredFields(model, configuration);

        // Assert
        result[nameof(TestModel.Ssn)].ShouldBe("enc(123-45-6789)");
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Throw_When_Service_Is_Null()
    {
        // Arrange
        IEncryptionService service = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            service.EncryptConfiguredFields(new TestModel(), (IFormSecurity?)null));
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Ssn { get; set; }
        public string? CreditCard { get; set; }
        public int Age { get; set; }
    }
}
