namespace FormCraft.UnitTests.Security;

/// <summary>
/// Pins <see cref="EncryptedFieldHelper"/> on nested fields listed by full dotted path (#423): before,
/// <c>typeof(TModel).GetProperty("Address.City")</c> returned null and the field stayed in plaintext.
/// </summary>
public class EncryptedFieldHelperTests
{
    private readonly IEncryptionService _encryptionService = A.Fake<IEncryptionService>();

    public EncryptedFieldHelperTests()
    {
        A.CallTo(() => _encryptionService.Encrypt(A<string?>._)).ReturnsLazily((string? v) => $"ENC:{v}");
        A.CallTo(() => _encryptionService.Decrypt(A<string?>._)).ReturnsLazily((string? v) => v!["ENC:".Length..]);
    }

    [Fact]
    public void EncryptFields_Should_Encrypt_A_Nested_Field_Listed_By_Full_Path()
    {
        var model = new TestModel { Address = new TestAddress { City = "Brussels" } };

        EncryptedFieldHelper.EncryptFields(model, Security("Address.City"), _encryptionService);

        model.Address.City.ShouldBe("ENC:Brussels");
    }

    [Fact]
    public void DecryptFields_Should_Restore_A_Nested_Field_Listed_By_Full_Path()
    {
        var model = new TestModel { Address = new TestAddress { City = "ENC:Brussels" } };

        EncryptedFieldHelper.DecryptFields(model, Security("Address.City"), _encryptionService);

        model.Address.City.ShouldBe("Brussels");
    }

    [Fact]
    public void EncryptFields_Should_Encrypt_A_Top_Level_Field_As_Before()
    {
        var model = new TestModel { Ssn = "123-45-6789" };

        EncryptedFieldHelper.EncryptFields(model, Security("Ssn"), _encryptionService);

        model.Ssn.ShouldBe("ENC:123-45-6789");
    }

    [Fact]
    public void EncryptFields_Should_Skip_A_Nested_Field_Whose_Parent_Is_Null()
    {
        var model = new TestModel { Address = null! };

        EncryptedFieldHelper.EncryptFields(model, Security("Address.City"), _encryptionService);

        A.CallTo(() => _encryptionService.Encrypt(A<string?>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData("Address.Town")] // no such property
    [InlineData("Age")] // not a string
    [InlineData("ReadOnly")] // no setter: the plaintext could not be replaced
    public void EncryptFields_Should_Fail_Closed_Without_Touching_The_Model(string unresolvable)
    {
        var model = new TestModel { Ssn = "123-45-6789" };

        Should.Throw<ArgumentException>(() =>
            EncryptedFieldHelper.EncryptFields(model, Security("Ssn", unresolvable), _encryptionService));

        model.Ssn.ShouldBe("123-45-6789");
    }

    [Fact]
    public void EncryptFields_Should_Encrypt_A_Shared_Nested_Object_Only_Once()
    {
        var shared = new TestAddress { City = "Brussels" };
        var model = new TestModel { Address = shared, Work = shared };

        EncryptedFieldHelper.EncryptFields(model, Security("Address.City", "Work.City"), _encryptionService);

        shared.City.ShouldBe("ENC:Brussels");
    }

    [Fact]
    public void CreateDecryptedCopy_Should_Not_Decrypt_The_Original_Nested_Object()
    {
        var model = new TestModel { Address = new TestAddress { City = "ENC:Brussels" } };

        var copy = EncryptedFieldHelper.CreateDecryptedCopy(model, Security("Address.City"), _encryptionService);

        copy.Address.City.ShouldBe("Brussels");
        model.Address.City.ShouldBe("ENC:Brussels");
    }

    private static FormSecurity Security(params string[] paths)
    {
        var security = new FormSecurity();
        security.EncryptedFields.UnionWith(paths);
        return security;
    }

    private class TestModel
    {
        public string? Ssn { get; set; }
        public int Age { get; set; }
        public string ReadOnly => "fixed";
        public TestAddress Address { get; set; } = new();
        public TestAddress? Work { get; set; }
    }

    private class TestAddress
    {
        public string City { get; set; } = string.Empty;
    }
}
