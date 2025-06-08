namespace FormCraft.UnitTests.Validators;

public class AsyncValidatorTests
{
    private readonly IServiceProvider _services;

    public AsyncValidatorTests()
    {
        _services = A.Fake<IServiceProvider>();
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Success_When_Function_Returns_True()
    {
        // Arrange
        Func<string, Task<bool>> validationFunction = async value =>
        {
            await Task.Delay(10);
            return !string.IsNullOrEmpty(value);
        };

        var validator = new AsyncValidator<TestModel, string>(validationFunction, "Value cannot be empty");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, "Valid value", _services);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Failure_When_Function_Returns_False()
    {
        // Arrange
        Func<string, Task<bool>> validationFunction = async value =>
        {
            await Task.Delay(10);
            return !string.IsNullOrEmpty(value);
        };

        var validator = new AsyncValidator<TestModel, string>(validationFunction, "Value cannot be empty");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, string.Empty, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Value cannot be empty");
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Null_Values()
    {
        // Arrange
        Func<string?, Task<bool>> validationFunction = async value =>
        {
            await Task.Delay(10);
            return value != null;
        };

        var validator = new AsyncValidator<TestModel, string?>(validationFunction, "Value cannot be null");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, null, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Value cannot be null");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Database_Validation_Simulation()
    {
        // Arrange
        var existingUsernames = new HashSet<string> { "john", "jane", "admin" };

        Func<string, Task<bool>> uniqueUsernameValidation = async username =>
        {
            // Simulate database call
            await Task.Delay(50);
            return !existingUsernames.Contains(username.ToLower());
        };

        var validator = new AsyncValidator<TestModel, string>(
            uniqueUsernameValidation,
            "Username is already taken");
        var model = new TestModel();

        // Act & Assert
        var uniqueResult = await validator.ValidateAsync(model, "newuser", _services);
        uniqueResult.IsValid.ShouldBeTrue();

        var existingResult = await validator.ValidateAsync(model, "john", _services);
        existingResult.IsValid.ShouldBeFalse();
        existingResult.ErrorMessage.ShouldBe("Username is already taken");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_API_Validation_Simulation()
    {
        // Arrange
        Func<string, Task<bool>> emailValidation = async email =>
        {
            // Simulate API call to email validation service
            await Task.Delay(100);

            // Simple validation logic for testing
            return email.Contains("@") && email.Contains(".") && !email.Contains("invalid");
        };

        var validator = new AsyncValidator<TestModel, string>(
            emailValidation,
            "Email address is not valid or disposable");
        var model = new TestModel();

        // Act & Assert
        var validResult = await validator.ValidateAsync(model, "user@example.com", _services);
        validResult.IsValid.ShouldBeTrue();

        var invalidResult = await validator.ValidateAsync(model, "user@invalid.com", _services);
        invalidResult.IsValid.ShouldBeFalse();
        invalidResult.ErrorMessage.ShouldBe("Email address is not valid or disposable");
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Exception_In_Validation_Function()
    {
        // Arrange
        Func<string, Task<bool>> validationFunction = async value =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Simulated service error");
        };

        var validator = new AsyncValidator<TestModel, string>(
            validationFunction,
            "Validation failed due to service error");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, "test", _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Validation failed due to service error");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Complex_Async_Validation()
    {
        // Arrange
        Func<string, Task<bool>> complexValidation = async phoneNumber =>
        {
            // Simulate multiple async operations
            await Task.Delay(25); // First service call

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            await Task.Delay(25); // Second service call

            // Check format
            var cleanNumber = phoneNumber.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");
            if (cleanNumber.Length != 10)
                return false;

            await Task.Delay(25); // Third service call

            // Check if it's a valid area code (simulate)
            var areaCode = cleanNumber.Substring(0, 3);
            var validAreaCodes = new[] { "555", "123", "456" };

            return validAreaCodes.Contains(areaCode);
        };

        var validator = new AsyncValidator<TestModel, string>(
            complexValidation,
            "Phone number is not valid or from an unsupported area");
        var model = new TestModel();

        // Act & Assert
        var validResult = await validator.ValidateAsync(model, "(555) 123-4567", _services);
        validResult.IsValid.ShouldBeTrue();

        var invalidAreaResult = await validator.ValidateAsync(model, "(999) 123-4567", _services);
        invalidAreaResult.IsValid.ShouldBeFalse();

        var invalidFormatResult = await validator.ValidateAsync(model, "123", _services);
        invalidFormatResult.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Cancellation_Token_Timeout()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        Func<string, Task<bool>> slowValidation = async value =>
        {
            await Task.Delay(100, cts.Token); // This should timeout
            return true;
        };

        var validator = new AsyncValidator<TestModel, string>(
            slowValidation,
            "Validation timed out");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, "test", _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Validation timed out");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Multiple_Concurrent_Validations()
    {
        // Arrange
        var callCount = 0;
        Func<string, Task<bool>> validationFunction = async value =>
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(50);
            return !string.IsNullOrEmpty(value);
        };

        var validator = new AsyncValidator<TestModel, string>(validationFunction, "Invalid");
        var model = new TestModel();

        // Act
        var tasks = new List<Task<ValidationResult>>
        {
            validator.ValidateAsync(model, "value1", _services),
            validator.ValidateAsync(model, "value2", _services),
            validator.ValidateAsync(model, "value3", _services),
            validator.ValidateAsync(model, "", _services),
            validator.ValidateAsync(model, "value5", _services)
        };

        var results = await Task.WhenAll(tasks);

        // Assert
        callCount.ShouldBe(5);
        results.Count(r => r.IsValid).ShouldBe(4);
        results.Count(r => !r.IsValid).ShouldBe(1);
    }

    public class TestModel
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}