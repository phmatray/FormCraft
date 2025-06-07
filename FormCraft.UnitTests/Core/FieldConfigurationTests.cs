namespace FormCraft.UnitTests.Core;

public class FieldConfigurationTests
{
    [Fact]
    public void Constructor_Should_Extract_FieldName_From_Expression()
    {
        // Arrange
        Expression<Func<TestModel, string>> expression = x => x.Name;

        // Act
        var config = new FieldConfiguration<TestModel, string>(expression);

        // Assert
        config.FieldName.Should().Be("Name");
        config.ValueExpression.Should().BeSameAs(expression);
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Arrange
        Expression<Func<TestModel, string>> expression = x => x.Name;

        // Act
        var config = new FieldConfiguration<TestModel, string>(expression);

        // Assert
        config.Label.Should().Be("Name"); // Default label is field name
        config.Placeholder.Should().BeNull();
        config.HelpText.Should().BeNull();
        config.CssClass.Should().BeNull();
        config.IsRequired.Should().BeFalse();
        config.IsVisible.Should().BeTrue();
        config.IsDisabled.Should().BeFalse();
        config.IsReadOnly.Should().BeFalse();
        config.Order.Should().Be(0);
        config.VisibilityCondition.Should().BeNull();
        config.DisabledCondition.Should().BeNull();
        config.Validators.Should().BeEmpty();
        config.Dependencies.Should().BeEmpty();
        config.AdditionalAttributes.Should().BeEmpty();
        config.CustomTemplate.Should().BeNull();
    }

    [Fact]
    public void Should_Handle_Nested_Property_Expressions()
    {
        // Arrange
        Expression<Func<TestModel, string>> expression = x => x.Address.City;

        // Act
        var config = new FieldConfiguration<TestModel, string>(expression);

        // Assert
        config.FieldName.Should().Be("City"); // Only the final property name is used
    }

    [Fact]
    public void All_Properties_Should_Be_Settable()
    {
        // Arrange
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);
        RenderFragment<IFieldContext<TestModel, string>> template = context => builder => { };

        // Act
        config.Label = "Full Name";
        config.Placeholder = "Enter name";
        config.HelpText = "Your full name";
        config.CssClass = "custom-class";
        config.IsRequired = true;
        config.IsVisible = false;
        config.IsDisabled = true;
        config.IsReadOnly = true;
        config.Order = 5;
        config.VisibilityCondition = m => true;
        config.DisabledCondition = m => false;
        config.CustomTemplate = template;

        // Assert
        config.Label.Should().Be("Full Name");
        config.Placeholder.Should().Be("Enter name");
        config.HelpText.Should().Be("Your full name");
        config.CssClass.Should().Be("custom-class");
        config.IsRequired.Should().BeTrue();
        config.IsVisible.Should().BeFalse();
        config.IsDisabled.Should().BeTrue();
        config.IsReadOnly.Should().BeTrue();
        config.Order.Should().Be(5);
        config.VisibilityCondition.Should().NotBeNull();
        config.DisabledCondition.Should().NotBeNull();
        config.CustomTemplate.Should().BeSameAs(template);
    }

    [Fact]
    public void Validators_Collection_Should_Be_Modifiable()
    {
        // Arrange
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);
        var validator = A.Fake<IFieldValidator<TestModel, string>>();

        // Act
        config.Validators.Add(validator);

        // Assert
        config.Validators.Should().Contain(validator);
        config.Validators.Should().HaveCount(1);
    }

    [Fact]
    public void Dependencies_Collection_Should_Be_Modifiable()
    {
        // Arrange
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);
        var dependency = A.Fake<IFieldDependency<TestModel>>();

        // Act
        config.Dependencies.Add(dependency);

        // Assert
        config.Dependencies.Should().Contain(dependency);
        config.Dependencies.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("data-test", "value")]
    [InlineData("min", 10)]
    [InlineData("max", 100)]
    [InlineData("required", true)]
    public void AdditionalAttributes_Dictionary_Should_Support_Various_Types(string key, object value)
    {
        // Arrange
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);

        // Act
        config.AdditionalAttributes[key] = value;

        // Assert
        config.AdditionalAttributes.Should().ContainKey(key);
        config.AdditionalAttributes[key].Should().Be(value);
    }

    [Fact]
    public void AdditionalAttributes_Should_Allow_Multiple_Values()
    {
        // Arrange
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);

        // Act
        config.AdditionalAttributes["attr1"] = "value1";
        config.AdditionalAttributes["attr2"] = 42;
        config.AdditionalAttributes["attr3"] = true;

        // Assert
        config.AdditionalAttributes.Should().HaveCount(3);
        config.AdditionalAttributes["attr1"].Should().Be("value1");
        config.AdditionalAttributes["attr2"].Should().Be(42);
        config.AdditionalAttributes["attr3"].Should().Be(true);
    }

    [Fact]
    public void VisibilityCondition_Should_Be_Callable()
    {
        // Arrange
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);
        var model = new TestModel { Name = "Test" };
        
        // Act
        config.VisibilityCondition = m => !string.IsNullOrEmpty(m.Name);

        // Assert
        config.VisibilityCondition.Should().NotBeNull();
        config.VisibilityCondition!(model).Should().BeTrue();
        
        model.Name = string.Empty;
        config.VisibilityCondition!(model).Should().BeFalse();
    }

    [Fact]
    public void DisabledCondition_Should_Be_Callable()
    {
        // Arrange
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);
        var model = new TestModel { Name = "Test" };
        
        // Act
        config.DisabledCondition = m => string.IsNullOrEmpty(m.Name);

        // Assert
        config.DisabledCondition.Should().NotBeNull();
        config.DisabledCondition!(model).Should().BeFalse();
        
        model.Name = string.Empty;
        config.DisabledCondition!(model).Should().BeTrue();
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public AddressModel Address { get; set; } = new();
    }

    public class AddressModel
    {
        public string City { get; set; } = string.Empty;
    }
}