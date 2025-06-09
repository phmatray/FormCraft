namespace FormCraft.UnitTests.Components;

public class DynamicFormValidatorTests
{
    [Fact]
    public void DynamicFormValidator_Should_Initialize_With_Configuration()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        // Act
        var validator = new DynamicFormValidator<TestModel>();
        validator.Configuration = config;

        // Assert
        validator.Configuration.ShouldNotBeNull();
        validator.Configuration.ShouldBe(config);
    }

    [Fact]
    public void DynamicFormValidator_Should_Handle_Null_Configuration()
    {
        // Arrange & Act
        var validator = new DynamicFormValidator<TestModel>();
        // Configuration remains null

        // Assert
        validator.Configuration.ShouldBeNull();
    }

    [Fact]
    public void DynamicFormValidator_Should_Allow_Configuration_Property_Setting()
    {
        // Arrange
        var config1 = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        var config2 = FormBuilder<TestModel>.Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email"))
            .Build();

        var validator = new DynamicFormValidator<TestModel>();

        // Act
        validator.Configuration = config1;
        validator.Configuration.ShouldBe(config1);

        validator.Configuration = config2;

        // Assert
        validator.Configuration.ShouldBe(config2);
    }

    [Fact]
    public void DynamicFormValidator_Should_Dispose_Without_Exception()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        var validator = new DynamicFormValidator<TestModel>();
        validator.Configuration = config;

        // Act & Assert
        Should.NotThrow(() => validator.Dispose());
    }

    [Fact]
    public void DynamicFormValidator_Should_Dispose_Multiple_Times_Without_Exception()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .Build();

        var validator = new DynamicFormValidator<TestModel>();
        validator.Configuration = config;

        // Act & Assert
        Should.NotThrow(() =>
        {
            validator.Dispose();
            validator.Dispose(); // Should not throw on second disposal
        });
    }

    [Fact]
    public void DynamicFormValidator_Should_Handle_Dispose_With_Null_Configuration()
    {
        // Arrange
        var validator = new DynamicFormValidator<TestModel>();
        // Configuration remains null

        // Act & Assert
        Should.NotThrow(() => validator.Dispose());
    }

    [Fact]
    public void DynamicFormValidator_Should_Be_Generic_Type()
    {
        // Arrange & Act
        var validator = new DynamicFormValidator<TestModel>();

        // Assert
        validator.ShouldNotBeNull();
        typeof(DynamicFormValidator<TestModel>).IsGenericType.ShouldBeTrue();
        typeof(DynamicFormValidator<TestModel>).GetGenericArguments()[0].ShouldBe(typeof(TestModel));
    }

    [Fact]
    public void DynamicFormValidator_Should_Work_With_Different_Model_Types()
    {
        // Arrange & Act
        var validator1 = new DynamicFormValidator<TestModel>();
        var validator2 = new DynamicFormValidator<AnotherTestModel>();

        // Assert
        validator1.ShouldNotBeNull();
        validator2.ShouldNotBeNull();
        validator1.GetType().ShouldNotBe(validator2.GetType());
    }

    [Fact]
    public void DynamicFormValidator_Should_Implement_IDisposable()
    {
        // Arrange & Act
        var validator = new DynamicFormValidator<TestModel>();

        // Assert
        validator.ShouldBeAssignableTo<IDisposable>();
    }

    [Fact]
    public void DynamicFormValidator_Configuration_Should_Accept_Complex_Forms()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithPlaceholder("Enter name"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email"))
            .AddField(x => x.Age, field => field
                .WithLabel("Age"))
            .Build();

        // Act
        var validator = new DynamicFormValidator<TestModel>();
        validator.Configuration = config;

        // Assert
        validator.Configuration.ShouldNotBeNull();
        validator.Configuration.Fields.Count().ShouldBe(3);
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class AnotherTestModel
    {
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}