using System.ComponentModel.DataAnnotations;

namespace FormCraft.UnitTests.Extensions;

public class AttributeFormBuilderExtensionsTests
{
    private class TestModel
    {
        [Required]
        [TextField("First Name", "Enter first name")]
        [MinLength(2)]
        public string FirstName { get; set; } = string.Empty;

        [EmailField("Email Address")]
        [Required]
        public string Email { get; set; } = string.Empty;

        [NumberField("Age", "Enter your age")]
        [Range(18, 120)]
        public int Age { get; set; }

        [DateField("Birth Date")]
        public DateTime BirthDate { get; set; }

        [CheckboxField("I agree to terms", "I agree to the terms and conditions")]
        public bool AgreeToTerms { get; set; }

        [TextArea("Comments", "Enter your comments")]
        [MaxLength(500)]
        public string Comments { get; set; } = string.Empty;

        [SelectField("Country", "Select your country")]
        public string Country { get; set; } = string.Empty;

        [NumberField("Price")]
        public decimal Price { get; set; }

        [DateField("Appointment Date")]
        public DateTime? AppointmentDate { get; set; }
    }

    private class EmailTestModel
    {
        [EmailField("Work Email", "work@example.com")]
        public string WorkEmail { get; set; } = string.Empty;
    }

    private class NumberTestModel
    {
        [NumberField("Quantity")]
        public int IntField { get; set; }

        [NumberField("Price")]
        public decimal DecimalField { get; set; }

        [NumberField("Rating")]
        public double DoubleField { get; set; }

        [NumberField("Temperature")]
        public float FloatField { get; set; }

        [NumberField("Large Number")]
        public long LongField { get; set; }
    }

    private class SelectTestModel
    {
        [SelectField("Status", "Draft", "Published", "Archived")]
        public string Status { get; set; } = string.Empty;

        [SelectField("Categories")]
        public string Category { get; set; } = string.Empty;
    }

    [Fact]
    public void AddFieldsFromAttributes_Should_Create_TextField_From_Annotations()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var firstNameField = config.Fields
            .OfType<FieldConfigurationWrapper<TestModel, string>>()
            .FirstOrDefault(f => f.TypedConfiguration.FieldName == "FirstName");

