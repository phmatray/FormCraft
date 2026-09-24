namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>
/// Pins that <c>.WithCssClass(...)</c> reaches the rendered <c>&lt;form&gt;</c> (#457) — it used to be
/// stored on the configuration and never read by the adapter.
/// </summary>
public class FormContainerCssClassTests : FluentUITestBase
{
    [Fact]
    public void Form_Should_Carry_The_Configured_CssClass()
    {
        // Arrange
        var config = FormBuilder<CssModel>.Create()
            .WithCssClass("my-form")
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<CssModel>>(p => p
            .Add(c => c.Model, new CssModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find("form").ClassList.ShouldContain("my-form");
    }

    [Fact]
    public void Form_Should_Render_No_Class_Attribute_When_No_CssClass_Is_Configured()
    {
        // Arrange
        var config = FormBuilder<CssModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<CssModel>>(p => p
            .Add(c => c.Model, new CssModel())
            .Add(c => c.Configuration, config));

        // Assert - null drops the attribute rather than rendering class=""
        component.Find("form").HasAttribute("class").ShouldBeFalse();
    }

    public class CssModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
