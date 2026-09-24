namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>
/// Pins that <c>.WithLayout(...)</c> reaches the rendered form as a <c>formcraft-layout-{value}</c>
/// class on the ungrouped-fields wrapper (#457), styled by <c>css/formcraft-layout.css</c>. The
/// setting used to be stored and never read.
/// </summary>
public class FormContainerLayoutTests : FluentUITestBase
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
        component.Find($"form .{expectedClass} fluent-text-input").ShouldNotBeNull();
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
        component.Find("form .formcraft-layout-vertical fluent-text-input").ShouldNotBeNull();
    }

    [Theory]
    [InlineData(FormLayout.Vertical, "display: contents")]
#pragma warning disable CS0618 // the obsolete Horizontal must still render like Vertical (#457)
    [InlineData(FormLayout.Horizontal, "display: contents")]
#pragma warning restore CS0618
    [InlineData(FormLayout.Inline, null)]
    [InlineData(FormLayout.Grid, null)]
    public void Only_A_Stacked_Layout_Should_Dissolve_Its_Wrapper_Into_The_FluentStack(
        FormLayout layout, string? expectedStyle)
    {
        // Arrange - Vertical must keep today's markup behaviour: its fields stay direct flex items
        // of the FluentStack (and keep its 12px gap) even when formcraft-layout.css is not linked.
        var config = FormBuilder<LayoutModel>.Create()
            .WithLayout(layout)
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<LayoutModel>>(p => p
            .Add(c => c.Model, new LayoutModel())
            .Add(c => c.Configuration, config));

        // Assert
        var wrapper = component.Find($"form .formcraft-layout-{layout.ToString().ToLowerInvariant()}");
        wrapper.GetAttribute("style").ShouldBe(expectedStyle);
    }

    public class LayoutModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
