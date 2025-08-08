namespace FormCraft.UnitTests.Validators;

public class RequiredValidatorTests
{
    private readonly RequiredValidator<TestModel, string> _stringValidator;
    private readonly RequiredValidator<TestModel, int> _intValidator;
    private readonly RequiredValidator<TestModel, int?> _nullableIntValidator;
    private readonly RequiredValidator<TestModel, bool> _boolValidator;
    private readonly RequiredValidator<TestModel, bool?> _nullableBoolValidator;
    private readonly IServiceProvider _services;

    public RequiredValidatorTests()
    {
        _stringValidator = new RequiredValidator<TestModel, string>("Field is required");
        _intValidator = new RequiredValidator<TestModel, int>("Field is required");
        _nullableIntValidator = new RequiredValidator<TestModel, int?>("Field is required");
        _boolValidator = new RequiredValidator<TestModel, bool>("Field is required");
        _nullableBoolValidator = new RequiredValidator<TestModel, bool?>("Field is required");
        _services = A.Fake<IServiceProvider>();
    }

    [Fact]
    public async Task ValidateAsync_String_Should_Fail_When_Null()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _stringValidator.ValidateAsync(model, null!, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Field is required");
    }

    [Fact]
    public async Task ValidateAsync_String_Should_Fail_When_Empty()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _stringValidator.ValidateAsync(model, string.Empty, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Field is required");
    }

    [Fact]
    public async Task ValidateAsync_String_Should_Fail_When_Whitespace()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _stringValidator.ValidateAsync(model, "   ", _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Field is required");
    }

    [Fact]
    public async Task ValidateAsync_String_Should_Pass_When_Valid()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _stringValidator.ValidateAsync(model, "Valid value", _services);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_Int_Should_Pass_When_Zero()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _intValidator.ValidateAsync(model, 0, _services);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_Int_Should_Pass_When_NonZero()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _intValidator.ValidateAsync(model, 42, _services);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_Bool_Should_Fail_When_False()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _boolValidator.ValidateAsync(model, false, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Field is required");
    }

    [Fact]
    public async Task ValidateAsync_Bool_Should_Pass_When_True()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _boolValidator.ValidateAsync(model, true, _services);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_NullableBool_Should_Fail_When_Null()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _nullableBoolValidator.ValidateAsync(model, null, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Field is required");
    }

    [Fact]
    public async Task ValidateAsync_NullableBool_Should_Fail_When_False()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _nullableBoolValidator.ValidateAsync(model, false, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Field is required");
    }

    [Fact]
    public async Task ValidateAsync_NullableBool_Should_Pass_When_True()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _nullableBoolValidator.ValidateAsync(model, true, _services);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_NullableInt_Should_Fail_When_Null()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _nullableIntValidator.ValidateAsync(model, null, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Field is required");
    }

    [Fact]
    public async Task ValidateAsync_NullableInt_Should_Pass_When_HasValue()
    {
        // Arrange
        var model = new TestModel();

        // Act
        var result = await _nullableIntValidator.ValidateAsync(model, 42, _services);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_Should_Use_Default_Message_When_Not_Provided()
    {
        // Arrange
        var validator = new RequiredValidator<TestModel, string>();
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, null!, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("This field is required.");
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_List_Types()
    {
        // Arrange
        var validator = new RequiredValidator<TestModel, List<string>>("List is required");
        var model = new TestModel();

        // Act
        var nullResult = await validator.ValidateAsync(model, null!, _services);
        var emptyResult = await validator.ValidateAsync(model, new List<string>(), _services);
        var validResult = await validator.ValidateAsync(model, new List<string> { "item" }, _services);

        // Assert
        nullResult.IsValid.ShouldBeFalse();
        emptyResult.IsValid.ShouldBeFalse();
        validResult.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Custom_Types()
    {
        // Arrange
        var validator = new RequiredValidator<TestModel, CustomType>("Custom type is required");
        var model = new TestModel();

        // Act
        var nullResult = await validator.ValidateAsync(model, null!, _services);
        var validResult = await validator.ValidateAsync(model, new CustomType(), _services);

        // Assert
        nullResult.IsValid.ShouldBeFalse();
        nullResult.ErrorMessage.ShouldBe("Custom type is required");
        validResult.IsValid.ShouldBeTrue();
    }

    public class TestModel
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public int? NullableAge { get; set; }
    }

    private class CustomType
    {
        public string Value { get; set; } = string.Empty;
    }
}