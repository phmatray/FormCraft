namespace FormCraft.UnitTests.Core;

public class SelectOptionTests
{
    [Fact]
    public void Parameterless_Constructor_Should_Create_Instance_With_Default_Values()
    {
        // Act
        var option = new SelectOption<string>();

        // Assert
        option.Value.ShouldBeNull();
        option.Label.ShouldBe(string.Empty);
    }

    [Fact]
    public void Parameterized_Constructor_Should_Set_Properties()
    {
        // Arrange
        const string value = "option1";
        const string label = "Option 1";

        // Act
        var option = new SelectOption<string>(value, label);

        // Assert
        option.Value.ShouldBe(value);
        option.Label.ShouldBe(label);
    }

    [Fact]
    public void Should_Support_Different_Value_Types()
    {
        // Act & Assert for int
        var intOption = new SelectOption<int>(42, "Answer");
        intOption.Value.ShouldBe(42);
        intOption.Label.ShouldBe("Answer");

        // Act & Assert for enum
        var enumOption = new SelectOption<DayOfWeek>(DayOfWeek.Monday, "Monday");
        enumOption.Value.ShouldBe(DayOfWeek.Monday);
        enumOption.Label.ShouldBe("Monday");

        // Act & Assert for custom type
        var customOption = new SelectOption<TestClass>(new TestClass { Id = 1 }, "Test");
        customOption.Value.Id.ShouldBe(1);
        customOption.Label.ShouldBe("Test");
    }

    [Fact]
    public void Properties_Should_Be_Settable()
    {
        // Arrange
        var option = new SelectOption<string>();

        // Act
        option.Value = "newValue";
        option.Label = "New Label";

        // Assert
        option.Value.ShouldBe("newValue");
        option.Label.ShouldBe("New Label");
    }

    private class TestClass
    {
        public int Id { get; set; }
    }
}