using FluentValidation;
using FluentValidation.Results;

namespace FormCraft.UnitTests.Validators;

public class FluentValidationAdapterTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IValidator<TestModel> _validator;

    public FluentValidationAdapterTests()
    {
        _validator = A.Fake<IValidator<TestModel>>();

        var services = new ServiceCollection();
        services.AddSingleton(_validator);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ValidateAsync_WhenNoValidatorRegistered_ReturnsSuccess()
    {
        // Arrange
        var emptyServiceProvider = new ServiceCollection().BuildServiceProvider();
        var adapter = new FluentValidationAdapter<TestModel, string>(x => x.Name);
        var model = new TestModel { Name = "Test" };

        // Act
        var result = await adapter.ValidateAsync(model, "Test", emptyServiceProvider);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenValidationPasses_ReturnsSuccess()
    {
        // Arrange
        var adapter = new FluentValidationAdapter<TestModel, string>(x => x.Name);
        var model = new TestModel { Name = "ValidName" };

        A.CallTo(() => _validator.ValidateAsync(A<ValidationContext<TestModel>>._, CancellationToken.None))
            .Returns(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await adapter.ValidateAsync(model, "ValidName", _serviceProvider);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenValidationFails_ReturnsFailureWithMessage()
    {
        // Arrange
        var adapter = new FluentValidationAdapter<TestModel, string>(x => x.Name);
        var model = new TestModel { Name = "" };
        var expectedError = "Name is required";

        var validationResult = new FluentValidation.Results.ValidationResult([
            new ValidationFailure("Name", expectedError)
        ]);

        A.CallTo(() => _validator.ValidateAsync(A<ValidationContext<TestModel>>._, CancellationToken.None))
            .Returns(validationResult);

        // Act
        var result = await adapter.ValidateAsync(model, "", _serviceProvider);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
    }

    [Fact]
    public async Task ValidateAsync_WhenValidationFailsForDifferentProperty_ReturnsSuccess()
    {
        // Arrange
        var adapter = new FluentValidationAdapter<TestModel, string>(x => x.Name);
        var model = new TestModel { Name = "ValidName", Email = "" };

        var validationResult = new FluentValidation.Results.ValidationResult([
            new ValidationFailure("Email", "Email is required")
        ]);

        A.CallTo(() => _validator.ValidateAsync(A<ValidationContext<TestModel>>._, CancellationToken.None))
            .Returns(validationResult);

        // Act
        var result = await adapter.ValidateAsync(model, "ValidName", _serviceProvider);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithNestedProperty_ExtractsCorrectPropertyName()
    {
        // Arrange
        var adapter = new FluentValidationAdapter<TestModel, string>(x => x.Address.Street);
        var model = new TestModel
        {
            Name = "Test",
            Address = new Address { Street = "" }
        };

        var validationResult = new FluentValidation.Results.ValidationResult([
            new ValidationFailure("Address.Street", "Street is required")
        ]);

        A.CallTo(() => _validator.ValidateAsync(A<ValidationContext<TestModel>>._, CancellationToken.None))
            .Returns(validationResult);

        // Act
        var result = await adapter.ValidateAsync(model, "", _serviceProvider);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Street is required");
    }

    [Fact]
    public async Task WithFluentValidator_UsesProvidedValidatorNotDI()
    {
        // Arrange
        var specificValidator = A.Fake<IValidator<TestModel>>();
        var model = new TestModel { Name = "" };

        var validationResult = new FluentValidation.Results.ValidationResult([
            new ValidationFailure("Name", "Custom error")
        ]);

        A.CallTo(() => specificValidator.ValidateAsync(A<ValidationContext<TestModel>>._, CancellationToken.None))
            .Returns(validationResult);

        // Create adapter through extension method
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithFluentValidator(specificValidator, x => x.Name))
            .Build();

        var fieldConfig = formConfig.Fields.First();
        var adapter = fieldConfig.Validators.First();

        // Act
        var result = await adapter.ValidateAsync(model, "", _serviceProvider);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Custom error");

        // Verify the DI validator was not used
        A.CallTo(() => _validator.ValidateAsync(A<ValidationContext<TestModel>>._, CancellationToken.None))
            .MustNotHaveHappened();
    }

    public class TestModel
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public Address Address { get; set; } = new();
    }

    public class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
    }
}