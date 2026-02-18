# Testing

Comprehensive guide to testing FormCraft forms, validators, and components.

## Test Project Setup

### Required Packages

```xml
<PackageReference Include="bunit" Version="1.*" />
<PackageReference Include="FakeItEasy" Version="8.*" />
<PackageReference Include="FluentAssertions" Version="6.*" /> <!-- or Shouldly -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
```

### Base Test Class for MudBlazor

Create a base test class that configures all required services:

```csharp
using Bunit;
using MudBlazor.Services;

public abstract class MudBlazorTestBase : BunitContext
{
    protected MudBlazorTestBase()
    {
        // Add FormCraft services
        Services.AddFormCraft();

        // Add MudBlazor services
        Services.AddMudServices();

        // Configure JSInterop for MudBlazor
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
```

## Testing FormBuilder Configurations

### Basic Configuration Tests

```csharp
public class FormBuilderTests
{
    [Fact]
    public void Create_Should_Return_New_FormBuilder_Instance()
    {
        // Act
        var builder = FormBuilder<TestModel>.Create();

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<FormBuilder<TestModel>>();
    }

    [Fact]
    public void AddField_Should_Add_Field_To_Configuration()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddField(x => x.Name, field => field
            .WithLabel("Name"));
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        config.Fields.Count.ShouldBe(1);

        var field = config.Fields.First(f => f.FieldName == "Name");
        field.FieldName.ShouldBe("Name");
        field.Label.ShouldBe("Name");
    }

    [Fact]
    public void AddField_WithConfiguration_Should_Apply_All_Settings()
    {
        // Arrange & Act
        var configuration = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Full Name")
                .Required("Name is required")
                .WithPlaceholder("Enter your name"))
            .Build();

        // Assert
        var nameField = configuration.Fields.First(f => f.FieldName == "Name");
        nameField.Label.ShouldBe("Full Name");
        nameField.IsRequired.ShouldBeTrue();
        nameField.Placeholder.ShouldBe("Enter your name");
        nameField.Validators.ShouldContain(v => v.ErrorMessage == "Name is required");
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
```

### Testing Field Order

```csharp
[Fact]
public void AddField_Should_Assign_Incremental_Order()
{
    // Arrange & Act
    var config = FormBuilder<TestModel>.Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .AddField(x => x.Email, field => field.WithLabel("Email"))
        .AddField(x => x.Age, field => field.WithLabel("Age"))
        .Build();

    // Assert
    var nameField = config.Fields.First(f => f.FieldName == "Name");
    var emailField = config.Fields.First(f => f.FieldName == "Email");
    var ageField = config.Fields.First(f => f.FieldName == "Age");

    nameField.Order.ShouldBe(0);
    emailField.Order.ShouldBe(1);
    ageField.Order.ShouldBe(2);
}

[Fact]
public void WithOrder_Should_Override_Default_Order()
{
    // Arrange & Act
    var config = FormBuilder<TestModel>.Create()
        .AddField(x => x.Name, field => field.WithLabel("Name").WithOrder(3))
        .AddField(x => x.Email, field => field.WithLabel("Email").WithOrder(1))
        .AddField(x => x.Age, field => field.WithLabel("Age").WithOrder(2))
        .Build();

    // Assert
    var orderedFields = config.Fields.OrderBy(f => f.Order).ToList();
    orderedFields[0].FieldName.ShouldBe("Email");
    orderedFields[1].FieldName.ShouldBe("Age");
    orderedFields[2].FieldName.ShouldBe("Name");
}
```

### Testing Field Dependencies

