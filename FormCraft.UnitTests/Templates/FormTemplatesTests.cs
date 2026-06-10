namespace FormCraft.UnitTests.Templates;

public class FormTemplatesTests
{
    #region ContactForm

    [Fact]
    public void ContactForm_Should_Add_Fields_For_Matching_Properties()
    {
        // Act
        var config = FormTemplates.ContactForm<ContactModel>();

        // Assert
        config.ShouldNotBeNull();
        config.Fields.Count.ShouldBe(5);
        config.Fields.ShouldContain(f => f.FieldName == "FirstName");
        config.Fields.ShouldContain(f => f.FieldName == "LastName");
        config.Fields.ShouldContain(f => f.FieldName == "Email");
        config.Fields.ShouldContain(f => f.FieldName == "Phone");
        config.Fields.ShouldContain(f => f.FieldName == "Message");
    }

    [Fact]
    public void ContactForm_Should_Use_Single_Name_Field_When_No_FirstName_LastName()
    {
        // Act
        var config = FormTemplates.ContactForm<SimpleContactModel>();

        // Assert
        config.Fields.Count.ShouldBe(2);
        config.Fields.ShouldContain(f => f.FieldName == "Name");
        config.Fields.ShouldContain(f => f.FieldName == "Email");
    }

    [Fact]
    public void ContactForm_Should_Configure_Required_Email_Field()
    {
        // Act
        var config = FormTemplates.ContactForm<ContactModel>();

        // Assert
        var emailField = config.Fields.First(f => f.FieldName == "Email");
        emailField.Label.ShouldBe("Email Address");
        emailField.IsRequired.ShouldBeTrue();
        emailField.Validators.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ContactForm_Should_Throw_When_No_Matching_Properties()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => FormTemplates.ContactForm<UnrelatedModel>());

