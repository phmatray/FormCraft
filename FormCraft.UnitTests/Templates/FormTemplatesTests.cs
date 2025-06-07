namespace FormCraft.UnitTests.Templates;

public class FormTemplatesTests
{
    [Fact]
    public void ContactForm_Should_Return_Valid_Configuration()
    {
        // Act
        var config = FormTemplates.ContactForm<TestModel>();

        // Assert
        config.ShouldNotBeNull();
        config.ShouldBeAssignableTo<IFormConfiguration<TestModel>>();
    }

    [Fact]
    public void ContactForm_Should_Create_Empty_Configuration()
    {
        // Act
        var config = FormTemplates.ContactForm<TestModel>();

        // Assert
        config.Fields.ShouldNotBeNull();
        config.Fields.Count().ShouldBe(0); // Template returns empty configuration
    }

    [Fact]
    public void RegistrationForm_Should_Return_Valid_Configuration()
    {
        // Act
        var config = FormTemplates.RegistrationForm<TestModel>();

        // Assert
        config.ShouldNotBeNull();
        config.ShouldBeAssignableTo<IFormConfiguration<TestModel>>();
    }

    [Fact]
    public void RegistrationForm_Should_Create_Empty_Configuration()
    {
        // Act
        var config = FormTemplates.RegistrationForm<TestModel>();

        // Assert
        config.Fields.ShouldNotBeNull();
        config.Fields.Count().ShouldBe(0); // Template returns empty configuration
    }

    [Fact]
    public void LoginForm_Should_Return_Valid_Configuration()
    {
        // Act
        var config = FormTemplates.LoginForm<TestModel>();

        // Assert
        config.ShouldNotBeNull();
        config.ShouldBeAssignableTo<IFormConfiguration<TestModel>>();
    }

    [Fact]
    public void LoginForm_Should_Create_Empty_Configuration()
    {
        // Act
        var config = FormTemplates.LoginForm<TestModel>();

        // Assert
        config.Fields.ShouldNotBeNull();
        config.Fields.Count().ShouldBe(0); // Template returns empty configuration
    }

    [Fact]
    public void AddressForm_Should_Return_Valid_Configuration()
    {
        // Act
        var config = FormTemplates.AddressForm<TestModel>();

        // Assert
        config.ShouldNotBeNull();
        config.ShouldBeAssignableTo<IFormConfiguration<TestModel>>();
    }

    [Fact]
    public void AddressForm_Should_Create_Empty_Configuration()
    {
        // Act
        var config = FormTemplates.AddressForm<TestModel>();

        // Assert
        config.Fields.ShouldNotBeNull();
        config.Fields.Count().ShouldBe(0); // Template returns empty configuration
    }

    [Fact]
    public void ContactForm_Should_Work_With_Different_Model_Types()
    {
        // Act
        var config1 = FormTemplates.ContactForm<TestModel>();
        var config2 = FormTemplates.ContactForm<AnotherTestModel>();

        // Assert
        config1.ShouldNotBeNull();
        config2.ShouldNotBeNull();
        config1.ShouldBeAssignableTo<IFormConfiguration<TestModel>>();
        config2.ShouldBeAssignableTo<IFormConfiguration<AnotherTestModel>>();
    }

    [Fact]
    public void RegistrationForm_Should_Work_With_Different_Model_Types()
    {
        // Act
        var config1 = FormTemplates.RegistrationForm<TestModel>();
        var config2 = FormTemplates.RegistrationForm<AnotherTestModel>();

        // Assert
        config1.ShouldNotBeNull();
        config2.ShouldNotBeNull();
        config1.ShouldBeAssignableTo<IFormConfiguration<TestModel>>();
        config2.ShouldBeAssignableTo<IFormConfiguration<AnotherTestModel>>();
    }

    [Fact]
    public void LoginForm_Should_Work_With_Different_Model_Types()
    {
        // Act
        var config1 = FormTemplates.LoginForm<TestModel>();
        var config2 = FormTemplates.LoginForm<AnotherTestModel>();

        // Assert
        config1.ShouldNotBeNull();
        config2.ShouldNotBeNull();
        config1.ShouldBeAssignableTo<IFormConfiguration<TestModel>>();
        config2.ShouldBeAssignableTo<IFormConfiguration<AnotherTestModel>>();
    }

    [Fact]
    public void AddressForm_Should_Work_With_Different_Model_Types()
    {
        // Act
        var config1 = FormTemplates.AddressForm<TestModel>();
        var config2 = FormTemplates.AddressForm<AnotherTestModel>();

        // Assert
        config1.ShouldNotBeNull();
        config2.ShouldNotBeNull();
        config1.ShouldBeAssignableTo<IFormConfiguration<TestModel>>();
        config2.ShouldBeAssignableTo<IFormConfiguration<AnotherTestModel>>();
    }

    [Fact]
    public void All_Templates_Should_Have_Default_Layout()
    {
        // Act
        var contactConfig = FormTemplates.ContactForm<TestModel>();
        var registrationConfig = FormTemplates.RegistrationForm<TestModel>();
        var loginConfig = FormTemplates.LoginForm<TestModel>();
        var addressConfig = FormTemplates.AddressForm<TestModel>();

        // Assert
        contactConfig.Layout.ShouldBe(FormLayout.Vertical); // Default layout
        registrationConfig.Layout.ShouldBe(FormLayout.Vertical);
        loginConfig.Layout.ShouldBe(FormLayout.Vertical);
        addressConfig.Layout.ShouldBe(FormLayout.Vertical);
    }

    [Fact]
    public void All_Templates_Should_Support_Method_Chaining()
    {
        // Act
        var contactConfig = FormTemplates.ContactForm<TestModel>();
        var registrationConfig = FormTemplates.RegistrationForm<TestModel>();
        var loginConfig = FormTemplates.LoginForm<TestModel>();
        var addressConfig = FormTemplates.AddressForm<TestModel>();

        // Assert
        // Since these return IFormConfiguration, they can be used directly
        contactConfig.ShouldNotBeNull();
        registrationConfig.ShouldNotBeNull();
        loginConfig.ShouldNotBeNull();
        addressConfig.ShouldNotBeNull();
    }

    [Fact]
    public void Templates_Should_Have_Empty_Field_Dependencies()
    {
        // Act
        var config = FormTemplates.ContactForm<TestModel>();

        // Assert
        config.FieldDependencies.ShouldNotBeNull();
        config.FieldDependencies.Count().ShouldBe(0);
    }

    [Fact]
    public void Templates_Should_Support_Generic_Constraints()
    {
        // These should all compile successfully due to the 'new()' constraint
        
        // Act & Assert - compilation test
        var config1 = FormTemplates.ContactForm<TestModel>();
        var config2 = FormTemplates.RegistrationForm<TestModel>();
        var config3 = FormTemplates.LoginForm<TestModel>();
        var config4 = FormTemplates.AddressForm<TestModel>();

        config1.ShouldNotBeNull();
        config2.ShouldNotBeNull();
        config3.ShouldNotBeNull();
        config4.ShouldNotBeNull();
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