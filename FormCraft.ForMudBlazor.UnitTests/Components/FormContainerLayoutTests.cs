namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Pins that <c>.WithLayout(...)</c> reaches the rendered form as a <c>formcraft-layout-{value}</c>
/// class on the ungrouped-fields wrapper (#457), styled by <c>css/formcraft-layout.css</c>. The
/// setting used to be stored and never read.
/// </summary>
public class FormContainerLayoutTests : MudBlazorTestBase
{
    [Theory]
    [InlineData(FormLayout.Vertical, "formcraft-layout-vertical")]
    [InlineData(FormLayout.Inline, "formcraft-layout-inline")]
    [InlineData(FormLayout.Grid, "formcraft-layout-grid")]
    public void Fields_Wrapper_Should_Carry_The_Layout_Class(FormLayout layout, string expectedClass)
    {
        // Arrange
        var config = FormBuilder<LayoutModel>.Create()
            .WithLayout(layout)
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<LayoutModel>>(p => p
            .Add(c => c.Model, new LayoutModel())
            .Add(c => c.Configuration, config));

        // Assert - the class sits on an element that actually wraps the field's input
        component.Find($"form .{expectedClass} input").ShouldNotBeNull();
    }

    [Fact]
    public void Default_Layout_Should_Be_Vertical()
    {
        // Arrange
        var config = FormBuilder<LayoutModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<LayoutModel>>(p => p
            .Add(c => c.Model, new LayoutModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find("form .formcraft-layout-vertical input").ShouldNotBeNull();
    }

    public class LayoutModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
