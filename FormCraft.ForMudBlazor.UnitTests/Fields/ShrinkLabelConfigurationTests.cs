namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests for the configurable MudBlazor ShrinkLabel (#177): the .WithShrinkLabel(...)
/// field extension, the form-level FormCraftComponent.DefaultShrinkLabel parameter, and
/// the precedence between them. Follow-up to the configurable Variant (#146) — with
/// Variant.Text a permanently shrunk label has nothing to anchor to, so consumers need
/// to be able to let it float.
/// </summary>
public class ShrinkLabelConfigurationTests : MudBlazorTestBase
{
    [Fact]
    public void TextField_Should_Default_To_ShrinkLabel_True()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - unchanged from v3.1.0: the label stays pinned unless asked otherwise
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    [Fact]
    public void WithShrinkLabel_False_Should_Apply_To_TextField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithShrinkLabel(false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void WithShrinkLabel_Should_Default_Its_Argument_To_True()
    {
        // Arrange - the parameterless call mirrors AsPassword(enableVisibilityToggle: true)
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithShrinkLabel())
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
