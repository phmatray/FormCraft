namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>A minimal model shared by the container-level tests.</summary>
public class TestModel
{
    /// <summary>A plain string field.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// FormCraft validates server-side and documents that the browser runs no constraint validation of
/// its own. That guarantee is a real <c>novalidate</c> attribute in the markup (#206), not a script
/// applied after render - the Fluent adapter carries it on the same terms as the MudBlazor one.
/// </summary>
public class NoValidateTests : FluentUITestBase
{
    [Fact]
    public void Form_Should_Render_The_NoValidate_Attribute()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert - the attribute a browser actually reads, on the element it reads it from
        component.Find("form").HasAttribute("novalidate").ShouldBeTrue();
    }

    [Fact]
    public void Form_Should_Render_A_Submit_Button_By_Default()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Markup.ShouldContain("Submit");
    }

    [Fact]
    public void Form_Should_Omit_The_Submit_Button_When_Asked()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config)
            .Add(c => c.ShowSubmitButton, false));

        // Assert
        component.FindAll("fluent-button").ShouldBeEmpty();
    }

    [Fact]
    public void Hidden_Fields_Should_Not_Render()
    {
        // Arrange - a visibility condition that never holds
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Name")
                .VisibleWhen(_ => false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Markup.ShouldNotContain("Name");
    }
}