```csharp
[Fact]
public void Build_Should_Include_Field_Dependencies()
{
    // Arrange & Act
    var configuration = FormBuilder<TestModel>.Create()
        .AddField(x => x.City, field => field
            .DependsOn(x => x.Country, (m, v) => m.City = string.Empty))
        .Build();

    // Assert
    configuration.FieldDependencies.ShouldContainKey("City");
    configuration.FieldDependencies["City"].Count.ShouldBe(1);
    configuration.FieldDependencies["City"].First().DependentFieldName.ShouldBe("Country");
}

[Fact]
public void VisibleWhen_Should_Set_Visibility_Condition()
{
    // Arrange & Act
    var config = FormBuilder<TestModel>.Create()
        .AddField(x => x.City, field => field
            .VisibleWhen(m => !string.IsNullOrEmpty(m.Country)))
        .Build();

    // Assert
    var cityField = config.Fields.First(f => f.FieldName == "City");
    cityField.VisibilityCondition.ShouldNotBeNull();
}
```

### Testing Form Layout Options

```csharp
[Fact]
public void WithLayout_Should_Set_Layout()
{
    // Act
    var config = FormBuilder<TestModel>.Create()
        .WithLayout(FormLayout.Horizontal)
        .Build();

    // Assert
    config.Layout.ShouldBe(FormLayout.Horizontal);
}

[Fact]
public void Default_Configuration_Should_Have_Expected_Values()
{
    // Act
    var config = FormBuilder<TestModel>.Create().Build();

    // Assert
    config.Layout.ShouldBe(FormLayout.Vertical);
    config.CssClass.ShouldBeNull();
    config.ShowValidationSummary.ShouldBeTrue();
    config.ShowRequiredIndicator.ShouldBeTrue();
    config.RequiredIndicator.ShouldBe("*");
    config.Fields.ShouldBeEmpty();
    config.FieldDependencies.ShouldBeEmpty();
}
```

## Testing Validators

### Testing Sync Validators

```csharp
public class CustomValidatorTests
{
    private readonly IServiceProvider _services = A.Fake<IServiceProvider>();

    [Fact]
    public async Task CustomValidator_Should_Return_Success_When_Valid()
    {
        // Arrange
        var validator = new CustomValidator<TestModel, string>(
            value => !string.IsNullOrEmpty(value),
            "Value is required");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, "Valid", _services);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task CustomValidator_Should_Return_Error_When_Invalid()
    {
        // Arrange
        var validator = new CustomValidator<TestModel, string>(
            value => !string.IsNullOrEmpty(value),
            "Value is required");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, "", _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Value is required");
    }
}
```

### Testing Async Validators

```csharp
public class AsyncValidatorTests
{
    private readonly IServiceProvider _services = A.Fake<IServiceProvider>();

    [Fact]
    public async Task ValidateAsync_Should_Return_Success_When_Function_Returns_True()
    {
        // Arrange
        async Task<bool> ValidationFunction(string value)
        {
            await Task.Delay(10);
            return !string.IsNullOrEmpty(value);
        }

        var validator = new AsyncValidator<TestModel, string>(
            ValidationFunction,
            "Value cannot be empty");
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
        async Task<bool> ValidationFunction(string value)
        {
            await Task.Delay(10);
            return !string.IsNullOrEmpty(value);
        }

        var validator = new AsyncValidator<TestModel, string>(
            ValidationFunction,
            "Value cannot be empty");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, string.Empty, _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Value cannot be empty");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Database_Validation_Simulation()
    {
        // Arrange
        var existingUsernames = new HashSet<string> { "john", "jane", "admin" };

        async Task<bool> UniqueUsernameValidation(string username)
        {
            await Task.Delay(50); // Simulate database call
            return !existingUsernames.Contains(username.ToLower());
        }

        var validator = new AsyncValidator<TestModel, string>(
            UniqueUsernameValidation,
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
    public async Task ValidateAsync_Should_Handle_Exception_In_Validation_Function()
    {
        // Arrange
        async Task<bool> ValidationFunction(string _)
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Simulated service error");
        }

        var validator = new AsyncValidator<TestModel, string>(
            ValidationFunction,
            "Validation failed");
        var model = new TestModel();

        // Act
        var result = await validator.ValidateAsync(model, "test", _services);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Validation failed");
    }

    [Fact]
    public async Task ValidateAsync_Should_Support_Multiple_Concurrent_Validations()
    {
        // Arrange
        var callCount = 0;

        async Task<bool> ValidationFunction(string value)
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(50);
            return !string.IsNullOrEmpty(value);
        }

        var validator = new AsyncValidator<TestModel, string>(ValidationFunction, "Invalid");
        var model = new TestModel();

        // Act
        var tasks = new List<Task<ValidationResult>>
        {
            validator.ValidateAsync(model, "value1", _services),
            validator.ValidateAsync(model, "value2", _services),
            validator.ValidateAsync(model, "", _services)
        };

        var results = await Task.WhenAll(tasks);

        // Assert
        callCount.ShouldBe(3);
        results.Count(r => r.IsValid).ShouldBe(2);
        results.Count(r => !r.IsValid).ShouldBe(1);
    }
}
```