        exception.Message.ShouldContain("ContactForm");
        exception.Message.ShouldContain("UnrelatedModel");
        exception.Message.ShouldContain("Email");
    }

    #endregion

    #region RegistrationForm

    [Fact]
    public void RegistrationForm_Should_Add_Fields_For_Matching_Properties()
    {
        // Act
        var config = FormTemplates.RegistrationForm<RegistrationModel>();

        // Assert
        config.Fields.Count.ShouldBe(6);
        config.Fields.ShouldContain(f => f.FieldName == "FirstName");
        config.Fields.ShouldContain(f => f.FieldName == "LastName");
        config.Fields.ShouldContain(f => f.FieldName == "Email");
        config.Fields.ShouldContain(f => f.FieldName == "Password");
        config.Fields.ShouldContain(f => f.FieldName == "ConfirmPassword");
        config.Fields.ShouldContain(f => f.FieldName == "AcceptTerms");
    }

    [Fact]
    public void RegistrationForm_Should_Configure_Password_Fields_As_Password_Inputs()
    {
        // Act
        var config = FormTemplates.RegistrationForm<RegistrationModel>();

        // Assert
        var passwordField = config.Fields.First(f => f.FieldName == "Password");
        passwordField.InputType.ShouldBe("password");
        passwordField.IsRequired.ShouldBeTrue();

        var confirmPasswordField = config.Fields.First(f => f.FieldName == "ConfirmPassword");
        confirmPasswordField.InputType.ShouldBe("password");
        confirmPasswordField.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void RegistrationForm_Should_Skip_Properties_Missing_From_Model()
    {
        // Act - model only has Email and Password
        var config = FormTemplates.RegistrationForm<MinimalRegistrationModel>();

        // Assert
        config.Fields.Count.ShouldBe(2);
        config.Fields.ShouldContain(f => f.FieldName == "Email");
        config.Fields.ShouldContain(f => f.FieldName == "Password");
    }

    [Fact]
    public void RegistrationForm_Should_Throw_When_No_Matching_Properties()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => FormTemplates.RegistrationForm<UnrelatedModel>());

        exception.Message.ShouldContain("RegistrationForm");
        exception.Message.ShouldContain("Password");
    }

    #endregion

    #region LoginForm

    [Fact]
    public void LoginForm_Should_Add_Email_Password_And_RememberMe_Fields()
    {
        // Act
        var config = FormTemplates.LoginForm<EmailLoginModel>();

        // Assert
        config.Fields.Count.ShouldBe(3);
        config.Fields.ShouldContain(f => f.FieldName == "Email");
        config.Fields.ShouldContain(f => f.FieldName == "Password");
        config.Fields.ShouldContain(f => f.FieldName == "RememberMe");
    }

    [Fact]
    public void LoginForm_Should_Use_Username_When_Email_Is_Missing()
    {
        // Act
        var config = FormTemplates.LoginForm<UsernameLoginModel>();

        // Assert
        config.Fields.Count.ShouldBe(2);
        config.Fields.ShouldContain(f => f.FieldName == "Username");
        config.Fields.ShouldContain(f => f.FieldName == "Password");
    }

    [Fact]
    public void LoginForm_Should_Configure_Password_As_Required_Password_Input()
    {
        // Act
        var config = FormTemplates.LoginForm<EmailLoginModel>();

        // Assert
        var passwordField = config.Fields.First(f => f.FieldName == "Password");
        passwordField.Label.ShouldBe("Password");
        passwordField.InputType.ShouldBe("password");
        passwordField.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void LoginForm_Should_Throw_When_No_Matching_Properties()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => FormTemplates.LoginForm<UnrelatedModel>());

        exception.Message.ShouldContain("LoginForm");
        exception.Message.ShouldContain("Username");
    }

    #endregion

    #region AddressForm

    [Fact]
    public void AddressForm_Should_Add_Fields_For_Matching_Properties()
    {
        // Act
        var config = FormTemplates.AddressForm<AddressModel>();

        // Assert
        config.Fields.Count.ShouldBe(5);
        config.Fields.ShouldContain(f => f.FieldName == "Street");
        config.Fields.ShouldContain(f => f.FieldName == "City");
        config.Fields.ShouldContain(f => f.FieldName == "State");
        config.Fields.ShouldContain(f => f.FieldName == "PostalCode");
        config.Fields.ShouldContain(f => f.FieldName == "Country");
    }

    [Fact]
    public void AddressForm_Should_Support_Alternative_Property_Names()
    {
        // Act - model uses AddressLine1/AddressLine2/ZipCode naming
        var config = FormTemplates.AddressForm<AlternativeAddressModel>();

        // Assert
        config.Fields.Count.ShouldBe(4);
        config.Fields.ShouldContain(f => f.FieldName == "AddressLine1");
        config.Fields.ShouldContain(f => f.FieldName == "AddressLine2");
        config.Fields.ShouldContain(f => f.FieldName == "City");
        config.Fields.ShouldContain(f => f.FieldName == "ZipCode");
    }

    [Fact]
    public void AddressForm_Should_Throw_When_No_Matching_Properties()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => FormTemplates.AddressForm<UnrelatedModel>());

        exception.Message.ShouldContain("AddressForm");
        exception.Message.ShouldContain("City");
    }

    #endregion

    [Fact]
    public void All_Templates_Should_Have_Default_Layout()
    {
        // Act
        var contactConfig = FormTemplates.ContactForm<ContactModel>();
        var registrationConfig = FormTemplates.RegistrationForm<RegistrationModel>();
        var loginConfig = FormTemplates.LoginForm<EmailLoginModel>();
        var addressConfig = FormTemplates.AddressForm<AddressModel>();

        // Assert
        contactConfig.Layout.ShouldBe(FormLayout.Vertical); // Default layout
        registrationConfig.Layout.ShouldBe(FormLayout.Vertical);
        loginConfig.Layout.ShouldBe(FormLayout.Vertical);
        addressConfig.Layout.ShouldBe(FormLayout.Vertical);
    }

    #region Test Models

    public class ContactModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class SimpleContactModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class RegistrationModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool AcceptTerms { get; set; }
    }

    public class MinimalRegistrationModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class EmailLoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class UsernameLoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AddressModel
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class AlternativeAddressModel
    {
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    public class UnrelatedModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}
