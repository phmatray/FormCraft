namespace FormCraft.UnitTests.Core;

public class FieldDependencyTests
{
    [Fact]
    public void Constructor_Should_Extract_DependsOnFieldName()
    {
        // Arrange
        Expression<Func<TestModel, string?>> expression = x => x.Country;
        Action<TestModel, string?> onChanged = (m, v) => { };

        // Act
        var dependency = new FieldDependency<TestModel, string?>(expression, onChanged);

        // Assert
        dependency.DependentFieldName.ShouldBe("Country");
    }

    [Fact]
    public void OnDependencyChanged_Should_Invoke_Callback_With_Correct_Value()
    {
        // Arrange
        var model = new TestModel { Country = "USA", City = "New York" };
        string? capturedCountry = null;
        TestModel? capturedModel = null;

        Action<TestModel, string?> onChanged = (m, v) =>
        {
            capturedModel = m;
            capturedCountry = v;
        };

        var dependency = new FieldDependency<TestModel, string?>(x => x.Country, onChanged);

        // Act
        dependency.OnDependencyChanged(model);

        // Assert
        capturedModel.ShouldBeSameAs(model);
        capturedCountry.ShouldBe("USA");
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
        capturedCountry.ShouldBeNull();
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
        capturedAge.ShouldBe(25);

        // Arrange for bool
        bool capturedIsActive = false;
        model.IsActive = true;

        var boolDependency = new FieldDependency<TestModel, bool>(
            x => x.IsActive,
            (m, v) => capturedIsActive = v);

        // Act
        boolDependency.OnDependencyChanged(model);

        // Assert
        capturedIsActive.ShouldBeTrue();
    }

    [Fact]
    public void Should_Handle_Complex_Dependency_Actions()
    {
        // Arrange
        var model = new TestModel { Country = "Canada", City = "Toronto" };

        Action<TestModel, string?> onChanged = (m, country) =>
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

        var dependency = new FieldDependency<TestModel, string?>(x => x.Country, onChanged);

        // Act
        model.Country = "USA";
        dependency.OnDependencyChanged(model);

        // Assert
        model.City.ShouldBe("New York");

        // Act
        model.Country = "UK";
        dependency.OnDependencyChanged(model);

        // Assert
        model.City.ShouldBeEmpty();
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
        capturedPostalCode.ShouldBe("12345");
        dependency.DependentFieldName.ShouldBe("PostalCode");
    }

    [Fact]
    public void Async_Constructor_Should_Extract_DependsOnFieldName()
    {
        // Arrange
        Expression<Func<TestModel, string?>> expression = x => x.Country;
        Func<TestModel, string?, Task> onChangedAsync = (_, _) => Task.CompletedTask;

        // Act
        var dependency = new FieldDependency<TestModel, string?>(expression, onChangedAsync);

        // Assert
        dependency.DependentFieldName.ShouldBe("Country");
    }

    [Fact]
    public async Task OnDependencyChangedAsync_Should_Invoke_Async_Callback_With_Correct_Value()
    {
        // Arrange
        var model = new TestModel { Country = "USA" };
        string? capturedCountry = null;
        TestModel? capturedModel = null;

        var dependency = new FieldDependency<TestModel, string?>(
            x => x.Country,
            async (m, v) =>
            {
                await Task.Delay(10);
                capturedModel = m;
                capturedCountry = v;
            });

        // Act
        await dependency.OnDependencyChangedAsync(model);

        // Assert
        capturedModel.ShouldBeSameAs(model);
        capturedCountry.ShouldBe("USA");
    }

    [Fact]
    public async Task OnDependencyChangedAsync_Should_Invoke_Sync_Callback_When_Constructed_Synchronously()
    {
        // Arrange
        var model = new TestModel { Country = "Belgium" };
        string? capturedCountry = null;

        var dependency = new FieldDependency<TestModel, string?>(
            x => x.Country,
            (_, v) => capturedCountry = v);

        // Act
        await dependency.OnDependencyChangedAsync(model);

        // Assert
        capturedCountry.ShouldBe("Belgium");
    }

    [Fact]
    public void OnDependencyChanged_Should_Run_Async_Callback_To_Completion()
    {
        // Arrange - the legacy sync entry point must still execute async callbacks
        var model = new TestModel { Country = "France" };
        string? capturedCountry = null;

        var dependency = new FieldDependency<TestModel, string?>(
            x => x.Country,
            (_, v) =>
            {
                capturedCountry = v;
                return Task.CompletedTask;
            });

        // Act
        dependency.OnDependencyChanged(model);

        // Assert
        capturedCountry.ShouldBe("France");
    }

    [Fact]
    public async Task Default_Interface_Implementation_Should_Wrap_Sync_OnDependencyChanged()
    {
        // Arrange - an external implementor that only knows the sync member must
        // keep working through the new async dispatch path (non-breaking contract)
        var model = new TestModel { Country = "USA" };
        IFieldDependency<TestModel> dependency = new SyncOnlyDependency();

        // Act
        await dependency.OnDependencyChangedAsync(model);

        // Assert
        ((SyncOnlyDependency)dependency).Invocations.ShouldBe(1);
    }

    private class SyncOnlyDependency : IFieldDependency<TestModel>
    {
        public int Invocations { get; private set; }
        public string DependentFieldName => "Country";
        public void OnDependencyChanged(TestModel model) => Invocations++;
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
