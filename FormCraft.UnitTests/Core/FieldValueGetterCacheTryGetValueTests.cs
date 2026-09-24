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

    [Fact]
    public void TryGetValue_Should_Propagate_An_Exception_From_A_Genuinely_Faulting_Accessor()
    {
        // Arrange - a getter that is broken, not merely unreachable behind a null intermediate.
        // Before #425 TryInvoke caught Exception broadly, so this read came back as (false, null)
        // and every caller validated it as an ordinary null value.
        var field = new FieldConfigurationWrapper<FaultingModel, string>(
            new FieldConfiguration<FaultingModel, string>(x => x.Faulting));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            FieldValueGetterCache<FaultingModel>.TryGetValue(field, new FaultingModel(), out _));
    }

    [Fact]
    public void TryGetValue_Should_Propagate_An_OperationCanceledException()
    {
        // Arrange - a cancellation must stay a cancellation, not become a fabricated null value (#425).
        var field = new FieldConfigurationWrapper<FaultingModel, string>(
            new FieldConfiguration<FaultingModel, string>(x => x.Cancelled));

        // Act & Assert
        Should.Throw<OperationCanceledException>(() =>
            FieldValueGetterCache<FaultingModel>.TryGetValue(field, new FaultingModel(), out _));
    }

    private class TestModel
    {
        public NestedModel? Nested { get; set; }
    }

    private class NestedModel
    {
        public string Value { get; set; } = string.Empty;
    }

    private class FaultingModel
    {
        public string Faulting => throw new InvalidOperationException("boom");

        public string Cancelled => throw new OperationCanceledException();
    }
}
