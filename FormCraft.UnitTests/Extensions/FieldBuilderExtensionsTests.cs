namespace FormCraft.UnitTests.Extensions;

public class FieldBuilderExtensionsTests
{
    [Fact]
    public void AsTextArea_Should_Set_Lines_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Description, field => field
                .AsTextArea(5))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Description");
        field.AdditionalAttributes.ShouldContainKey("Lines");
        field.AdditionalAttributes["Lines"].ShouldBe(5);
    }

    [Fact]
    public void AsTextArea_Should_Set_MaxLength_When_Provided()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Description, field => field
                .AsTextArea(lines: 3, maxLength: 500))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Description");
        field.AdditionalAttributes.ShouldContainKey("MaxLength");
        field.AdditionalAttributes["MaxLength"].ShouldBe(500);
    }

    [Fact]
    public void AsTextArea_Should_Not_Set_MaxLength_When_Null()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Description, field => field
                .AsTextArea())
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Description");
        field.AdditionalAttributes.ShouldNotContainKey("MaxLength");
    }

    [Fact]
    public void WithOptions_Should_Set_Options_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Status, field => field
                .WithOptions(
                    ("active", "Active"),
                    ("inactive", "Inactive"),
                    ("pending", "Pending")))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Status");
        field.AdditionalAttributes.ShouldContainKey("Options");

        var options = (field.AdditionalAttributes["Options"] as IEnumerable<SelectOption<string>>)?.ToList();
        options.ShouldNotBeNull();
        options.Count.ShouldBe(3);
        options.ShouldContain(o => o.Value == "active" && o.Label == "Active");
        options.ShouldContain(o => o.Value == "inactive" && o.Label == "Inactive");
        options.ShouldContain(o => o.Value == "pending" && o.Label == "Pending");
    }

    [Fact]
    public void WithOptions_Should_Set_Int_Options_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Rating, field => field
                .WithOptions(
                    (1, "Low"),
                    (2, "Medium"),
                    (3, "High")))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Rating");
        field.AdditionalAttributes.ShouldContainKey("Options");

        var options = (field.AdditionalAttributes["Options"] as IEnumerable<SelectOption<int>>)?.ToList();
        options.ShouldNotBeNull();
        options.Count.ShouldBe(3);
        options.ShouldContain(o => o.Value == 1 && o.Label == "Low");
        options.ShouldContain(o => o.Value == 2 && o.Label == "Medium");
        options.ShouldContain(o => o.Value == 3 && o.Label == "High");
    }

    [Fact]
    public void WithOptions_Should_Preserve_Int_Value_Types()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Age, field => field
                .WithOptions(
                    (18, "Eighteen"),
                    (25, "Twenty-Five"),
                    (65, "Sixty-Five")))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Age");
        field.AdditionalAttributes.ShouldContainKey("Options");

        var options = (field.AdditionalAttributes["Options"] as IEnumerable<SelectOption<int>>)?.ToList();
        options.ShouldNotBeNull();
        options.Count.ShouldBe(3);

        // Verify the values are actual ints, not strings
        options[0].Value.ShouldBeOfType<int>();
        options[0].Value.ShouldBe(18);
    }

    [Fact]
    public void AsMultiSelect_Should_Set_MultiSelectOptions_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Categories, field => field
                .AsMultiSelect(
                    ("tech", "Technology"),
                    ("health", "Healthcare")))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Categories");
        field.AdditionalAttributes.ShouldContainKey("MultiSelectOptions");

        var options = (field.AdditionalAttributes["MultiSelectOptions"] as IEnumerable<SelectOption<string>>)?.ToList();
        options.ShouldNotBeNull();
        options.Count.ShouldBe(2);
    }


    [Fact]
    public void AsSlider_Should_Set_Slider_Attributes()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Rating, field => field
                .AsSlider(0, 10, 1, showValue: false))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Rating");
        field.AdditionalAttributes.ShouldContainKey("UseSlider");
        field.AdditionalAttributes.ShouldContainKey("Min");
        field.AdditionalAttributes.ShouldContainKey("Max");
        field.AdditionalAttributes.ShouldContainKey("Step");
        field.AdditionalAttributes.ShouldContainKey("ShowValue");
        field.AdditionalAttributes["UseSlider"].ShouldBe(true);
        field.AdditionalAttributes["Min"].ShouldBe(0);
        field.AdditionalAttributes["Max"].ShouldBe(10);
        field.AdditionalAttributes["Step"].ShouldBe(1);
        field.AdditionalAttributes["ShowValue"].ShouldBe(false);
    }

    [Fact]
    public async Task WithEmailValidation_Should_Add_Email_Validator()
    {
        // Arrange
        var services = A.Fake<IServiceProvider>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Email, field => field
                .WithEmailValidation("Invalid email"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Email");
        field.Validators.Count.ShouldBe(1);

        // Test validation behavior
        var validator = field.Validators.First();
        var model = new TestModel();

        var validResult = await validator.ValidateAsync(model, "test@example.com", services);
        validResult.IsValid.ShouldBeTrue();

        var invalidResult = await validator.ValidateAsync(model, "invalid-email", services);
        invalidResult.IsValid.ShouldBeFalse();
        invalidResult.ErrorMessage.ShouldBe("Invalid email");
    }

    [Fact]
    public async Task WithEmailValidation_Should_Use_Default_Message()
    {
        // Arrange
        var services = A.Fake<IServiceProvider>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Email, field => field
                .WithEmailValidation())
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Email");
        var validator = field.Validators.First();
        var model = new TestModel();

        var invalidResult = await validator.ValidateAsync(model, "invalid", services);
        invalidResult.ErrorMessage.ShouldBe("Please enter a valid email address");
    }

    [Fact]
    public async Task WithMinLength_Should_Add_MinLength_Validator()
    {
        // Arrange
        var services = A.Fake<IServiceProvider>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Username, field => field
                .WithMinLength(3, "Too short"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Username");
        field.Validators.Count.ShouldBe(1);

        var validator = field.Validators.First();
        var model = new TestModel();

        var validResult = await validator.ValidateAsync(model, "test", services);
        validResult.IsValid.ShouldBeTrue();

        var invalidResult = await validator.ValidateAsync(model, "ab", services);
        invalidResult.IsValid.ShouldBeFalse();
        invalidResult.ErrorMessage.ShouldBe("Too short");
    }

    [Fact]
    public async Task WithMinLength_Should_Allow_Null_Or_Empty()
    {
        // Arrange
        var services = A.Fake<IServiceProvider>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Username, field => field
                .WithMinLength(3))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Username");
        var validator = field.Validators.First();
        var model = new TestModel();

        var nullResult = await validator.ValidateAsync(model, null!, services);
        nullResult.IsValid.ShouldBeTrue();

        var emptyResult = await validator.ValidateAsync(model, "", services);
        emptyResult.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task WithMaxLength_Should_Add_MaxLength_Validator()
    {
        // Arrange
        var services = A.Fake<IServiceProvider>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Bio, field => field
                .WithMaxLength(10, "Too long"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Bio");
        field.Validators.Count.ShouldBe(1);

        var validator = field.Validators.First();
        var model = new TestModel();

        var validResult = await validator.ValidateAsync(model, "short", services);
        validResult.IsValid.ShouldBeTrue();

        var invalidResult = await validator.ValidateAsync(model, "this is way too long", services);
        invalidResult.IsValid.ShouldBeFalse();
        invalidResult.ErrorMessage.ShouldBe("Too long");
    }

    [Fact]
    public async Task WithRange_Should_Add_Range_Validator()
    {
        // Arrange
        var services = A.Fake<IServiceProvider>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Age, field => field
                .WithRange(18, 65, "Age out of range"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Age");
        field.Validators.Count.ShouldBe(1);

        var validator = field.Validators.First();
        var model = new TestModel();

        var validResult = await validator.ValidateAsync(model, 25, services);
        validResult.IsValid.ShouldBeTrue();

        var tooYoungResult = await validator.ValidateAsync(model, 17, services);
        tooYoungResult.IsValid.ShouldBeFalse();
        tooYoungResult.ErrorMessage.ShouldBe("Age out of range");

        var tooOldResult = await validator.ValidateAsync(model, 66, services);
        tooOldResult.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task WithRange_Should_Use_Default_Message()
    {
        // Arrange
        var services = A.Fake<IServiceProvider>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Age, field => field
                .WithRange(1, 10))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Age");
        var validator = field.Validators.First();
        var model = new TestModel();

        var invalidResult = await validator.ValidateAsync(model, 11, services);
        invalidResult.ErrorMessage.ShouldBe("Must be between 1 and 10");
    }

    public class TestModel
    {
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public IEnumerable<string> Categories { get; set; } = Array.Empty<string>();
        public IBrowserFile? ProfileImage { get; set; }
        public int Rating { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}