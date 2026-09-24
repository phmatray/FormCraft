namespace FormCraft.UnitTests.Core;

public class FieldValueSetterCacheTests
{
    [Fact]
    public void GetOrCompile_Should_Assign_A_Top_Level_Property()
    {
        // Arrange
        var model = new TestModel();
        var field = new FieldConfigurationWrapper<TestModel, string>(
            new FieldConfiguration<TestModel, string>(x => x.Name));

        // Act
        FieldValueSetterCache<TestModel>.GetOrCompile(field)(model, "Jane");

        // Assert
        model.Name.ShouldBe("Jane");
    }

    [Fact]
    public void GetOrCompile_Should_Assign_A_Nested_Member_When_The_Intermediate_Is_Not_Null()
    {
        // Arrange
        var model = new TestModel { Nested = new NestedModel() };
        var field = new FieldConfigurationWrapper<TestModel, string>(
            new FieldConfiguration<TestModel, string>(x => x.Nested!.Value));

        // Act
        FieldValueSetterCache<TestModel>.GetOrCompile(field)(model, "hello");

        // Assert
        model.Nested.Value.ShouldBe("hello");
    }

    [Fact]
    public void GetOrCompile_Should_Throw_For_A_Nested_Binding_With_A_Null_Intermediate()
    {
        // Arrange - the caller's job to catch, mirroring FieldValueGetterCache's read side.
        var model = new TestModel { Nested = null };
        var field = new FieldConfigurationWrapper<TestModel, string>(
            new FieldConfiguration<TestModel, string>(x => x.Nested!.Value));
        var setter = FieldValueSetterCache<TestModel>.GetOrCompile(field);

        // Act & Assert
        Should.Throw<NullReferenceException>(() => setter(model, "hello"));
    }

    [Fact]
    public void GetOrCompile_Should_Return_The_Same_Delegate_For_The_Same_Field_Instance()
    {
        // Arrange
        var field = new FieldConfigurationWrapper<TestModel, string>(
            new FieldConfiguration<TestModel, string>(x => x.Name));

        // Act
        var first = FieldValueSetterCache<TestModel>.GetOrCompile(field);
        var second = FieldValueSetterCache<TestModel>.GetOrCompile(field);

        // Assert
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void GetOrCompile_Should_Convert_The_Value_To_The_Target_Members_Type()
    {
        // Arrange - mirrors the Convert.ChangeType coercion both adapters' UpdateFieldValue used to
        // perform themselves against a reflection-found PropertyInfo, now sourced from the resolved
        // member's actual type so it also applies to a nested member.
        var model = new TestModel();
        var field = new FieldConfigurationWrapper<TestModel, int>(
            new FieldConfiguration<TestModel, int>(x => x.Age));

        // Act - a boxed long, as a numeric UI control might hand back.
        FieldValueSetterCache<TestModel>.GetOrCompile(field)(model, 42L);

        // Assert
        model.Age.ShouldBe(42);
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public NestedModel? Nested { get; set; }
    }

    private class NestedModel
    {
        public string Value { get; set; } = string.Empty;
    }
}