## Component Testing with bUnit

### Basic Component Rendering

```csharp
public class FormCraftComponentTests : MudBlazorTestBase
{
    [Fact]
    public void FormCraftComponent_Should_Render_Form()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.ShouldNotBeNull();
        component.Find("form").ShouldNotBeNull();
    }

    [Fact]
    public void FormCraftComponent_Should_Render_All_Fields()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .AddField(x => x.Email, field => field.WithLabel("Email"))
            .AddField(x => x.Age, field => field.WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var fields = component.FindAll(".mb-4");
        fields.Count.ShouldBe(3);
    }

    [Fact]
    public void FormCraftComponent_Should_Have_Novalidate_Attribute()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var form = component.Find("form");
        form.Attributes
            .FirstOrDefault(a => a.Name == "novalidate")?
            .Value.ShouldBe("novalidate");
    }
}
```

### Testing Field Visibility

```csharp
[Fact]
public void FormCraftComponent_Should_Hide_Fields_When_Visibility_Condition_False()
{
    // Arrange
    var model = new TestModel { ShowEmail = false };
    var config = FormBuilder<TestModel>
        .Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .AddField(x => x.Email, field => field
            .WithLabel("Email")
            .VisibleWhen(m => m.ShowEmail))
        .Build();

    // Act
    var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
        .Add(p => p.Model, model)
        .Add(p => p.Configuration, config));

    // Assert
    var fields = component.FindAll(".mb-4");
    fields.Count.ShouldBe(1);

    var labels = component.FindAll(".mud-input-label");
    labels.Count.ShouldBe(1);
    labels[0].TextContent.ShouldContain("Name");
}

[Fact]
public void FormCraftComponent_Should_Show_Fields_When_Visibility_Condition_True()
{
    // Arrange
    var model = new TestModel { ShowEmail = true };
    var config = FormBuilder<TestModel>
        .Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .AddField(x => x.Email, field => field
            .WithLabel("Email")
            .VisibleWhen(m => m.ShowEmail))
        .Build();

    // Act
    var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
        .Add(p => p.Model, model)
        .Add(p => p.Configuration, config));

    // Assert
    var fields = component.FindAll(".mb-4");
    fields.Count.ShouldBe(2);
}
```

### Testing Field Input and Model Updates

```csharp
[Fact]
public async Task TextField_Should_Update_Model_On_Input()
{
    // Arrange
    var model = new TestModel { Name = "" };
    var config = FormBuilder<TestModel>
        .Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .Build();

    var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
        .Add(p => p.Model, model)
        .Add(p => p.Configuration, config));

    var mudTextField = component.FindComponent<MudTextField<string>>();

    // Act
    await mudTextField.InvokeAsync(() => mudTextField.Instance.SetText("John Doe"));

    // Assert
    model.Name.ShouldBe("John Doe");
}

[Fact]
public void TextField_Should_Display_Initial_Value()
{
    // Arrange
    var model = new TestModel { Name = "Jane Doe" };
    var config = FormBuilder<TestModel>
        .Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .Build();

    // Act
    var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
        .Add(p => p.Model, model)
        .Add(p => p.Configuration, config));

    // Assert
    var mudTextField = component.FindComponent<MudTextField<string>>();
    mudTextField.Instance.Value.ShouldBe("Jane Doe");
}
```

