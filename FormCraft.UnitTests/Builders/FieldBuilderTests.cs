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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Label.ShouldBe("Full Name");
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Placeholder.ShouldBe("Enter your name");
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.HelpText.ShouldBe("Please enter your full name");
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.CssClass.ShouldBe("custom-input");
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(1);
        field.Validators.First().ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
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
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(1);
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsDisabled.ShouldBeTrue();
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsReadOnly.ShouldBeTrue();
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.VisibilityCondition.ShouldBeSameAs(condition);
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.DisabledCondition.ShouldBeSameAs(condition);
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.AdditionalAttributes.ShouldContainKey("data-test");
        field.AdditionalAttributes["data-test"].ShouldBe("value");
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.AdditionalAttributes.ShouldContainKey("data-test");
        field.AdditionalAttributes.ShouldContainKey("min");
        field.AdditionalAttributes.ShouldContainKey("max");
        foreach (var attr in attributes)
        {
            field.AdditionalAttributes[attr.Key].ShouldBe(attr.Value);
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Count.ShouldBe(1);
        field.Validators.First().ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Count.ShouldBe(1);
        field.Validators.First().ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Count.ShouldBe(1);
        field.Validators.First().ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "City");
        field.Dependencies.Count.ShouldBe(1);
        field.Dependencies.First().DependentFieldName.ShouldBe("Country");
        
        config.FieldDependencies.ShouldContainKey("City");
        config.FieldDependencies["City"].Count.ShouldBe(1);
        config.FieldDependencies["City"].First().DependentFieldName.ShouldBe("Country");
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        var wrapper = field as FieldConfigurationWrapper<TestModel, string>;
        wrapper.ShouldNotBeNull();
        wrapper!.TypedConfiguration.CustomTemplate.ShouldBeSameAs(template);
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Order.ShouldBe(5);
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
        newFieldBuilder.ShouldNotBeSameAs(fieldBuilder);
        newFieldBuilder.ShouldBeOfType<FieldBuilder<TestModel, string>>();
        
        config.Fields.Count.ShouldBe(2);
        config.Fields.ShouldContain(f => f.FieldName == "Name");
        config.Fields.ShouldContain(f => f.FieldName == "Email");
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
        result.ShouldNotBeNull();
        result.Fields.Count.ShouldBe(1);
        result.Fields.First().FieldName.ShouldBe("Name");
        result.Fields.First().Label.ShouldBe("Name");
        result.Fields.First().IsRequired.ShouldBeTrue();
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
        result.ShouldBeSameAs(fieldBuilder);
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Label.ShouldBe("Full Name");
        field.Placeholder.ShouldBe("Enter full name");
        field.IsRequired.ShouldBeTrue();
        field.CssClass.ShouldBe("form-control");
        field.HelpText.ShouldBe("Your legal name");
        field.Order.ShouldBe(1);
        field.IsDisabled.ShouldBeFalse();
        field.IsReadOnly.ShouldBeFalse();
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