        firstNameField.ShouldNotBeNull();
        firstNameField.TypedConfiguration.Label.ShouldBe("First Name");
        firstNameField.TypedConfiguration.Placeholder.ShouldBe("Enter first name");
        firstNameField.TypedConfiguration.IsRequired.ShouldBeTrue();
        firstNameField.TypedConfiguration.InputType.ShouldBe("text");
        firstNameField.TypedConfiguration.Validators.ShouldContain(v => v is RequiredValidator<TestModel, string>);
    }

    [Fact]
    public void AddFieldsFromAttributes_Should_Create_EmailField_From_Annotations()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var emailField = config.Fields
            .OfType<FieldConfigurationWrapper<TestModel, string>>()
            .FirstOrDefault(f => f.TypedConfiguration.FieldName == "Email");

        emailField.ShouldNotBeNull();
        emailField.TypedConfiguration.Label.ShouldBe("Email Address");
        emailField.TypedConfiguration.Placeholder.ShouldBe("user@example.com");
        emailField.TypedConfiguration.InputType.ShouldBe("email");
        emailField.TypedConfiguration.IsRequired.ShouldBeTrue();

        // Should have email validation
        emailField.TypedConfiguration.Validators
            .OfType<CustomValidator<TestModel, string>>()
            .ShouldNotBeEmpty();
    }

    [Fact]
    public void AddFieldsFromAttributes_Should_Create_NumberField_From_Annotations()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var ageField = config.Fields
            .OfType<FieldConfigurationWrapper<TestModel, int>>()
            .FirstOrDefault(f => f.TypedConfiguration.FieldName == "Age");

        ageField.ShouldNotBeNull();
        ageField.TypedConfiguration.Label.ShouldBe("Age");
        ageField.TypedConfiguration.Placeholder.ShouldBe("Enter your age");
        ageField.TypedConfiguration.InputType.ShouldBe("number");
        ageField.TypedConfiguration.AdditionalAttributes.ShouldContainKey("min");
        ageField.TypedConfiguration.AdditionalAttributes["min"].ShouldBe(18);
        ageField.TypedConfiguration.AdditionalAttributes.ShouldContainKey("max");
        ageField.TypedConfiguration.AdditionalAttributes["max"].ShouldBe(120);
    }

    [Fact]
    public void AddFieldsFromAttributes_Should_Create_DateField_From_Annotations()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var birthDateField = config.Fields
            .OfType<FieldConfigurationWrapper<TestModel, DateTime>>()
            .FirstOrDefault(f => f.TypedConfiguration.FieldName == "BirthDate");

        birthDateField.ShouldNotBeNull();
        birthDateField.TypedConfiguration.Label.ShouldBe("Birth Date");
        birthDateField.TypedConfiguration.InputType.ShouldBe("date");
    }

    [Fact]
    public void AddFieldsFromAttributes_Should_Create_CheckboxField_From_Annotations()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var checkboxField = config.Fields
            .OfType<FieldConfigurationWrapper<TestModel, bool>>()
            .FirstOrDefault(f => f.TypedConfiguration.FieldName == "AgreeToTerms");

        checkboxField.ShouldNotBeNull();
        checkboxField.TypedConfiguration.Label.ShouldBe("I agree to terms");
        checkboxField.TypedConfiguration.AdditionalAttributes.ShouldContainKey("text");
        checkboxField.TypedConfiguration.AdditionalAttributes["text"].ShouldBe("I agree to the terms and conditions");
    }

    [Fact]
    public void AddFieldsFromAttributes_Should_Create_TextAreaField_From_Annotations()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var textAreaField = config.Fields
            .OfType<FieldConfigurationWrapper<TestModel, string>>()
            .FirstOrDefault(f => f.TypedConfiguration.FieldName == "Comments");

        textAreaField.ShouldNotBeNull();
        textAreaField.TypedConfiguration.Label.ShouldBe("Comments");
        textAreaField.TypedConfiguration.Placeholder.ShouldBe("Enter your comments");
        textAreaField.TypedConfiguration.AdditionalAttributes.ShouldContainKey("rows");
        textAreaField.TypedConfiguration.AdditionalAttributes["rows"].ShouldBe(4);
    }

    [Fact]
    public void AddFieldsFromAttributes_Should_Create_SelectField_From_Annotations()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var selectField = config.Fields
            .FirstOrDefault(f => f.FieldName == "Country");

        selectField.ShouldNotBeNull();
        selectField.Label.ShouldBe("Country");
        selectField.Placeholder.ShouldBe("Select your country");
    }

    [Fact]
    public void EmailField_Should_Use_Default_Placeholder_When_Not_Specified()
    {
        var config = FormBuilder<EmailTestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var emailField = config.Fields
            .OfType<FieldConfigurationWrapper<EmailTestModel, string>>()
            .FirstOrDefault();

        emailField.ShouldNotBeNull();
        emailField.TypedConfiguration.Placeholder.ShouldBe("work@example.com");
    }

    [Fact]
    public void NumberField_Should_Support_Multiple_Numeric_Types()
    {
        var config = FormBuilder<NumberTestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        config.Fields.Count.ShouldBe(5);

        var intField = config.Fields
            .OfType<FieldConfigurationWrapper<NumberTestModel, int>>()
            .FirstOrDefault();
        intField.ShouldNotBeNull();
        intField.TypedConfiguration.InputType.ShouldBe("number");

        var decimalField = config.Fields
            .OfType<FieldConfigurationWrapper<NumberTestModel, decimal>>()
            .FirstOrDefault();
        decimalField.ShouldNotBeNull();
        decimalField.TypedConfiguration.InputType.ShouldBe("number");

        var doubleField = config.Fields
            .OfType<FieldConfigurationWrapper<NumberTestModel, double>>()
            .FirstOrDefault();
        doubleField.ShouldNotBeNull();
        doubleField.TypedConfiguration.InputType.ShouldBe("number");

        var floatField = config.Fields
            .OfType<FieldConfigurationWrapper<NumberTestModel, float>>()
            .FirstOrDefault();
        floatField.ShouldNotBeNull();
        floatField.TypedConfiguration.InputType.ShouldBe("number");

        var longField = config.Fields
            .OfType<FieldConfigurationWrapper<NumberTestModel, long>>()
            .FirstOrDefault();
        longField.ShouldNotBeNull();
        longField.TypedConfiguration.InputType.ShouldBe("number");
    }

    [Fact]
    public void DateField_Should_Support_Nullable_DateTime()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var nullableDateField = config.Fields
            .OfType<FieldConfigurationWrapper<TestModel, DateTime?>>()
            .FirstOrDefault(f => f.TypedConfiguration.FieldName == "AppointmentDate");

        nullableDateField.ShouldNotBeNull();
        nullableDateField.TypedConfiguration.Label.ShouldBe("Appointment Date");
        nullableDateField.TypedConfiguration.InputType.ShouldBe("date");
    }

    [Fact]
    public void SelectField_Should_Store_Options_When_Provided()
    {
        var config = FormBuilder<SelectTestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var statusField = config.Fields
            .FirstOrDefault(f => f.FieldName == "Status");

        statusField.ShouldNotBeNull();

        // Check if options are stored in additional attributes
        var additionalAttrs = statusField.AdditionalAttributes;
        additionalAttrs.ShouldContainKey("options");

        var options = additionalAttrs["options"] as string[];
        options.ShouldNotBeNull();
        options.Length.ShouldBe(3);
        options.ShouldContain("Draft");
        options.ShouldContain("Published");
        options.ShouldContain("Archived");
    }

    [Fact]
    public void TextArea_With_MaxLength_Should_Apply_Validation()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        var textAreaField = config.Fields
            .OfType<FieldConfigurationWrapper<TestModel, string>>()
            .FirstOrDefault(f => f.TypedConfiguration.FieldName == "Comments");

        textAreaField.ShouldNotBeNull();

        // Should have max length validator
        textAreaField.TypedConfiguration.Validators
            .OfType<CustomValidator<TestModel, string>>()
            .ShouldNotBeEmpty();
    }

    [Fact]
    public void Multiple_Attributes_Should_All_Be_Processed()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        // Should have all fields from the model
        config.Fields.Count.ShouldBeGreaterThanOrEqualTo(9);

        // Verify each field type exists
        config.Fields.ShouldContain(f => f.FieldName == "FirstName");
        config.Fields.ShouldContain(f => f.FieldName == "Email");
        config.Fields.ShouldContain(f => f.FieldName == "Age");
        config.Fields.ShouldContain(f => f.FieldName == "BirthDate");
        config.Fields.ShouldContain(f => f.FieldName == "AgreeToTerms");
        config.Fields.ShouldContain(f => f.FieldName == "Comments");
        config.Fields.ShouldContain(f => f.FieldName == "Country");
        config.Fields.ShouldContain(f => f.FieldName == "Price");
        config.Fields.ShouldContain(f => f.FieldName == "AppointmentDate");
    }
}