### Testing Different Field Types

```csharp
[Fact]
public void FormCraftComponent_Should_Render_Different_Field_Types()
{
    // Arrange
    var model = new TestModel();
    var config = FormBuilder<TestModel>
        .Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .AddField(x => x.Age, field => field.WithLabel("Age"))
        .AddField(x => x.IsActive, field => field.WithLabel("Is Active"))
        .Build();

    // Act
    var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
        .Add(p => p.Model, model)
        .Add(p => p.Configuration, config));

    // Assert
    component.FindComponents<MudTextField<string>>().Count.ShouldBe(1);
    component.FindComponents<MudNumericField<int>>().Count.ShouldBe(1);
    component.FindComponents<MudCheckBox<bool>>().Count.ShouldBe(1);
}
```

### Testing Submit Button

```csharp
[Fact]
public void FormCraftComponent_Should_Render_Submit_Button()
{
    // Arrange
    var model = new TestModel();
    var config = FormBuilder<TestModel>
        .Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .Build();

    // Act
    var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
        .Add(p => p.Model, model)
        .Add(p => p.Configuration, config)
        .Add(p => p.ShowSubmitButton, true)
        .Add(p => p.SubmitButtonText, "Submit Form"));

    // Assert
    var submitButton = component.Find(".mud-button");
    submitButton.ShouldNotBeNull();
    submitButton.TextContent.ShouldContain("Submit Form");
}

[Fact]
public void FormCraftComponent_Should_Hide_Submit_Button_When_ShowSubmitButton_Is_False()
{
    // Arrange
    var model = new TestModel();
    var config = FormBuilder<TestModel>
        .Create()
        .AddField(x => x.Name, field => field.WithLabel("Name"))
        .Build();

    // Act
    var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
        .Add(p => p.Model, model)
        .Add(p => p.Configuration, config)
        .Add(p => p.ShowSubmitButton, false));

    // Assert
    var buttons = component.FindAll("button[type='submit']");
    buttons.Count.ShouldBe(0);
}
```

## Testing FluentValidation Integration

### Setting Up FluentValidation Tests

```csharp
public class FluentValidationTests : MudBlazorTestBase
{
    public FluentValidationTests()
    {
        // Register your validators
        Services.AddScoped<IValidator<TestModel>, TestModelValidator>();
    }

    [Fact]
    public async Task FluentValidation_Should_Validate_Field()
    {
        // Arrange
        var validator = new TestModelValidator();
        var model = new TestModel { Email = "invalid-email" };

        // Act
        var result = await validator.ValidateAsync(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }
}

public class TestModelValidator : AbstractValidator<TestModel>
{
    public TestModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 100).WithMessage("Age must be between 18 and 100");
    }
}
```

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test FormCraft.UnitTests/FormCraft.UnitTests.csproj
dotnet test FormCraft.ForMudBlazor.UnitTests/FormCraft.ForMudBlazor.UnitTests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~FormBuilderTests"

# Run specific test method
dotnet test --filter "DisplayName~Should_Build_Valid_Configuration"

# Run tests by category
dotnet test --filter "Category=Builder"
dotnet test --filter "Category=Validator"
```

### Test Naming Convention

Follow the pattern: `MethodName_Should_ExpectedBehavior_When_Condition`

```csharp
[Fact]
public void AddField_Should_Add_Field_To_Configuration()

[Fact]
public void VisibleWhen_Should_Hide_Field_When_Condition_False()

[Fact]
public async Task ValidateAsync_Should_Return_Error_When_Invalid()
```

## Best Practices

1. **Use Arrange-Act-Assert pattern** for clear test structure
2. **Test one thing per test** - keep tests focused
3. **Use meaningful test names** that describe the behavior
4. **Mock external dependencies** using FakeItEasy or similar
5. **Test edge cases** - null values, empty strings, boundary conditions
6. **Test async code properly** - use `async Task` return types
7. **Keep test data simple** - use minimal models for tests
8. **Clean up resources** - dispose of bUnit components properly
