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
    public void EncryptConfiguredFields_Should_Fail_Closed_On_A_Non_String_Property()
    {
        // Fail closed (#423): silently omitting a listed field would hand the caller no ciphertext
        // for a value it believes is encrypted.
        var model = new TestModel { Age = 42 };
        var security = new FormSecurity();
        security.EncryptedFields.Add(nameof(TestModel.Age));

        Should.Throw<ArgumentException>(() => _encryptionService.EncryptConfiguredFields(model, security));
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Encrypt_A_Nested_Field_Under_Its_Full_Path()
    {
        var model = new TestModel { Address = new TestAddress { City = "Brussels" } };
        var configuration = FormBuilder<TestModel>.Create()
            .WithSecurity(security => security.EncryptField(x => x.Address!.City))
            .Build();

        var result = _encryptionService.EncryptConfiguredFields(model, configuration);

        result["Address.City"].ShouldBe("enc(Brussels)");
        model.Address.City.ShouldBe("Brussels");
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Pass_Null_Through_For_A_Nested_Field_Under_A_Null_Parent()
    {
        var security = new FormSecurity();
        security.EncryptedFields.Add("Address.City");

        var result = _encryptionService.EncryptConfiguredFields(new TestModel(), security);

        result["Address.City"].ShouldBeNull();
        A.CallTo(() => _encryptionService.Encrypt(A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void EncryptConfiguredFields_Should_Resolve_A_Property_Inherited_Through_A_Base_Interface()
    {
        // #429: Contact is declared IContact, and Email lives on the base interface IHasEmail that
        // IContact inherits from, not on IContact itself.
        var model = new InterfaceModel { Contact = new Contact { Email = "a@b.com" } };
        var configuration = FormBuilder<InterfaceModel>.Create()
            .WithSecurity(security => security.EncryptField(x => x.Contact.Email))
            .Build();

        var result = _encryptionService.EncryptConfiguredFields(model, configuration);

        result["Contact.Email"].ShouldBe("enc(a@b.com)");
        model.Contact.Email.ShouldBe("a@b.com");
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
        public TestAddress? Address { get; set; }
    }

    private class TestAddress
    {
        public string City { get; set; } = string.Empty;
    }

    private interface IHasEmail
    {
        string Email { get; set; }
    }

    private interface IContact : IHasEmail
    {
    }

    private class Contact : IContact
    {
        public string Email { get; set; } = "";
    }

    private class InterfaceModel
    {
        public IContact Contact { get; set; } = new Contact();
    }
}
