namespace FormCraft.UnitTests.Builders;

public class FormBuilderTests
{
    [Fact]
    public void Create_Should_Return_New_FormBuilder_Instance()
    {
        // Act
        var builder = FormBuilder<TestModel>.Create();

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<FormBuilder<TestModel>>();
    }

    [Fact]
    public void AddField_Should_Create_FieldBuilder_And_Add_To_Fields()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var fieldBuilder = builder.AddField(x => x.Name);
        var config = fieldBuilder.Build();

        // Assert
        fieldBuilder.ShouldNotBeNull();
        fieldBuilder.ShouldBeOfType<FieldBuilder<TestModel, string>>();
        
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.FieldName.ShouldBe("Name");
    }

    [Fact]
    public void AddField_Should_Assign_Incremental_Order()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var field1 = builder.AddField(x => x.Name);
        var field2 = field1.AddField(x => x.Email);
        var field3 = field2.AddField(x => x.Age);
        var config = field3.Build();

        // Assert
        var nameField = config.Fields.First(f => f.FieldName == "Name");
        var emailField = config.Fields.First(f => f.FieldName == "Email");
        var ageField = config.Fields.First(f => f.FieldName == "Age");
        
        nameField.Order.ShouldBe(0);
        emailField.Order.ShouldBe(1);
        ageField.Order.ShouldBe(2);
    }

    [Fact]
    public void WithLayout_Should_Set_Layout()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.WithLayout(FormLayout.Horizontal);
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        config.Layout.ShouldBe(FormLayout.Horizontal);
    }

    [Fact]
    public void WithCssClass_Should_Set_CssClass()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.WithCssClass("custom-form");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        config.CssClass.ShouldBe("custom-form");
    }

    [Fact]
    public void ShowValidationSummary_Should_Set_ShowValidationSummary()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.ShowValidationSummary(true);
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        config.ShowValidationSummary.ShouldBeTrue();
    }

    [Fact]
    public void ShowRequiredIndicator_Should_Set_Properties()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var result = builder.ShowRequiredIndicator(true, "**");
        var config = result.Build();

        // Assert
        result.ShouldBeSameAs(builder);
        config.ShowRequiredIndicator.ShouldBeTrue();
        config.RequiredIndicator.ShouldBe("**");
    }

    [Fact]
    public void Build_Should_Return_FormConfiguration_With_All_Fields()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create()
            .WithLayout(FormLayout.Inline)
            .WithCssClass("test-form")
            .ShowValidationSummary(true)
            .ShowRequiredIndicator(true, "*");

        builder.AddField(x => x.Name)
            .WithLabel("Name")
            .Required()
            .AddField(x => x.Email)
            .WithLabel("Email");

        // Act
        var configuration = builder.Build();

        // Assert
        configuration.ShouldNotBeNull();
        configuration.Layout.ShouldBe(FormLayout.Inline);
        configuration.CssClass.ShouldBe("test-form");
        configuration.ShowValidationSummary.ShouldBeTrue();
        configuration.ShowRequiredIndicator.ShouldBeTrue();
        configuration.RequiredIndicator.ShouldBe("*");
        configuration.Fields.Count.ShouldBe(2);
        configuration.Fields.ShouldContain(f => f.FieldName == "Name");
        configuration.Fields.ShouldContain(f => f.FieldName == "Email");
    }

    [Fact]
    public void Build_Should_Include_Field_Dependencies()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        
        builder.AddField(x => x.City)
            .DependsOn(x => x.Country, (m, v) => m.City = string.Empty);

        // Act
        var configuration = builder.Build();

        // Assert
        configuration.FieldDependencies.ShouldContainKey("City");
        configuration.FieldDependencies["City"].Count.ShouldBe(1);
        configuration.FieldDependencies["City"].First().DependentFieldName.ShouldBe("Country");
    }

    [Fact]
    public void Complex_Form_Building_Scenario()
    {
        // Arrange & Act
        var configuration = FormBuilder<TestModel>.Create()
            .WithLayout(FormLayout.Vertical)
            .WithCssClass("registration-form")
            .ShowValidationSummary(true)
            .ShowRequiredIndicator(true, "*")
            .AddField(x => x.Name)
                .WithLabel("Full Name")
                .WithPlaceholder("Enter your full name")
                .Required("Name is required")
                .WithOrder(1)
            .AddField(x => x.Email)
                .WithLabel("Email Address")
                .WithPlaceholder("user@example.com")
                .Required("Email is required")
                .WithOrder(2)
            .AddField(x => x.Age)
                .WithLabel("Age")
                .WithOrder(3)
            .AddField(x => x.Country)
                .WithLabel("Country")
                .Required()
                .WithOrder(4)
            .AddField(x => x.City)
                .WithLabel("City")
                .VisibleWhen(m => !string.IsNullOrEmpty(m.Country))
                .DependsOn(x => x.Country, (m, country) =>
                {
                    if (string.IsNullOrEmpty(country))
                        m.City = string.Empty;
                })
                .WithOrder(5)
            .Build();

        // Assert
        configuration.ShouldNotBeNull();
        configuration.Layout.ShouldBe(FormLayout.Vertical);
        configuration.CssClass.ShouldBe("registration-form");
        configuration.Fields.Count.ShouldBe(5);
        
        var nameField = configuration.Fields.First(f => f.FieldName == "Name");
        nameField.Label.ShouldBe("Full Name");
        nameField.IsRequired.ShouldBeTrue();
        nameField.Order.ShouldBe(1);

        var cityField = configuration.Fields.First(f => f.FieldName == "City");
        cityField.VisibilityCondition.ShouldNotBeNull();
        
        configuration.FieldDependencies.ShouldContainKey("City");
        configuration.FieldDependencies["City"].Count.ShouldBe(1);
    }

    [Fact]
    public void Multiple_Fields_Should_Be_Ordered_Correctly()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();

        // Act
        var config = builder
            .AddField(x => x.Name)
            .AddField(x => x.Email)
            .AddField(x => x.Age)
            .AddField(x => x.Country)
            .Build();

        // Assert
        config.Fields.Count.ShouldBe(4);
        
        var orderedFields = config.Fields.OrderBy(f => f.Order).ToList();
        orderedFields[0].FieldName.ShouldBe("Name");
        orderedFields[1].FieldName.ShouldBe("Email");
        orderedFields[2].FieldName.ShouldBe("Age");
        orderedFields[3].FieldName.ShouldBe("Country");
    }

    [Fact]
    public void FormBuilder_Should_Allow_Method_Chaining()
    {
        // Act
        var result = FormBuilder<TestModel>.Create()
            .WithLayout(FormLayout.Grid)
            .WithCssClass("test-form")
            .ShowValidationSummary(false)
            .ShowRequiredIndicator(false);

        var config = result.Build();

        // Assert
        config.Layout.ShouldBe(FormLayout.Grid);
        config.CssClass.ShouldBe("test-form");
        config.ShowValidationSummary.ShouldBeFalse();
        config.ShowRequiredIndicator.ShouldBeFalse();
    }

    [Fact]
    public void Default_Configuration_Should_Have_Expected_Values()
    {
        // Act
        var config = FormBuilder<TestModel>.Create().Build();

        // Assert
        config.Layout.ShouldBe(FormLayout.Vertical); // Default layout
        config.CssClass.ShouldBeNull();
        config.ShowValidationSummary.ShouldBeTrue(); // Default
        config.ShowRequiredIndicator.ShouldBeTrue(); // Default
        config.RequiredIndicator.ShouldBe("*"); // Default
        config.Fields.ShouldBeEmpty();
        config.FieldDependencies.ShouldBeEmpty();
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}