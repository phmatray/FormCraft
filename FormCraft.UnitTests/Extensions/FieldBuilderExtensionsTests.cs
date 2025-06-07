namespace FormCraft.UnitTests.Extensions;

public class FieldBuilderExtensionsTests
{
    [Fact]
    public void AsTextArea_Should_Set_Lines_Attribute()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Description);

        // Act
        var result = fieldBuilder.AsTextArea(5);
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Description");
        field.AdditionalAttributes.ShouldContainKey("Lines");
        field.AdditionalAttributes["Lines"].ShouldBe(5);
    }

    [Fact]
    public void AsTextArea_Should_Set_MaxLength_When_Provided()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Description);

        // Act
        var result = fieldBuilder.AsTextArea(lines: 3, maxLength: 500);
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Description");
        field.AdditionalAttributes.ShouldContainKey("MaxLength");
        field.AdditionalAttributes["MaxLength"].ShouldBe(500);
    }

    [Fact]
    public void AsTextArea_Should_Not_Set_MaxLength_When_Null()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Description);

        // Act
        var result = fieldBuilder.AsTextArea();
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Description");
        field.AdditionalAttributes.ShouldNotContainKey("MaxLength");
    }

    [Fact]
    public void WithOptions_Should_Set_Options_Attribute()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Status);

        // Act
        var result = fieldBuilder.WithOptions(
            ("active", "Active"),
            ("inactive", "Inactive"),
            ("pending", "Pending"));
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Status");
        field.AdditionalAttributes.ShouldContainKey("Options");
        
        var options = field.AdditionalAttributes["Options"] as IEnumerable<SelectOption<string>>;
        options.ShouldNotBeNull();
        options.Count().ShouldBe(3);
        options.ShouldContain(o => o.Value == "active" && o.Label == "Active");
        options.ShouldContain(o => o.Value == "inactive" && o.Label == "Inactive");
        options.ShouldContain(o => o.Value == "pending" && o.Label == "Pending");
    }

    [Fact]
    public void AsMultiSelect_Should_Set_MultiSelectOptions_Attribute()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Categories);

        // Act
        var result = fieldBuilder.AsMultiSelect(
            ("tech", "Technology"),
            ("health", "Healthcare"));
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Categories");
        field.AdditionalAttributes.ShouldContainKey("MultiSelectOptions");
        
        var options = field.AdditionalAttributes["MultiSelectOptions"] as IEnumerable<SelectOption<string>>;
        options.ShouldNotBeNull();
        options.Count().ShouldBe(2);
    }

    [Fact]
    public void AsFileUpload_Should_Set_File_Attributes()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.ProfileImage);

        // Act
        var result = fieldBuilder.AsFileUpload(5 * 1024 * 1024, ".jpg,.png");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "ProfileImage");
        field.AdditionalAttributes.ShouldContainKey("MaxFileSize");
        field.AdditionalAttributes.ShouldContainKey("AcceptedFileTypes");
        field.AdditionalAttributes["MaxFileSize"].ShouldBe(5 * 1024 * 1024);
        field.AdditionalAttributes["AcceptedFileTypes"].ShouldBe(".jpg,.png");
    }

    [Fact]
    public void AsFileUpload_Should_Use_Default_Values()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.ProfileImage);

        // Act
        var result = fieldBuilder.AsFileUpload();
        var config = result.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "ProfileImage");
        field.AdditionalAttributes["MaxFileSize"].ShouldBe(10 * 1024 * 1024);
        field.AdditionalAttributes["AcceptedFileTypes"].ShouldBe(".jpg,.jpeg,.png,.pdf");
    }

    [Fact]
    public void AsSlider_Should_Set_Slider_Attributes()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Rating);

        // Act
        var result = fieldBuilder.AsSlider(0, 10, 1, showValue: false);
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
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
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Email);
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = fieldBuilder.WithEmailValidation("Invalid email");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
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
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Email);
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = fieldBuilder.WithEmailValidation();
        var config = result.Build();

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
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Username);
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = fieldBuilder.WithMinLength(3, "Too short");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
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
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Username);
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = fieldBuilder.WithMinLength(3);
        var config = result.Build();

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
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Bio);
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = fieldBuilder.WithMaxLength(10, "Too long");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
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
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Age);
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = fieldBuilder.WithRange(18, 65, "Age out of range");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(fieldBuilder);
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
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Age);
        var services = A.Fake<IServiceProvider>();

        // Act
        var result = fieldBuilder.WithRange(1, 10);
        var config = result.Build();

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