namespace FormCraft.UnitTests.Builders;

public class FormBuilderTests
{
    [Fact]
    public void Create_Should_Return_New_FormBuilder_Instance()
    {
        // Act
        var builder = FormBuilder<TestModel>.Create();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<FormBuilder<TestModel>>();
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
        fieldBuilder.Should().NotBeNull();
        fieldBuilder.Should().BeOfType<FieldBuilder<TestModel, string>>();
        
        var field = config.Fields.First(f => f.FieldName == "Name");
        field.FieldName.Should().Be("Name");
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
        
        nameField.Order.Should().Be(0);
        emailField.Order.Should().Be(1);
        ageField.Order.Should().Be(2);
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
        result.Should().BeSameAs(builder);
        config.Layout.Should().Be(FormLayout.Horizontal);
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
        result.Should().BeSameAs(builder);
        config.CssClass.Should().Be("custom-form");
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
        result.Should().BeSameAs(builder);
        config.ShowValidationSummary.Should().BeTrue();
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
        result.Should().BeSameAs(builder);
        config.ShowRequiredIndicator.Should().BeTrue();
        config.RequiredIndicator.Should().Be("**");
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
        configuration.Should().NotBeNull();
        configuration.Layout.Should().Be(FormLayout.Inline);
        configuration.CssClass.Should().Be("test-form");
        configuration.ShowValidationSummary.Should().BeTrue();
        configuration.ShowRequiredIndicator.Should().BeTrue();
        configuration.RequiredIndicator.Should().Be("*");
        configuration.Fields.Should().HaveCount(2);
        configuration.Fields.Should().Contain(f => f.FieldName == "Name");
        configuration.Fields.Should().Contain(f => f.FieldName == "Email");
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
        configuration.FieldDependencies.Should().ContainKey("City");
        configuration.FieldDependencies["City"].Should().HaveCount(1);
        configuration.FieldDependencies["City"].First().DependentFieldName.Should().Be("Country");
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
        configuration.Should().NotBeNull();
        configuration.Layout.Should().Be(FormLayout.Vertical);
        configuration.CssClass.Should().Be("registration-form");
        configuration.Fields.Should().HaveCount(5);
        
        var nameField = configuration.Fields.First(f => f.FieldName == "Name");
        nameField.Label.Should().Be("Full Name");
        nameField.IsRequired.Should().BeTrue();
        nameField.Order.Should().Be(1);

        var cityField = configuration.Fields.First(f => f.FieldName == "City");
        cityField.VisibilityCondition.Should().NotBeNull();
        
        configuration.FieldDependencies.Should().ContainKey("City");
        configuration.FieldDependencies["City"].Should().HaveCount(1);
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
        config.Fields.Should().HaveCount(4);
        
        var orderedFields = config.Fields.OrderBy(f => f.Order).ToList();
        orderedFields[0].FieldName.Should().Be("Name");
        orderedFields[1].FieldName.Should().Be("Email");
        orderedFields[2].FieldName.Should().Be("Age");
        orderedFields[3].FieldName.Should().Be("Country");
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
        config.Layout.Should().Be(FormLayout.Grid);
        config.CssClass.Should().Be("test-form");
        config.ShowValidationSummary.Should().BeFalse();
        config.ShowRequiredIndicator.Should().BeFalse();
    }

    [Fact]
    public void Default_Configuration_Should_Have_Expected_Values()
    {
        // Act
        var config = FormBuilder<TestModel>.Create().Build();

        // Assert
        config.Layout.Should().Be(FormLayout.Vertical); // Default layout
        config.CssClass.Should().BeNull();
        config.ShowValidationSummary.Should().BeTrue(); // Default
        config.ShowRequiredIndicator.Should().BeTrue(); // Default
        config.RequiredIndicator.Should().Be("*"); // Default
        config.Fields.Should().BeEmpty();
        config.FieldDependencies.Should().BeEmpty();
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