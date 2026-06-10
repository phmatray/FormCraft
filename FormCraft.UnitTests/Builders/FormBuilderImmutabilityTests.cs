namespace FormCraft.UnitTests.Builders;

/// <summary>
/// Regression tests for the "immutable after Build()" contract: continuing to use
/// a builder after Build() used to silently mutate the already-published configuration.
/// </summary>
public class FormBuilderImmutabilityTests
{
    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    [Fact]
    public void AddField_After_Build_Should_Throw()
    {
        var builder = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"));

        var config = builder.Build();

        Should.Throw<InvalidOperationException>(() =>
            builder.AddField(x => x.Email, f => f.WithLabel("Email")));
        config.Fields.Count.ShouldBe(1);
    }

    [Fact]
    public void WithLayout_After_Build_Should_Throw()
    {
        var builder = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name);
        builder.Build();

        Should.Throw<InvalidOperationException>(() => builder.WithLayout(FormLayout.Grid));
    }
}
