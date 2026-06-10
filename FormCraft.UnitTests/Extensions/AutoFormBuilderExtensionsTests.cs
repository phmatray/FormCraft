using System.ComponentModel.DataAnnotations;

namespace FormCraft.UnitTests.Extensions;

public class AutoFormBuilderExtensionsTests
{
    public enum OrderStatus
    {
        Draft,
        InProgress,
        Completed
    }

    private class PlainModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Age { get; set; }
        public long ViewCount { get; set; }
        public short Floor { get; set; }
        public byte Level { get; set; }
        public decimal Price { get; set; }
        public double Rating { get; set; }
        public float Weight { get; set; }
        public bool IsActive { get; set; }
        public DateTime BirthDate { get; set; }
        public DateOnly StartDate { get; set; }
        public TimeOnly MeetingTime { get; set; }
        public OrderStatus Status { get; set; }
    }

    private class NullableModel
    {
        public int? Age { get; set; }
        public decimal? Price { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? BirthDate { get; set; }
        public OrderStatus? Status { get; set; }
        public string? Notes { get; set; }
    }

    private class SkippedMembersModel
    {
        public string Name { get; set; } = string.Empty;

        public string ReadOnlyValue => Name;

        public string WriteOnlyValue
        {
            set => Name = value;
        }

        public string this[int index]
        {
            get => Name;
            set => Name = value;
        }

        [ExcludeField]
        public string InternalReference { get; set; } = string.Empty;

        public ComplexChild Child { get; set; } = new();
        public List<ComplexChild> Children { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string[] Aliases { get; set; } = [];
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class ComplexChild
    {
        public string Description { get; set; } = string.Empty;
    }

    private class FileModel
    {
        public IBrowserFile? Resume { get; set; }
        public IReadOnlyList<IBrowserFile>? Attachments { get; set; }
    }

    private class AnnotatedModel
    {
        [Required(ErrorMessage = "Name please")]
        [MaxLength(50)]
        [MinLength(2)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Years of Experience")]
        [Range(0, 60)]
        public int Experience { get; set; }

        [EmailAddress]
        public string ContactAddress { get; set; } = string.Empty;

        [StringLength(20, MinimumLength = 5)]
        public string Code { get; set; } = string.Empty;
    }

    [Fact]
    public void AddFieldsAuto_Should_Return_Same_Builder_For_Chaining()
    {
        // Arrange
        var builder = FormBuilder<PlainModel>.Create();

        // Act
        var result = builder.AddFieldsAuto();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void AddFieldsAuto_Should_Generate_A_Field_For_Every_Supported_Property()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        config.Fields.Count.ShouldBe(15);
    }

    [Fact]
    public void AddFieldsAuto_Should_Map_String_To_Text_Input()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(PlainModel.FirstName));
        field.InputType.ShouldBe("text");
    }

    [Fact]
    public void AddFieldsAuto_Should_Map_Email_Named_Property_To_Email_Input_With_Validation()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(PlainModel.Email));
        field.InputType.ShouldBe("email");
        field.Validators.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddFieldsAuto_Should_Map_Password_Named_Property_To_Password_Input()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(PlainModel.Password));
        field.InputType.ShouldBe("password");
    }

    [Theory]
    [InlineData(nameof(PlainModel.Age))]
    [InlineData(nameof(PlainModel.ViewCount))]
    [InlineData(nameof(PlainModel.Floor))]
    [InlineData(nameof(PlainModel.Level))]
    [InlineData(nameof(PlainModel.Price))]
    [InlineData(nameof(PlainModel.Rating))]
    [InlineData(nameof(PlainModel.Weight))]
    public void AddFieldsAuto_Should_Map_Numeric_Types_To_Number_Input(string fieldName)
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == fieldName);
        field.InputType.ShouldBe("number");
    }

    [Fact]
    public void AddFieldsAuto_Should_Map_Bool_To_Checkbox_Without_Input_Type()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(PlainModel.IsActive));
        field.InputType.ShouldBeNull();
    }

    [Theory]
    [InlineData(nameof(PlainModel.BirthDate), "date")]
    [InlineData(nameof(PlainModel.StartDate), "date")]
    [InlineData(nameof(PlainModel.MeetingTime), "time")]
    public void AddFieldsAuto_Should_Map_Date_And_Time_Types(string fieldName, string expectedInputType)
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == fieldName);
        field.InputType.ShouldBe(expectedInputType);
    }

    [Fact]
    public void AddFieldsAuto_Should_Map_Enum_To_Select_With_All_Values_And_Humanized_Labels()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(PlainModel.Status));
        field.AdditionalAttributes.ShouldContainKey("Options");

        var options = field.AdditionalAttributes["Options"]
            .ShouldBeAssignableTo<IEnumerable<SelectOption<OrderStatus>>>()!
            .ToList();
        options.Count.ShouldBe(3);
        options.Select(o => o.Value).ShouldBe([OrderStatus.Draft, OrderStatus.InProgress, OrderStatus.Completed]);
        options.Single(o => o.Value == OrderStatus.InProgress).Label.ShouldBe("In Progress");
    }

    [Fact]
    public void AddFieldsAuto_Should_Support_Nullable_Variants()
    {
        // Act
        var config = FormBuilder<NullableModel>.Create().AddFieldsAuto().Build();

        // Assert
        config.Fields.Count.ShouldBe(6);
        config.Fields.Single(f => f.FieldName == nameof(NullableModel.Age)).InputType.ShouldBe("number");
        config.Fields.Single(f => f.FieldName == nameof(NullableModel.Price)).InputType.ShouldBe("number");
        config.Fields.Single(f => f.FieldName == nameof(NullableModel.BirthDate)).InputType.ShouldBe("date");
        config.Fields.Single(f => f.FieldName == nameof(NullableModel.Notes)).InputType.ShouldBe("text");

        var statusField = config.Fields.Single(f => f.FieldName == nameof(NullableModel.Status));
        var options = statusField.AdditionalAttributes["Options"]
            .ShouldBeAssignableTo<IEnumerable<SelectOption<OrderStatus?>>>()!
            .ToList();
        options.Count.ShouldBe(3);
    }

    [Fact]
    public void AddFieldsAuto_Should_Humanize_PascalCase_Labels()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create().AddFieldsAuto().Build();

        // Assert
        config.Fields.Single(f => f.FieldName == nameof(PlainModel.FirstName)).Label.ShouldBe("First Name");
        config.Fields.Single(f => f.FieldName == nameof(PlainModel.IsActive)).Label.ShouldBe("Is Active");
        config.Fields.Single(f => f.FieldName == nameof(PlainModel.BirthDate)).Label.ShouldBe("Birth Date");
        config.Fields.Single(f => f.FieldName == nameof(PlainModel.Email)).Label.ShouldBe("Email");
    }

    private class HumanizeModel
    {
        public string SSNNumber { get; set; } = string.Empty;
        public string Address1 { get; set; } = string.Empty;
        public string A { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData(nameof(HumanizeModel.SSNNumber), "SSN Number")]
    [InlineData(nameof(HumanizeModel.Address1), "Address 1")]
    [InlineData(nameof(HumanizeModel.A), "A")]
    public void AddFieldsAuto_Should_Humanize_Acronyms_Digits_And_Single_Letters(string fieldName, string expected)
    {
        // Act
        var config = FormBuilder<HumanizeModel>.Create().AddFieldsAuto().Build();

        // Assert
        config.Fields.Single(f => f.FieldName == fieldName).Label.ShouldBe(expected);
    }

    [Fact]
    public void AddFieldsAuto_Should_Skip_Indexers_ReadOnly_WriteOnly_And_Excluded_Properties()
    {
        // Act
        var config = FormBuilder<SkippedMembersModel>.Create().AddFieldsAuto().Build();

        // Assert
        config.Fields.Count.ShouldBe(1);
        config.Fields.Single().FieldName.ShouldBe(nameof(SkippedMembersModel.Name));
    }

    [Fact]
    public void AddFieldsAuto_Should_Skip_Complex_Objects_And_Collections_Of_Complex_Types()
    {
        // Act
        var config = FormBuilder<SkippedMembersModel>.Create().AddFieldsAuto().Build();

        // Assert
        config.Fields.ShouldNotContain(f => f.FieldName == nameof(SkippedMembersModel.Child));
        config.Fields.ShouldNotContain(f => f.FieldName == nameof(SkippedMembersModel.Children));
        config.Fields.ShouldNotContain(f => f.FieldName == nameof(SkippedMembersModel.Tags));
        config.Fields.ShouldNotContain(f => f.FieldName == nameof(SkippedMembersModel.Aliases));
        config.Fields.ShouldNotContain(f => f.FieldName == nameof(SkippedMembersModel.Metadata));
    }

    [Fact]
    public void AddFieldsAuto_Should_Map_Browser_File_Properties_To_File_Uploads()
    {
        // Act
        var config = FormBuilder<FileModel>.Create().AddFieldsAuto().Build();

        // Assert
        config.Fields.Count.ShouldBe(2);

        var resume = config.Fields.Single(f => f.FieldName == nameof(FileModel.Resume));
        resume.AdditionalAttributes.ShouldContainKey("FileUploadConfiguration");

        var attachments = config.Fields.Single(f => f.FieldName == nameof(FileModel.Attachments));
        attachments.AdditionalAttributes.ShouldContainKey("FileUploadConfiguration");
        var uploadConfig = attachments.AdditionalAttributes["FileUploadConfiguration"]
            .ShouldBeOfType<FileUploadConfiguration>();
        uploadConfig.MaxFiles.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void AddFieldsAuto_Should_Honor_Required_Annotation()
    {
        // Act
        var config = FormBuilder<AnnotatedModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(AnnotatedModel.FullName));
        field.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void AddFieldsAuto_Should_Honor_Display_Name_Annotation()
    {
        // Act
        var config = FormBuilder<AnnotatedModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(AnnotatedModel.Experience));
        field.Label.ShouldBe("Years of Experience");
    }

    [Fact]
    public void AddFieldsAuto_Should_Honor_Range_Annotation()
    {
        // Act
        var config = FormBuilder<AnnotatedModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(AnnotatedModel.Experience));
        field.AdditionalAttributes["min"].ShouldBe(0);
        field.AdditionalAttributes["max"].ShouldBe(60);
        field.Validators.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddFieldsAuto_Should_Honor_Length_Annotations()
    {
        // Act
        var config = FormBuilder<AnnotatedModel>.Create().AddFieldsAuto().Build();

        // Assert
        var fullName = config.Fields.Single(f => f.FieldName == nameof(AnnotatedModel.FullName));
        // Required + MinLength + MaxLength
        fullName.Validators.Count.ShouldBeGreaterThanOrEqualTo(3);

        var code = config.Fields.Single(f => f.FieldName == nameof(AnnotatedModel.Code));
        // StringLength produces both a min and a max validator
        code.Validators.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void AddFieldsAuto_Should_Honor_EmailAddress_Annotation()
    {
        // Act
        var config = FormBuilder<AnnotatedModel>.Create().AddFieldsAuto().Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(AnnotatedModel.ContactAddress));
        field.InputType.ShouldBe("email");
        field.Validators.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddFieldsAuto_Should_Exclude_Properties_Via_Options()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create()
            .AddFieldsAuto(options => options
                .Exclude(x => x.Password)
                .Exclude(nameof(PlainModel.Rating)))
            .Build();

        // Assert
        config.Fields.ShouldNotContain(f => f.FieldName == nameof(PlainModel.Password));
        config.Fields.ShouldNotContain(f => f.FieldName == nameof(PlainModel.Rating));
        config.Fields.Count.ShouldBe(13);
    }

    [Fact]
    public void AddFieldsAuto_Should_Only_Generate_Included_Properties_When_Include_List_Is_Set()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create()
            .AddFieldsAuto(options => options
                .Include(x => x.FirstName)
                .Include(nameof(PlainModel.Email)))
            .Build();

        // Assert
        config.Fields.Count.ShouldBe(2);
        config.Fields.Select(f => f.FieldName)
            .ShouldBe([nameof(PlainModel.FirstName), nameof(PlainModel.Email)], ignoreOrder: true);
    }

    [Fact]
    public void AddFieldsAuto_Should_Apply_Per_Field_Configure_Callback_After_Defaults()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create()
            .AddFieldsAuto(options => options
                .ConfigureField(x => x.FirstName, field => field
                    .WithLabel("Given Name")
                    .Required()))
            .Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(PlainModel.FirstName));
        field.Label.ShouldBe("Given Name");
        field.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void AddFieldsAuto_Should_Apply_Name_Based_Configure_Callback()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create()
            .AddFieldsAuto(options => options
                .ConfigureField<int>(nameof(PlainModel.Age), field => field.WithHelpText("In years")))
            .Build();

        // Assert
        var field = config.Fields.Single(f => f.FieldName == nameof(PlainModel.Age));
        field.HelpText.ShouldBe("In years");
    }

    [Fact]
    public void AddFieldsAuto_Should_Compose_With_Manually_Added_Fields()
    {
        // Act
        var config = FormBuilder<PlainModel>.Create()
            .AddField(x => x.FirstName, field => field.WithLabel("Manual"))
            .AddFieldsAuto(options => options.Include(x => x.Email))
            .Build();

        // Assert
        config.Fields.Count.ShouldBe(2);
        config.Fields.First().Label.ShouldBe("Manual");
    }
}
