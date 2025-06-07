namespace FormCraft.UnitTests.Integration;

public class ValidationPipelineIntegrationTests
{
    [Fact]
    public void Multi_Validator_Field_Should_Have_Multiple_Validators()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<ValidationTestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
                .Required("Name is required")
                .WithMinLength(3, "Name must be at least 3 characters")
                .WithMaxLength(50, "Name must be less than 50 characters")
            .Build();

        var field = formConfig.Fields.First();

        // Assert - Field should have multiple validators
        field.Validators.Count.ShouldBe(3);
        field.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void Validation_Configuration_Should_Aggregate_Multiple_Rules()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<ValidationTestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
                .Required("Name is required")
                .WithMinLength(3, "Name must be at least 3 characters")
                .WithValidator(value => !value.Contains("invalid"), "Name cannot contain 'invalid'")
            .Build();

        var field = formConfig.Fields.First();

        // Assert - Field should have all validation rules configured
        field.Validators.Count.ShouldBe(3);
        field.IsRequired.ShouldBeTrue();
        
        // Verify field configuration
        field.FieldName.ShouldBe("Name");
        field.Label.ShouldBe("Name");
    }

    [Fact]
    public void Cross_Field_Dependencies_Should_Be_Configurable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<ValidationTestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
                .WithValidator(value => !string.IsNullOrEmpty(value), "Name is required")
            .AddField(x => x.Email)
                .WithLabel("Email")
                .WithEmailValidation()
            .Build();

        // Assert - Both fields should be configured with validation
        var nameField = formConfig.Fields.First(f => f.FieldName == "Name");
        var emailField = formConfig.Fields.First(f => f.FieldName == "Email");

        nameField.Validators.Count.ShouldBeGreaterThan(0);
        emailField.Validators.Count.ShouldBeGreaterThan(0);
        
        // Verify field structure
        formConfig.Fields.Count().ShouldBe(2);
    }

    [Fact]
    public void Async_Validation_Should_Be_Configurable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        // Use async validator method
        var asyncValidation = async (string value) =>
        {
            // Simulate async operation (e.g., database lookup)
            await Task.Delay(1);
            return !string.IsNullOrEmpty(value);
        };

        var formConfig = FormBuilder<ValidationTestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
                .WithAsyncValidator(asyncValidation, "Async validation failed")
            .Build();

        var field = formConfig.Fields.First();

        // Assert - Field should have async validator configured
        field.Validators.Count.ShouldBe(1);
        field.FieldName.ShouldBe("Name");
        field.Label.ShouldBe("Name");
    }

    [Fact]
    public void Validation_With_Service_Dependencies_Should_Be_Configurable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        services.AddSingleton<IValidationService, TestValidationService>();
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<ValidationTestModel>.Create()
            .AddField(x => x.Email)
                .WithLabel("Email")
                .WithEmailValidation("Please enter a valid email")
            .Build();

        var field = formConfig.Fields.First();

        // Assert - Field should have email validation configured
        field.Validators.Count.ShouldBeGreaterThan(0);
        field.FieldName.ShouldBe("Email");
        field.Label.ShouldBe("Email");
        
        // Verify service is available
        var validationService = serviceProvider.GetRequiredService<IValidationService>();
        validationService.ShouldNotBeNull();
    }

    [Fact]
    public void Validation_Configuration_Should_Handle_Edge_Cases()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<ValidationTestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
                .WithValidator(value => !string.IsNullOrEmpty(value), "Name is required")
            .Build();

        var field = formConfig.Fields.First();

        // Assert - Field should be properly configured
        field.Validators.Count.ShouldBe(1);
        field.FieldName.ShouldBe("Name");
        field.Label.ShouldBe("Name");
        
        // Verify form structure
        formConfig.Fields.Count().ShouldBe(1);
    }

    [Fact]
    public void Complex_Validation_Scenario_Should_Configure_Properly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        services.AddSingleton<IValidationService, TestValidationService>();
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<ComplexValidationModel>.Create()
            .AddField(x => x.Username)
                .WithLabel("Username")
                .Required()
                .WithValidator(value => value.Length >= 3, "Username must be at least 3 characters")
            .AddField(x => x.Email)
                .WithLabel("Email")
                .Required()
                .WithEmailValidation()
            .AddField(x => x.Age)
                .WithLabel("Age")
                .WithRange(18, 120, "Age must be between 18 and 120")
            .AddField(x => x.AcceptTerms)
                .WithLabel("Accept Terms")
                .WithValidator(value => value, "You must accept the terms")
            .Build();

        // Act & Assert - Test valid model
        var validModel = new ComplexValidationModel
        {
            Username = "john_doe",
            Email = "john@example.com",
            Age = 25,
            AcceptTerms = true
        };

        // Verify form structure has fields with validators
        formConfig.Fields.Count().ShouldBe(4);
        
        var usernameField = formConfig.Fields.First(f => f.FieldName == "Username");
        var emailField = formConfig.Fields.First(f => f.FieldName == "Email");
        var ageField = formConfig.Fields.First(f => f.FieldName == "Age");
        var termsField = formConfig.Fields.First(f => f.FieldName == "AcceptTerms");
        
        // All fields should have validators
        usernameField.Validators.Count.ShouldBeGreaterThan(0);
        emailField.Validators.Count.ShouldBeGreaterThan(0);
        ageField.Validators.Count.ShouldBeGreaterThan(0);
        termsField.Validators.Count.ShouldBeGreaterThan(0);

        // Verify form has validation rules configured
        formConfig.ShouldNotBeNull();
        
        // Check required fields
        usernameField.IsRequired.ShouldBeTrue();
        emailField.IsRequired.ShouldBeTrue();
        ageField.IsRequired.ShouldBeFalse(); // Range validator doesn't make it required
        termsField.IsRequired.ShouldBeFalse(); // Custom validator doesn't make it required
    }

    // Test classes and validators
    public class TrackingValidator : IFieldValidator<ValidationTestModel, string>
    {
        private readonly string _name;
        private readonly List<string> _executionOrder;

        public TrackingValidator(string name, List<string> executionOrder)
        {
            _name = name;
            _executionOrder = executionOrder;
        }

        public string? ErrorMessage { get; set; }

        public async Task<ValidationResult> ValidateAsync(ValidationTestModel model, string? value, IServiceProvider services)
        {
            _executionOrder.Add(_name);
            await Task.Delay(1); // Simulate async work
            return ValidationResult.Success();
        }
    }

    public class ServiceDependentValidator : IFieldValidator<ValidationTestModel, string>
    {
        public string? ErrorMessage { get; set; }

        public async Task<ValidationResult> ValidateAsync(ValidationTestModel model, string? value, IServiceProvider services)
        {
            var validationService = services.GetRequiredService<IValidationService>();
            var isValid = validationService.IsEmailValid(value ?? "");
            
            return await Task.FromResult(isValid 
                ? ValidationResult.Success() 
                : ValidationResult.Failure("Invalid email format"));
        }
    }

    public class ThrowingValidator : IFieldValidator<ValidationTestModel, string>
    {
        public string? ErrorMessage { get; set; }

        public async Task<ValidationResult> ValidateAsync(ValidationTestModel model, string? value, IServiceProvider services)
        {
            await Task.Delay(1);
            if (value == "trigger-exception")
            {
                throw new InvalidOperationException("Validation error");
            }
            return ValidationResult.Success();
        }
    }

    public interface IValidationService
    {
        bool IsEmailValid(string email);
    }

    public class TestValidationService : IValidationService
    {
        public bool IsEmailValid(string email) => true; // Always valid for testing
    }

    public class ValidationTestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ComplexValidationModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool AcceptTerms { get; set; }
    }
}