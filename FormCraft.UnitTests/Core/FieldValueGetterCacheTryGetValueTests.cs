namespace FormCraft.UnitTests.Core;

public class FieldValueGetterCacheTryGetValueTests
{
    [Fact]
    public void TryGetValue_Should_Return_True_And_The_Value_For_A_Resolvable_Binding()
    {
        // Arrange
        var model = new TestModel { Nested = new NestedModel { Value = "hello" } };
        var field = new FieldConfigurationWrapper<TestModel, string>(
            new FieldConfiguration<TestModel, string>(x => x.Nested!.Value));

        // Act
        var found = FieldValueGetterCache<TestModel>.TryGetValue(field, model, out var value);

        // Assert
        found.ShouldBeTrue();
        value.ShouldBe("hello");
    }

    [Fact]
    public void TryGetValue_Should_Return_False_Not_Throw_For_A_Nested_Binding_With_A_Null_Intermediate()
    {
        // Arrange
        var model = new TestModel { Nested = null };
        var field = new FieldConfigurationWrapper<TestModel, string>(
            new FieldConfiguration<TestModel, string>(x => x.Nested!.Value));

        // Act
        var found = FieldValueGetterCache<TestModel>.TryGetValue(field, model, out var value);

        // Assert
        found.ShouldBeFalse();
        value.ShouldBeNull();
    }

    private class TestModel
    {
        public NestedModel? Nested { get; set; }
    }

    private class NestedModel
    {
        public string Value { get; set; } = string.Empty;
    }
}
