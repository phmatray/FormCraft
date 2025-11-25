using FluentValidation;

namespace FormCraft.UnitTests.Validators;

public class FluentValidationExtensionsTests
{
    [Fact]
    public void WithFluentValidation_AddsFluentValidationAdapter()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithFluentValidation(x => x.Name))
            .Build();

        // Assert
        var fieldConfig = formConfig.Fields.First();
        fieldConfig.Validators.ShouldNotBeEmpty();
        // Validators are wrapped in ValidatorWrapper by FormCraft
        var validator = fieldConfig.Validators.First();
        validator.ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void WithFluentValidator_AddsSpecificValidatorAdapter()
    {
        // Arrange
        var validator = A.Fake<IValidator<TestModel>>();

        // Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithFluentValidator(validator, x => x.Name))
            .Build();

        // Assert
        var fieldConfig = formConfig.Fields.First();
        fieldConfig.Validators.ShouldNotBeEmpty();
        var validatorAdapter = fieldConfig.Validators.First();
        // The validator is wrapped in ValidatorWrapper
        validatorAdapter.ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public async Task WithFluentValidation_IntegrationTest_ValidatesUsingRegisteredValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        var validator = new TestModelValidator();
        services.AddSingleton<IValidator<TestModel>>(validator);
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithFluentValidation(x => x.Name))
            .Build();

        var model = new TestModel { Name = "" };
        var fieldConfig = formConfig.Fields.First();

        // Act
        var validationResult = await fieldConfig.Validators.First()
            .ValidateAsync(model, "", serviceProvider);

        // Assert
        validationResult.IsValid.ShouldBeFalse();
        validationResult.ErrorMessage.ShouldBe("Name is required");
    }

    [Fact]
    public async Task WithFluentValidator_IntegrationTest_ValidatesUsingProvidedValidator()
    {
        // Arrange
        var validator = new TestModelValidator();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithFluentValidator(validator, x => x.Name))
            .Build();

        var model = new TestModel { Name = "a" }; // Too short
        var fieldConfig = formConfig.Fields.First();

        // Act
        var validationResult = await fieldConfig.Validators.First()
            .ValidateAsync(model, "a", serviceProvider);

        // Assert
        validationResult.IsValid.ShouldBeFalse();
        validationResult.ErrorMessage.ShouldBe("Name must be at least 3 characters");
    }

    [Fact]
    public void WithFluentValidation_CanChainMultipleValidators()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .Required("Required by FormCraft")
                .WithFluentValidation(x => x.Name)
                .WithMinLength(5, "Minimum length validation"))
            .Build();

        // Assert
        var fieldConfig = formConfig.Fields.First();
        fieldConfig.Validators.Count.ShouldBe(3);
        // All validators are wrapped in ValidatorWrapper
        fieldConfig.Validators[0].ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
        fieldConfig.Validators[1].ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
        fieldConfig.Validators[2].ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    public class TestModel
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    private class TestModelValidator : AbstractValidator<TestModel>
    {
        public TestModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
        }
    }
}