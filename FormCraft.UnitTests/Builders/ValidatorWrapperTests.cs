namespace FormCraft.UnitTests.Builders;

public class ValidatorWrapperTests
{
    [Fact]
    public void ValidatorWrapper_Should_Initialize_With_Inner_Validator()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, string>>();

        // Act
        var wrapper = new ValidatorWrapper<TestModel, string>(innerValidator);

        // Assert
        wrapper.ShouldNotBeNull();
    }

    [Fact]
    public void ErrorMessage_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, string>>();
        var wrapper = new ValidatorWrapper<TestModel, string>(innerValidator);

        // Act - Get
        var _ = wrapper.ErrorMessage;

        // Assert - Get
        A.CallTo(() => innerValidator.ErrorMessage).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.ErrorMessage = "Test Error";

        // Assert - Set
        A.CallToSet(() => innerValidator.ErrorMessage).To("Test Error").MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ValidateAsync_Should_Convert_Object_To_TValue_And_Call_Inner()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, string>>();
        var expectedResult = ValidationResult.Success();
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<string>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, string>(innerValidator);
        var model = new TestModel();
        var value = "test value";
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, value, services);

        // Assert
        result.ShouldBe(expectedResult);
        A.CallTo(() => innerValidator.ValidateAsync(model, "test value", services))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Null_Value()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, string>>();
        var expectedResult = ValidationResult.Failure("Required");
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<string>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, string>(innerValidator);
        var model = new TestModel();
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, null, services);

        // Assert
        result.ShouldBe(expectedResult);
        A.CallTo(() => innerValidator.ValidateAsync(model, A<string>.That.IsNull(), services))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Type_Conversion_Exception()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, int>>();
        var expectedResult = ValidationResult.Failure("Test error");
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<int>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, int>(innerValidator);
        var model = new TestModel();
        var invalidValue = "not a number"; // This will cause conversion exception
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, invalidValue, services);

        // Assert
        result.ShouldBe(expectedResult);
        // Should call inner validator with default value (0 for int) due to conversion failure
        A.CallTo(() => innerValidator.ValidateAsync(model, 0, services))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ValidateAsync_Should_Convert_Compatible_Types()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, int>>();
        var expectedResult = ValidationResult.Success();
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<int>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, int>(innerValidator);
        var model = new TestModel();
        var value = 42;
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, value, services);

        // Assert
        result.ShouldBe(expectedResult);
        A.CallTo(() => innerValidator.ValidateAsync(model, 42, services))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Boolean_Conversion()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, bool>>();
        var expectedResult = ValidationResult.Success();
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<bool>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, bool>(innerValidator);
        var model = new TestModel();
        var value = true;
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, value, services);

        // Assert
        result.ShouldBe(expectedResult);
        A.CallTo(() => innerValidator.ValidateAsync(model, true, services))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_DateTime_Conversion()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, DateTime>>();
        var expectedResult = ValidationResult.Success();
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<DateTime>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, DateTime>(innerValidator);
        var model = new TestModel();
        var value = new DateTime(2023, 1, 1);
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, value, services);

        // Assert
        result.ShouldBe(expectedResult);
        A.CallTo(() => innerValidator.ValidateAsync(model, new DateTime(2023, 1, 1), services))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ValidateAsync_Should_Use_Default_For_Null_With_Value_Types()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, int>>();
        var expectedResult = ValidationResult.Failure("Test error");
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<int>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, int>(innerValidator);
        var model = new TestModel();
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, null, services);

        // Assert
        result.ShouldBe(expectedResult);
        // For value types, null should convert to default (0 for int)
        A.CallTo(() => innerValidator.ValidateAsync(model, 0, services))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_String_To_Numeric_Conversion_Failure()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, decimal>>();
        var expectedResult = ValidationResult.Failure("Test error");
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<decimal>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, decimal>(innerValidator);
        var model = new TestModel();
        var invalidValue = "not-a-decimal";
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, invalidValue, services);

        // Assert
        result.ShouldBe(expectedResult);
        // Should call with default value (0.0m) due to conversion failure
        A.CallTo(() => innerValidator.ValidateAsync(model, 0.0m, services))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void ValidatorWrapper_Should_Implement_Correct_Interface()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, string>>();

        // Act
        var wrapper = new ValidatorWrapper<TestModel, string>(innerValidator);

        // Assert
        wrapper.ShouldBeAssignableTo<IFieldValidator<TestModel, object>>();
    }

    [Fact]
    public async Task ValidateAsync_Should_Propagate_Exceptions_From_Inner_Validator()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, string>>();
        var expectedException = new InvalidOperationException("Validation failed");
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<string>._, A<IServiceProvider>._))
            .Throws(expectedException);

        var wrapper = new ValidatorWrapper<TestModel, string>(innerValidator);
        var model = new TestModel();
        var value = "test";
        var services = A.Fake<IServiceProvider>();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await wrapper.ValidateAsync(model, value, services));

        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Complex_Object_Types()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, TestModel>>();
        var expectedResult = ValidationResult.Success();
        A.CallTo(() => innerValidator.ValidateAsync(A<TestModel>._, A<TestModel>._, A<IServiceProvider>._))
            .Returns(expectedResult);

        var wrapper = new ValidatorWrapper<TestModel, TestModel>(innerValidator);
        var model = new TestModel();
        var value = new TestModel { Name = "Test" };
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = await wrapper.ValidateAsync(model, value, services);

        // Assert
        result.ShouldBe(expectedResult);
        A.CallTo(() => innerValidator.ValidateAsync(model, value, services))
            .MustHaveHappenedOnceExactly();
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}