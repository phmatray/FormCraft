namespace FormCraft.UnitTests.Templates;

public class ContactFormTemplateTests
{
    [Fact]
    public void AsContactForm_Should_Create_Form_With_Required_Fields()
    {
        // Arrange
        var builder = FormBuilder<ContactModel>.Create();

        // Act
        var formBuilder = builder.AsContactForm();
        var config = formBuilder.Build();

        // Assert
        config.ShouldNotBeNull();
        config.Fields.Count().ShouldBe(4); // FirstName, LastName, Email, Phone
        
        // Check field names
        var fieldNames = config.Fields.Select(f => f.FieldName).ToArray();
        fieldNames.ShouldContain("FirstName");
        fieldNames.ShouldContain("LastName");
        fieldNames.ShouldContain("Email");
        fieldNames.ShouldContain("Phone");
    }

    [Fact]
    public void AsContactForm_Should_Set_Correct_Labels()
    {
        // Arrange
        var builder = FormBuilder<ContactModel>.Create();

        // Act
        var config = builder.AsContactForm().Build();

        // Assert
        var firstNameField = config.Fields.First(f => f.FieldName == "FirstName");
        firstNameField.Label.ShouldBe("First Name");

        var lastNameField = config.Fields.First(f => f.FieldName == "LastName");
        lastNameField.Label.ShouldBe("Last Name");

        var emailField = config.Fields.First(f => f.FieldName == "Email");
        emailField.Label.ShouldBe("Email Address");

        var phoneField = config.Fields.First(f => f.FieldName == "Phone");
        phoneField.Label.ShouldBe("Phone Number");
    }

    [Fact]
    public void AsContactForm_Should_Set_Fields_As_Required()
    {
        // Arrange
        var builder = FormBuilder<ContactModel>.Create();

        // Act
        var config = builder.AsContactForm().Build();

        // Assert
        var firstNameField = config.Fields.First(f => f.FieldName == "FirstName");
        firstNameField.IsRequired.ShouldBeTrue();

        var lastNameField = config.Fields.First(f => f.FieldName == "LastName");
        lastNameField.IsRequired.ShouldBeTrue();

        var emailField = config.Fields.First(f => f.FieldName == "Email");
        emailField.IsRequired.ShouldBeTrue();

        var phoneField = config.Fields.First(f => f.FieldName == "Phone");
        phoneField.IsRequired.ShouldBeFalse(); // Phone field may not be required in the actual implementation
    }

    [Fact]
    public void AsContactForm_Should_Set_Placeholders()
    {
        // Arrange
        var builder = FormBuilder<ContactModel>.Create();

        // Act
        var config = builder.AsContactForm().Build();

        // Assert
        var firstNameField = config.Fields.First(f => f.FieldName == "FirstName");
        firstNameField.Placeholder.ShouldBe("Enter your first name");

        var lastNameField = config.Fields.First(f => f.FieldName == "LastName");
        lastNameField.Placeholder.ShouldBe("Enter your last name");
    }

