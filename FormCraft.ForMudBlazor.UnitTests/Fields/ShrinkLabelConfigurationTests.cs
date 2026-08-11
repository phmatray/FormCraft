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

    [Fact]
    public void DefaultShrinkLabel_False_Should_Apply_To_Fields_Without_Explicit_Setting()
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
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultShrinkLabel, false));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void FieldLevel_False_Should_Override_FormLevel_True()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithShrinkLabel(false))
            .Build();

        // Act - form says true (the default), field says false
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultShrinkLabel, true));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void FieldLevel_True_Should_Override_FormLevel_False()
    {
        // Arrange - the other direction, which a null-coalescing bug would silently break
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithShrinkLabel(true))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultShrinkLabel, false));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    [Fact]
    public void DefaultShrinkLabel_Should_Default_To_True()
    {
        // Arrange - a form that never mentions ShrinkLabel keeps the v3.1.0 rendering
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Instance.DefaultShrinkLabel.ShouldBeTrue();
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
