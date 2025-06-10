namespace FormCraft.UnitTests.Extensions;

public class FluentFormBuilderExtensionsTests
{
    [Fact]
    public void AddRequiredTextField_Should_Configure_Text_Field_With_Required_Validation()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddRequiredTextField(x => x.FirstName, "First Name", "Enter first name");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        var field = config.Fields.First(f => f.FieldName == "FirstName");
        field.Label.ShouldBe("First Name");
        field.Placeholder.ShouldBe("Enter first name");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(1); // Required validator
    }

    [Fact]
    public void AddRequiredTextField_Should_Add_MinLength_Validator_When_MinLength_Greater_Than_1()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddRequiredTextField(x => x.FirstName, "First Name", minLength: 3);
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "FirstName");
        field.Validators.Count.ShouldBe(2); // Required + MinLength
    }

    [Fact]
    public void AddRequiredTextField_Should_Add_MaxLength_Validator_When_MaxLength_Less_Than_255()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddRequiredTextField(x => x.FirstName, "First Name", maxLength: 50);
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "FirstName");
        field.Validators.Count.ShouldBe(2); // Required + MaxLength
    }

    [Fact]
    public void AddRequiredTextField_Should_Not_Set_Placeholder_When_Null()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddRequiredTextField(x => x.FirstName, "First Name");
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "FirstName");
        field.Placeholder.ShouldBeNull();
    }

    [Fact]
    public void AddEmailField_Should_Configure_Email_Field_With_Validation()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddEmailField(x => x.Email);
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        var field = config.Fields.First(f => f.FieldName == "Email");
        field.Label.ShouldBe("Email Address");
        field.Placeholder.ShouldBe("your.email@example.com");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(2); // Required + Email validation
    }

    [Fact]
    public void AddEmailField_Should_Use_Custom_Label_And_Placeholder()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddEmailField(x => x.Email, "Contact Email", "contact@company.com");
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Email");
        field.Label.ShouldBe("Contact Email");
        field.Placeholder.ShouldBe("contact@company.com");
    }

    [Fact]
    public void AddNumericField_Should_Configure_Numeric_Field_With_Required_Validation()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddNumericField(x => x.Age, "Age");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        var field = config.Fields.First(f => f.FieldName == "Age");
        field.Label.ShouldBe("Age");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(1); // Required validator only (no range when min/max are defaults)
    }

    [Fact]
    public void AddNumericField_Should_Add_Range_Validator_When_Min_Or_Max_Specified()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddNumericField(x => x.Age, "Age", min: 18, max: 65);
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Age");
        field.Validators.Count.ShouldBe(2); // Required + Range
    }

    [Fact]
    public void AddNumericField_Should_Not_Require_When_Required_False()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddNumericField(x => x.Age, "Age", required: false);
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Age");
        field.IsRequired.ShouldBeFalse();
        field.Validators.ShouldBeEmpty();
    }

    [Fact]
    public void AddDropdownField_Should_Configure_Dropdown_With_Options()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddDropdownField(x => x.Country, "Country",
            ("US", "United States"),
            ("CA", "Canada"),
            ("UK", "United Kingdom"));
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        var field = config.Fields.First(f => f.FieldName == "Country");
        field.Label.ShouldBe("Country");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(1); // Required validator
        field.AdditionalAttributes.ShouldContainKey("Options");

        var options = (field.AdditionalAttributes["Options"] as IEnumerable<SelectOption<string>>)?.ToList();
        options.ShouldNotBeNull();
        options.Count.ShouldBe(3);
        options.ShouldContain(o => o.Value == "US" && o.Label == "United States");
    }

    [Fact]
    public void AddPhoneField_Should_Configure_Phone_Field_With_Validation()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddPhoneField(x => x.PhoneNumber);
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        var field = config.Fields.First(f => f.FieldName == "PhoneNumber");
        field.Label.ShouldBe("Phone Number");
        field.Placeholder.ShouldBe("(555) 123-4567");
        field.IsRequired.ShouldBeFalse(); // Default is not required
        field.Validators.Count.ShouldBe(1); // Phone format validator
    }

    [Fact]
    public void AddPhoneField_Should_Add_Required_Validator_When_Required()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddPhoneField(x => x.PhoneNumber, required: true);
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "PhoneNumber");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(2); // Phone format + Required
    }

    [Fact]
    public void AddPasswordField_Should_Configure_Password_Field_With_MinLength()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddPasswordField(x => x.Password);
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        var field = config.Fields.First(f => f.FieldName == "Password");
        field.Label.ShouldBe("Password");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(3); // Required + MinLength + Special chars
    }

    [Fact]
    public void AddPasswordField_Should_Not_Require_Special_Chars_When_Disabled()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddPasswordField(x => x.Password, requireSpecialChars: false);
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Password");
        field.Validators.Count.ShouldBe(2); // Required + MinLength only
    }

    [Fact]
    public void AddPasswordField_Should_Use_Custom_MinLength()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddPasswordField(x => x.Password, minLength: 12);
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Password");
        field.Validators.Count.ShouldBe(3); // Required + MinLength + Special chars
    }

    [Fact]
    public void AddCheckboxField_Should_Configure_Checkbox_Field()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddCheckboxField(x => x.AcceptTerms, "I accept the terms");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        var field = config.Fields.First(f => f.FieldName == "AcceptTerms");
        field.Label.ShouldBe("I accept the terms");
        field.HelpText.ShouldBeNull();
    }

    [Fact]
    public void AddCheckboxField_Should_Set_HelpText_When_Provided()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.AddCheckboxField(x => x.AcceptTerms, "I accept", "Required to continue");
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "AcceptTerms");
        field.HelpText.ShouldBe("Required to continue");
    }


    public class TestModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Country { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool AcceptTerms { get; set; }
    }
}