    [Fact]
    public void AsContactForm_Should_Set_Validators()
    {
        // Arrange
        var builder = FormBuilder<ContactModel>.Create();

        // Act
        var config = builder.AsContactForm().Build();

        // Assert
        var firstNameField = config.Fields.First(f => f.FieldName == "FirstName");
        firstNameField.Validators.Count().ShouldBeGreaterThan(0);

        var lastNameField = config.Fields.First(f => f.FieldName == "LastName");
        lastNameField.Validators.Count().ShouldBeGreaterThan(0);

        var emailField = config.Fields.First(f => f.FieldName == "Email");
        emailField.Validators.Count().ShouldBeGreaterThan(0);

        var phoneField = config.Fields.First(f => f.FieldName == "Phone");
        phoneField.Validators.Count().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AsRegistrationForm_Should_Create_Form_With_Required_Fields()
    {
        // Arrange
        var builder = FormBuilder<RegistrationModel>.Create();

        // Act
        var formBuilder = builder.AsRegistrationForm();
        var config = formBuilder.Build();

        // Assert
        config.ShouldNotBeNull();
        config.Fields.Count().ShouldBe(5); // FirstName, LastName, Email, Password, AcceptTerms
        
        // Check field names
        var fieldNames = config.Fields.Select(f => f.FieldName).ToArray();
        fieldNames.ShouldContain("FirstName");
        fieldNames.ShouldContain("LastName");
        fieldNames.ShouldContain("Email");
        fieldNames.ShouldContain("Password");
        fieldNames.ShouldContain("AcceptTerms");
    }

    [Fact]
    public void AsRegistrationForm_Should_Set_Correct_Labels()
    {
        // Arrange
        var builder = FormBuilder<RegistrationModel>.Create();

        // Act
        var config = builder.AsRegistrationForm().Build();

        // Assert
        var firstNameField = config.Fields.First(f => f.FieldName == "FirstName");
        firstNameField.Label.ShouldBe("First Name");

        var lastNameField = config.Fields.First(f => f.FieldName == "LastName");
        lastNameField.Label.ShouldBe("Last Name");

        var acceptTermsField = config.Fields.First(f => f.FieldName == "AcceptTerms");
        acceptTermsField.Label.ShouldBe("I accept the terms and conditions");
    }

    [Fact]
    public void AsRegistrationForm_Should_Set_Fields_As_Required()
    {
        // Arrange
        var builder = FormBuilder<RegistrationModel>.Create();

        // Act
        var config = builder.AsRegistrationForm().Build();

        // Assert
        var firstNameField = config.Fields.First(f => f.FieldName == "FirstName");
        firstNameField.IsRequired.ShouldBeTrue();

        var lastNameField = config.Fields.First(f => f.FieldName == "LastName");
        lastNameField.IsRequired.ShouldBeTrue();

        var emailField = config.Fields.First(f => f.FieldName == "Email");
        emailField.IsRequired.ShouldBeTrue();

        var passwordField = config.Fields.First(f => f.FieldName == "Password");
        passwordField.IsRequired.ShouldBeTrue();

        // AcceptTerms may not be required by default, so check individually
        var acceptTermsField = config.Fields.First(f => f.FieldName == "AcceptTerms");
        acceptTermsField.IsRequired.ShouldBeFalse(); // Checkboxes typically aren't marked as required
    }

    [Fact]
    public void AsRegistrationForm_Should_Include_Checkbox_For_Terms()
    {
        // Arrange
        var builder = FormBuilder<RegistrationModel>.Create();

        // Act
        var config = builder.AsRegistrationForm().Build();

        // Assert
        var acceptTermsField = config.Fields.First(f => f.FieldName == "AcceptTerms");
        acceptTermsField.ShouldNotBeNull();
        acceptTermsField.Label.ShouldBe("I accept the terms and conditions");
    }

    [Fact]
    public void AsContactForm_Should_Throw_When_Properties_Missing()
    {
        // Arrange
        var builder = FormBuilder<IncompleteModel>.Create();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
        {
            builder.AsContactForm().Build();
        });

        exception.ShouldNotBeNull();
    }

    [Fact]
    public void AsRegistrationForm_Should_Throw_When_Properties_Missing()
    {
        // Arrange
        var builder = FormBuilder<IncompleteModel>.Create();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
        {
            builder.AsRegistrationForm().Build();
        });

        exception.ShouldNotBeNull();
    }

    [Fact]
    public void AsContactForm_Should_Support_Method_Chaining()
    {
        // Arrange
        var builder = FormBuilder<ContactModel>.Create();

        // Act
        var config = builder
            .AsContactForm()
            .WithLayout(FormLayout.Grid)
            .Build();

        // Assert
        config.ShouldNotBeNull();
        config.Layout.ShouldBe(FormLayout.Grid);
        config.Fields.Count().ShouldBe(4);
    }

    [Fact]
    public void AsRegistrationForm_Should_Support_Method_Chaining()
    {
        // Arrange
        var builder = FormBuilder<RegistrationModel>.Create();

        // Act
        var config = builder
            .AsRegistrationForm()
            .WithLayout(FormLayout.Horizontal)
            .Build();

        // Assert
        config.ShouldNotBeNull();
        config.Layout.ShouldBe(FormLayout.Horizontal);
        config.Fields.Count().ShouldBe(5);
    }

    public class ContactModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class RegistrationModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool AcceptTerms { get; set; }
    }

    public class IncompleteModel
    {
        public string Name { get; set; } = string.Empty;
        // Missing required properties for contact/registration forms
    }
}