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
        config.FieldName.ShouldBe("Name");
        config.ValueExpression.ShouldBeSameAs(expression);
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Arrange
        Expression<Func<TestModel, string>> expression = x => x.Name;

        // Act
        var config = new FieldConfiguration<TestModel, string>(expression);

        // Assert
        config.Label.ShouldBe("Name"); // Default label is field name
        config.Placeholder.ShouldBeNull();
        config.HelpText.ShouldBeNull();
        config.CssClass.ShouldBeNull();
        config.IsRequired.ShouldBeFalse();
        config.IsVisible.ShouldBeTrue();
        config.IsDisabled.ShouldBeFalse();
        config.IsReadOnly.ShouldBeFalse();
        config.Order.ShouldBe(0);
        config.VisibilityCondition.ShouldBeNull();
        config.DisabledCondition.ShouldBeNull();
        config.Validators.ShouldBeEmpty();
        config.Dependencies.ShouldBeEmpty();
        config.AdditionalAttributes.ShouldBeEmpty();
        config.CustomTemplate.ShouldBeNull();
    }

    [Fact]
    public void Should_Handle_Nested_Property_Expressions()
    {
        // Arrange
        Expression<Func<TestModel, string>> expression = x => x.Address.City;

        // Act
        var config = new FieldConfiguration<TestModel, string>(expression);

        // Assert: the identity is the full member path (#437), the default label stays the member.
        config.FieldName.ShouldBe("Address.City");
        config.Label.ShouldBe("City");
    }

    [Fact]
    public void FieldName_Should_Not_Collide_Between_Two_Differently_Nested_Fields_Sharing_A_Last_Segment()
    {
        // Act
        var billing = new FieldConfiguration<TwoSectionModel, decimal>(x => x.Billing.Amount);
        var shipping = new FieldConfiguration<TwoSectionModel, decimal>(x => x.Shipping.Amount);

        // Assert
        billing.FieldName.ShouldBe("Billing.Amount");
        shipping.FieldName.ShouldBe("Shipping.Amount");
        billing.Label.ShouldBe("Amount");
        shipping.Label.ShouldBe("Amount");
    }

    [Fact]
    public void FieldName_Should_Qualify_A_Three_Level_Nested_Path()
    {
        var config = new FieldConfiguration<ThreeLevelModel, decimal>(x => x.A.B.Amount);

        config.FieldName.ShouldBe("A.B.Amount");
    }

    [Fact]
    public void FieldName_Should_Stay_The_Bare_Member_For_A_Non_Nested_Field()
    {
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);

        config.FieldName.ShouldBe("Name");
        config.Label.ShouldBe("Name");
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
        config.Label.ShouldBe("Full Name");
        config.Placeholder.ShouldBe("Enter name");
        config.HelpText.ShouldBe("Your full name");
        config.CssClass.ShouldBe("custom-class");
        config.IsRequired.ShouldBeTrue();
        config.IsVisible.ShouldBeFalse();
        config.IsDisabled.ShouldBeTrue();
        config.IsReadOnly.ShouldBeTrue();
        config.Order.ShouldBe(5);
        config.VisibilityCondition.ShouldNotBeNull();
        config.DisabledCondition.ShouldNotBeNull();
        config.CustomTemplate.ShouldBeSameAs(template);
    }

    [Fact]
    public void Validators_Should_Be_Extended_Through_AddValidator()
    {
        // Arrange - `Validators` is IReadOnlyList since #155, so `.Add(...)` no longer compiles.
        // That is the point of the change: it used to compile on the object-typed view and silently
        // mutate a snapshot that never affected validation. AddValidator is the single mutation path
        // and has existed since 3.1.0, so consumers could migrate before upgrading.
        var config = new FieldConfiguration<TestModel, string>(x => x.Name);
        var validator = A.Fake<IFieldValidator<TestModel, string>>();

        // Act
        config.AddValidator(validator);

        // Assert
        config.Validators.ShouldContain(validator);
        config.Validators.Count.ShouldBe(1);
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
        config.Dependencies.ShouldContain(dependency);
        config.Dependencies.Count.ShouldBe(1);
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
        config.AdditionalAttributes.ShouldContainKey(key);
        config.AdditionalAttributes[key].ShouldBe(value);
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
        config.AdditionalAttributes.Count.ShouldBe(3);
        config.AdditionalAttributes["attr1"].ShouldBe("value1");
        config.AdditionalAttributes["attr2"].ShouldBe(42);
        config.AdditionalAttributes["attr3"].ShouldBe(true);
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
        config.VisibilityCondition.ShouldNotBeNull();
        config.VisibilityCondition!(model).ShouldBeTrue();

        model.Name = string.Empty;
        config.VisibilityCondition!(model).ShouldBeFalse();
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
        config.DisabledCondition.ShouldNotBeNull();
        config.DisabledCondition!(model).ShouldBeFalse();

        model.Name = string.Empty;
        config.DisabledCondition!(model).ShouldBeTrue();
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

    public class TwoSectionModel
    {
        public AmountSection Billing { get; set; } = new();
        public AmountSection Shipping { get; set; } = new();
    }

    public class AmountSection
    {
        public decimal Amount { get; set; }
    }

    public class ThreeLevelModel
    {
        public LevelA A { get; set; } = new();
    }

    public class LevelA
    {
        public LevelB B { get; set; } = new();
    }

    public class LevelB
    {
        public decimal Amount { get; set; }
    }
}
