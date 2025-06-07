namespace FormCraft.UnitTests.Builders;

public class FieldBuilderTests
{
    [Fact]
    public void WithLabel_Should_Set_Label_In_Built_Configuration()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.WithLabel("Full Name");
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Label.Should().Be("Full Name");
    }

    [Fact]
    public void WithPlaceholder_Should_Set_Placeholder_In_Built_Configuration()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.WithPlaceholder("Enter your name");
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Placeholder.Should().Be("Enter your name");
    }

    [Fact]
    public void WithHelpText_Should_Set_HelpText_In_Built_Configuration()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.WithHelpText("Please enter your full name");
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.HelpText.Should().Be("Please enter your full name");
    }

    [Fact]
    public void WithCssClass_Should_Set_CssClass_In_Built_Configuration()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.WithCssClass("custom-input");
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.CssClass.Should().Be("custom-input");
    }

    [Fact]
    public void Required_Should_Set_IsRequired_And_Add_Validator()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.Required("Name is required");
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsRequired.Should().BeTrue();
        field.Validators.Should().HaveCount(1);
        field.Validators.First().Should().BeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void Required_Without_Message_Should_Use_Default_Message()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        fieldBuilder.Required();
        var config = fieldBuilder.Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsRequired.Should().BeTrue();
        field.Validators.Should().HaveCount(1);
    }

    [Fact]
    public void Disabled_Should_Set_IsDisabled()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.Disabled(true);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public void ReadOnly_Should_Set_IsReadOnly()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.ReadOnly(true);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void VisibleWhen_Should_Set_VisibilityCondition()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);
        Func<TestModel, bool> condition = m => m.IsActive;

        // Act
        var result = fieldBuilder.VisibleWhen(condition);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.VisibilityCondition.Should().BeSameAs(condition);
    }

    [Fact]
    public void DisabledWhen_Should_Set_DisabledCondition()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);
        Func<TestModel, bool> condition = m => !m.IsActive;

        // Act
        var result = fieldBuilder.DisabledWhen(condition);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.DisabledCondition.Should().BeSameAs(condition);
    }

    [Fact]
    public void WithAttribute_Should_Add_Single_Attribute()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.WithAttribute("data-test", "value");
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.AdditionalAttributes.Should().ContainKey("data-test");
        field.AdditionalAttributes["data-test"].Should().Be("value");
    }

    [Fact]
    public void WithAttributes_Should_Add_Multiple_Attributes()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);
        var attributes = new Dictionary<string, object>
        {
            { "data-test", "value" },
            { "min", 10 },
            { "max", 100 }
        };

        // Act
        var result = fieldBuilder.WithAttributes(attributes);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.AdditionalAttributes.Should().ContainKeys(attributes.Keys);
        foreach (var attr in attributes)
        {
            field.AdditionalAttributes[attr.Key].Should().Be(attr.Value);
        }
    }

    [Fact]
    public void WithValidator_Should_Add_Validator()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);
        var validator = A.Fake<IFieldValidator<TestModel, string>>();

        // Act
        var result = fieldBuilder.WithValidator(validator);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Should().HaveCount(1);
        field.Validators.First().Should().BeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void WithValidator_Func_Should_Add_CustomValidator()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);
        Func<string, bool> validation = value => !string.IsNullOrEmpty(value);

        // Act
        var result = fieldBuilder.WithValidator(validation, "Value cannot be empty");
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Should().HaveCount(1);
        field.Validators.First().Should().BeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void WithAsyncValidator_Should_Add_AsyncValidator()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);
        Func<string, Task<bool>> validation = async value => 
        {
            await Task.Delay(1);
            return !string.IsNullOrEmpty(value);
        };

        // Act
        var result = fieldBuilder.WithAsyncValidator(validation, "Value cannot be empty");
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Should().HaveCount(1);
        field.Validators.First().Should().BeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void DependsOn_Should_Add_Field_Dependency()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.City);
        Action<TestModel, string> onChanged = (m, v) => m.City = string.Empty;

        // Act
        var result = fieldBuilder.DependsOn(x => x.Country, onChanged);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "City");
        field.Dependencies.Should().HaveCount(1);
        field.Dependencies.First().DependentFieldName.Should().Be("Country");
        
        config.FieldDependencies.Should().ContainKey("City");
        config.FieldDependencies["City"].Should().HaveCount(1);
        config.FieldDependencies["City"].First().DependentFieldName.Should().Be("Country");
    }

    [Fact]
    public void WithCustomTemplate_Should_Set_CustomTemplate()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);
        RenderFragment<IFieldContext<TestModel, string>> template = context => builder => 
        {
            builder.AddContent(0, "Custom content");
        };

        // Act
        var result = fieldBuilder.WithCustomTemplate(template);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        var wrapper = field as FieldConfigurationWrapper<TestModel, string>;
        wrapper.Should().NotBeNull();
        wrapper!.TypedConfiguration.CustomTemplate.Should().BeSameAs(template);
    }

    [Fact]
    public void WithOrder_Should_Set_Order()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder.WithOrder(5);
        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Order.Should().Be(5);
    }

    [Fact]
    public void AddField_Should_Return_New_FieldBuilder_And_Add_To_FormBuilder()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var newFieldBuilder = fieldBuilder.AddField(x => x.Email);
        var config = newFieldBuilder.Build();

        // Assert
        newFieldBuilder.Should().NotBeSameAs(fieldBuilder);
        newFieldBuilder.Should().BeOfType<FieldBuilder<TestModel, string>>();
        
        config.Fields.Should().HaveCount(2);
        config.Fields.Should().Contain(f => f.FieldName == "Name");
        config.Fields.Should().Contain(f => f.FieldName == "Email");
    }

    [Fact]
    public void Build_Should_Return_FormConfiguration()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name)
            .WithLabel("Name")
            .Required()
            .WithOrder(1);

        // Act
        var result = fieldBuilder.Build();

        // Assert
        result.Should().NotBeNull();
        result.Fields.Should().HaveCount(1);
        result.Fields.First().FieldName.Should().Be("Name");
        result.Fields.First().Label.Should().Be("Name");
        result.Fields.First().IsRequired.Should().BeTrue();
    }

    [Fact]
    public void Multiple_Fluent_Calls_Should_Chain_Correctly()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        var fieldBuilder = builder.AddField(x => x.Name);

        // Act
        var result = fieldBuilder
            .WithLabel("Full Name")
            .WithPlaceholder("Enter full name")
            .Required("Name is required")
            .WithCssClass("form-control")
            .WithHelpText("Your legal name")
            .WithOrder(1)
            .Disabled(false)
            .ReadOnly(false);

        var config = result.Build();

        // Assert
        result.Should().BeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Label.Should().Be("Full Name");
        field.Placeholder.Should().Be("Enter full name");
        field.IsRequired.Should().BeTrue();
        field.CssClass.Should().Be("form-control");
        field.HelpText.Should().Be("Your legal name");
        field.Order.Should().Be(1);
        field.IsDisabled.Should().BeFalse();
        field.IsReadOnly.Should().BeFalse();
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}