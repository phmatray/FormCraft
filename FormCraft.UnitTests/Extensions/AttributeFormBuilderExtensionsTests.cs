using System.ComponentModel.DataAnnotations;
using Shouldly;

namespace FormCraft.UnitTests.Extensions;

public class AttributeFormBuilderExtensionsTests
{
    private class TestModel
    {
        [Required]
        [TextField("First Name", "Enter first name")]
        [MinLength(2)]
        public string FirstName { get; set; } = string.Empty;
    }

    [Fact]
    public void AddFieldsFromAttributes_Should_Create_Field_From_Annotations()
    {
        var config = FormBuilder<TestModel>.Create()
            .AddFieldsFromAttributes()
            .Build();

        config.Fields.Count.ShouldBe(1);
        var wrapper = config.Fields[0] as FieldConfigurationWrapper<TestModel, string>;
        wrapper.ShouldNotBeNull();
        var field = wrapper!.TypedConfiguration;

        field.Label.ShouldBe("First Name");
        field.Placeholder.ShouldBe("Enter first name");
        field.IsRequired.ShouldBeTrue();
        field.Validators.ShouldContain(v => v is RequiredValidator<TestModel, string>);
        field.Validators.ShouldContain(v => v is CustomValidator<TestModel, string>);
    }
}
