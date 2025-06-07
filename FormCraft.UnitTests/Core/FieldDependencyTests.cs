namespace FormCraft.UnitTests.Core;

public class FieldDependencyTests
{
    [Fact]
    public void Constructor_Should_Extract_DependsOnFieldName()
    {
        // Arrange
        Expression<Func<TestModel, string>> expression = x => x.Country;
        Action<TestModel, string> onChanged = (m, v) => { };

        // Act
        var dependency = new FieldDependency<TestModel, string>(expression, onChanged);

        // Assert
        dependency.DependentFieldName.Should().Be("Country");
    }

    [Fact]
    public void OnDependencyChanged_Should_Invoke_Callback_With_Correct_Value()
    {
        // Arrange
        var model = new TestModel { Country = "USA", City = "New York" };
        string? capturedCountry = null;
        TestModel? capturedModel = null;
        
        Action<TestModel, string> onChanged = (m, v) =>
        {
            capturedModel = m;
            capturedCountry = v;
        };

        var dependency = new FieldDependency<TestModel, string>(x => x.Country, onChanged);

        // Act
        dependency.OnDependencyChanged(model);

        // Assert
        capturedModel.Should().BeSameAs(model);
        capturedCountry.Should().Be("USA");
    }

    [Fact]
    public void OnDependencyChanged_Should_Handle_Null_Values()
    {
        // Arrange
        var model = new TestModel { Country = null };
        string? capturedCountry = "initial";
        
        Action<TestModel, string?> onChanged = (m, v) =>
        {
            capturedCountry = v;
        };

        var dependency = new FieldDependency<TestModel, string?>(x => x.Country, onChanged);

        // Act
        dependency.OnDependencyChanged(model);

        // Assert
        capturedCountry.Should().BeNull();
    }

    [Fact]
    public void Should_Support_Different_Property_Types()
    {
        // Arrange for int
        var model = new TestModel { Age = 25 };
        int capturedAge = 0;
        
        var intDependency = new FieldDependency<TestModel, int>(
            x => x.Age, 
            (m, v) => capturedAge = v);

        // Act
        intDependency.OnDependencyChanged(model);

        // Assert
        capturedAge.Should().Be(25);

        // Arrange for bool
        bool capturedIsActive = false;
        model.IsActive = true;
        
        var boolDependency = new FieldDependency<TestModel, bool>(
            x => x.IsActive, 
            (m, v) => capturedIsActive = v);

        // Act
        boolDependency.OnDependencyChanged(model);

        // Assert
        capturedIsActive.Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Complex_Dependency_Actions()
    {
        // Arrange
        var model = new TestModel { Country = "Canada", City = "Toronto" };
        
        Action<TestModel, string> onChanged = (m, country) =>
        {
            if (country == "USA")
            {
                m.City = "New York";
            }
            else if (country == "Canada")
            {
                m.City = "Toronto";
            }
            else
            {
                m.City = string.Empty;
            }
        };

        var dependency = new FieldDependency<TestModel, string>(x => x.Country, onChanged);

        // Act
        model.Country = "USA";
        dependency.OnDependencyChanged(model);

        // Assert
        model.City.Should().Be("New York");

        // Act
        model.Country = "UK";
        dependency.OnDependencyChanged(model);

        // Assert
        model.City.Should().BeEmpty();
    }

    [Fact]
    public void Should_Handle_Nested_Property_Dependencies()
    {
        // Arrange
        var model = new TestModel 
        { 
            Address = new AddressModel { PostalCode = "12345" } 
        };
        string? capturedPostalCode = null;
        
        var dependency = new FieldDependency<TestModel, string>(
            x => x.Address.PostalCode, 
            (m, v) => capturedPostalCode = v);

        // Act
        dependency.OnDependencyChanged(model);

        // Assert
        capturedPostalCode.Should().Be("12345");
        dependency.DependentFieldName.Should().Be("PostalCode");
    }

    public class TestModel
    {
        public string? Country { get; set; }
        public string City { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public AddressModel Address { get; set; } = new();
    }

    public class AddressModel
    {
        public string PostalCode { get; set; } = string.Empty;
    }
}