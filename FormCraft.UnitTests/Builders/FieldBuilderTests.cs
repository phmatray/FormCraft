namespace FormCraft.UnitTests.Builders;

public class FieldBuilderTests
{
    [Fact]
    public void WithLabel_Should_Set_Label_In_Built_Configuration()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Full Name"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Label.ShouldBe("Full Name");
    }

    [Fact]
    public void WithPlaceholder_Should_Set_Placeholder_In_Built_Configuration()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithPlaceholder("Enter your name"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Placeholder.ShouldBe("Enter your name");
    }

    [Fact]
    public void WithHelpText_Should_Set_HelpText_In_Built_Configuration()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithHelpText("Please enter your full name"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.HelpText.ShouldBe("Please enter your full name");
    }

    [Fact]
    public void WithCssClass_Should_Set_CssClass_In_Built_Configuration()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithCssClass("custom-input"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.CssClass.ShouldBe("custom-input");
    }

    [Fact]
    public void Required_Should_Set_IsRequired_And_Add_Validator()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .Required("Name is required"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(1);
        field.Validators.First().ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void Required_Without_Message_Should_Use_Default_Message()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .Required())
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBe(1);
    }

    [Fact]
    public void Disabled_Should_Set_IsDisabled()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .Disabled())
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void ReadOnly_Should_Set_IsReadOnly()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .ReadOnly())
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void VisibleWhen_Should_Set_VisibilityCondition()
    {
        // Arrange
        Func<TestModel, bool> condition = m => m.IsActive;

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .VisibleWhen(condition))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.VisibilityCondition.ShouldBeSameAs(condition);
    }

    [Fact]
    public void DisabledWhen_Should_Set_DisabledCondition()
    {
        // Arrange
        Func<TestModel, bool> condition = m => !m.IsActive;

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .DisabledWhen(condition))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.DisabledCondition.ShouldBeSameAs(condition);
    }

    [Fact]
    public void WithAttribute_Should_Add_Single_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithAttribute("data-test", "value"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.AdditionalAttributes.ShouldContainKey("data-test");
        field.AdditionalAttributes["data-test"].ShouldBe("value");
    }

    [Fact]
    public void WithAttributes_Should_Add_Multiple_Attributes()
    {
        // Arrange
        var attributes = new Dictionary<string, object>
        {
            { "data-test", "value" },
            { "min", 10 },
            { "max", 100 }
        };

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithAttributes(attributes))
            .Build();

        // Assert
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
        var validator = A.Fake<IFieldValidator<TestModel, string>>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithValidator(validator))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Count.ShouldBe(1);
        field.Validators.First().ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void WithValidator_Func_Should_Add_CustomValidator()
    {
        // Arrange
        Func<string, bool> validation = value => !string.IsNullOrEmpty(value);

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithValidator(validation, "Value cannot be empty"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Count.ShouldBe(1);
        field.Validators.First().ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void WithAsyncValidator_Should_Add_AsyncValidator()
    {
        // Arrange
        Func<string, Task<bool>> validation = async value =>
        {
            await Task.Delay(1);
            return !string.IsNullOrEmpty(value);
        };

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithAsyncValidator(validation, "Value cannot be empty"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Validators.Count.ShouldBe(1);
        field.Validators.First().ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void DependsOn_Should_Add_Field_Dependency()
    {
        // Arrange
        Action<TestModel, string> onChanged = (m, v) => m.City = string.Empty;

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .DependsOn(x => x.Country, onChanged))
            .Build();

        // Assert
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
        RenderFragment<IFieldContext<TestModel, string>> template = context => builder =>
        {
            builder.AddContent(0, "Custom content");
        };

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithCustomTemplate(template))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        var wrapper = field as FieldConfigurationWrapper<TestModel, string>;
        wrapper.ShouldNotBeNull();
        wrapper.TypedConfiguration.CustomTemplate.ShouldBeSameAs(template);
    }

    [Fact]
    public void WithOrder_Should_Set_Order()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithOrder(5))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.Order.ShouldBe(5);
    }

    [Fact]
    public void AddField_Should_Add_Multiple_Fields_To_FormBuilder()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email"))
            .Build();

        // Assert
        config.Fields.Count.ShouldBe(2);
        config.Fields.ShouldContain(f => f.FieldName == "Name");
        config.Fields.ShouldContain(f => f.FieldName == "Email");
    }

    [Fact]
    public void Build_Should_Return_FormConfiguration()
    {
        // Arrange & Act
        var result = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .Required()
                .WithOrder(1))
            .Build();

        // Assert
        result.ShouldNotBeNull();
        result.Fields.Count.ShouldBe(1);
        result.Fields.First().FieldName.ShouldBe("Name");
        result.Fields.First().Label.ShouldBe("Name");
        result.Fields.First().IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void WithInputType_Should_Set_InputType_To_Password()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithInputType("password"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.InputType.ShouldBe("password");
    }

    [Fact]
    public void WithInputType_Should_Set_InputType_To_Email()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Email, field => field
                .WithInputType("email"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "Email");
        field.InputType.ShouldBe("email");
    }

    [Fact]
    public void Multiple_Fluent_Calls_Should_Chain_Correctly()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Full Name")
                .WithPlaceholder("Enter full name")
                .Required("Name is required")
                .WithCssClass("form-control")
                .WithHelpText("Your legal name")
                .WithOrder(1)
                .Disabled(false)
                .ReadOnly(false))
            .Build();

        // Assert
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