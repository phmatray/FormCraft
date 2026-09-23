namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>
/// Regression tests for WithCustomTemplate mirroring
/// <c>FormCraft.ForMudBlazor.UnitTests.Components.CustomTemplateTests</c>: a custom template must
/// render whatever its value expression can read, and never silently disappear (#330).
/// </summary>
/// <remarks>
/// <b>#330 investigation (Task 1, Step 4).</b> <c>FormCraftComponent.razor.cs</c> has two other
/// <c>typeof(TModel).GetProperty(...)</c> sites besides the custom-template read fixed here:
/// <list type="bullet">
/// <item>
/// <c>LogSubmissionAuditEventAsync</c> (audit logging, #321's security pipeline). It shares the
/// same reflective, top-level-only lookup, but not this issue's <i>rendering</i> defect — a miss
/// there degrades to a null audit value rather than an invisible field. Left alone; out of scope
/// for #330 and queued under #321.
/// </item>
/// <item>
/// <c>UpdateFieldValue</c> (the write-back). Explicitly out of scope per the issue's Non-goals —
/// read side only. It also cannot silently vanish the way the read could: a miss there just no-ops
/// the write.
/// </item>
/// </list>
/// Neither is touched by this fix.
/// </remarks>
public class CustomTemplateTests : FluentUITestBase
{
    [Fact]
    public void WithCustomTemplate_Should_Render_Template_Content()
    {
        // Arrange
        var model = new TestModel { Name = "John" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "my-custom-template");
                    builder.AddContent(2, $"Custom: {context.Value}");
                    builder.CloseElement();
                }))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find(".my-custom-template").TextContent.ShouldBe("Custom: John");
    }

    [Fact]
    public void WithCustomTemplate_Should_Render_When_Bound_To_A_Nested_Property()
    {
        // Arrange - the expression's last member is "Value", which is NOT a top-level property of
        // NestedPropertyModel. Before #330 the reflective read did
        // typeof(NestedPropertyModel).GetProperty("Value"), found nothing, and returned early: the
        // field silently rendered nothing even though Nested is set and the value is reachable.
        var model = new NestedPropertyModel { Nested = new NestedValue { Value = "deep" } };
        var config = FormBuilder<NestedPropertyModel>
            .Create()
            .AddField(x => x.Nested!.Value, field => field
                .WithLabel("Nested value")
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "nested-template");
                    builder.AddContent(2, $"Custom: {context.Value}");
                    builder.CloseElement();
                }))
            .Build();

        // Act
        var component = Render<FormCraftComponent<NestedPropertyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find(".nested-template").TextContent.ShouldBe("Custom: deep");
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }

    private class NestedPropertyModel
    {
        public NestedValue? Nested { get; set; }
    }

    private class NestedValue
    {
        public string Value { get; set; } = string.Empty;
    }
}
