namespace FormCraft.UnitTests.Validators;

public class CustomValidatorTests
{
    private readonly IServiceProvider _services;

    public CustomValidatorTests()
    {
        _services = A.Fake<IServiceProvider>();
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Success_When_Function_Returns_True()
    {
        // Arrange
        Func<string, bool> validationFunction = value => !string.IsNullOrEmpty(value);
        var validator = new CustomValidator<TestModel, string>(validationFunction, "Value cannot be empty");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, "Valid value", _services);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Failure_When_Function_Returns_False()
    {
        // Arrange
        Func<string, bool> validationFunction = value => !string.IsNullOrEmpty(value);
        var validator = new CustomValidator<TestModel, string>(validationFunction, "Value cannot be empty");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, string.Empty, _services);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Value cannot be empty");
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Null_Values()
    {
        // Arrange
        Func<string?, bool> validationFunction = value => value != null;
        var validator = new CustomValidator<TestModel, string?>(validationFunction, "Value cannot be null");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, null, _services);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Value cannot be null");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Complex_Validation_Logic()
    {
        // Arrange
        Func<string, bool> validationFunction = value =>
        {
            if (string.IsNullOrEmpty(value)) return false;
            return value.Length >= 3 && value.Length <= 20 && value.All(char.IsLetterOrDigit);
        };
        
        var validator = new CustomValidator<TestModel, string>(
            validationFunction, 
            "Value must be 3-20 alphanumeric characters");
        var model = new TestModel();

        // Act & Assert
        var validResult = await validator.ValidateAsync(model, "ValidUser123", _services);
        validResult.IsValid.Should().BeTrue();

        var tooShortResult = await validator.ValidateAsync(model, "ab", _services);
        tooShortResult.IsValid.Should().BeFalse();

        var tooLongResult = await validator.ValidateAsync(model, "thisusernameistoolong", _services);
        tooLongResult.IsValid.Should().BeFalse();

        var invalidCharsResult = await validator.ValidateAsync(model, "user@name", _services);
        invalidCharsResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Numeric_Validations()
    {
        // Arrange
        Func<int, bool> validationFunction = value => value >= 18 && value <= 65;
        var validator = new CustomValidator<TestModel, int>(
            validationFunction, 
            "Age must be between 18 and 65");
        var model = new TestModel();

        // Act & Assert
        var validResult = await validator.ValidateAsync(model, 30, _services);
        validResult.IsValid.Should().BeTrue();

        var tooYoungResult = await validator.ValidateAsync(model, 17, _services);
        tooYoungResult.IsValid.Should().BeFalse();

        var tooOldResult = await validator.ValidateAsync(model, 66, _services);
        tooOldResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Boolean_Validations()
    {
        // Arrange
        Func<bool, bool> validationFunction = value => value == true;
        var validator = new CustomValidator<TestModel, bool>(
            validationFunction, 
            "You must accept the terms");
        var model = new TestModel();

        // Act & Assert
        var validResult = await validator.ValidateAsync(model, true, _services);
        validResult.IsValid.Should().BeTrue();

        var invalidResult = await validator.ValidateAsync(model, false, _services);
        invalidResult.IsValid.Should().BeFalse();
        invalidResult.ErrorMessage.Should().Be("You must accept the terms");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_DateTime_Validations()
    {
        // Arrange
        var today = DateTime.Today;
        Func<DateTime, bool> validationFunction = value => value <= today;
        var validator = new CustomValidator<TestModel, DateTime>(
            validationFunction, 
            "Date cannot be in the future");
        var model = new TestModel();

        // Act & Assert
        var validResult = await validator.ValidateAsync(model, today.AddDays(-1), _services);
        validResult.IsValid.Should().BeTrue();

        var todayResult = await validator.ValidateAsync(model, today, _services);
        todayResult.IsValid.Should().BeTrue();

        var futureResult = await validator.ValidateAsync(model, today.AddDays(1), _services);
        futureResult.IsValid.Should().BeFalse();
        futureResult.ErrorMessage.Should().Be("Date cannot be in the future");
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Exception_In_Validation_Function()
    {
        // Arrange
        Func<string, bool> validationFunction = value => throw new InvalidOperationException("Test exception");
        var validator = new CustomValidator<TestModel, string>(
            validationFunction, 
            "Validation error");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, "test", _services);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Validation error");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Email_Validation()
    {
        // Arrange
        Func<string, bool> emailValidation = value =>
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            
            try
            {
                var addr = new System.Net.Mail.MailAddress(value);
                return addr.Address == value;
            }
            catch
            {
                return false;
            }
        };
        
        var validator = new CustomValidator<TestModel, string>(
            emailValidation, 
            "Please enter a valid email address");
        var model = new TestModel();

        // Act & Assert
        var validResult = await validator.ValidateAsync(model, "user@example.com", _services);
        validResult.IsValid.Should().BeTrue();

        var invalidResult = await validator.ValidateAsync(model, "invalid-email", _services);
        invalidResult.IsValid.Should().BeFalse();
        invalidResult.ErrorMessage.Should().Be("Please enter a valid email address");
    }

    public class TestModel
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public bool AcceptTerms { get; set; }
        public DateTime BirthDate { get; set; }
        public string? Email { get; set; }
    }
}