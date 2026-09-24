using FormCraft.ForMudBlazor.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Regression tests for WithCustomTemplate: templates configured through the
/// typed builder API must actually render (they used to be silently dropped by
/// FieldConfigurationWrapper and ignored by the render pipeline).
/// </summary>
/// <remarks>
/// <b>#330 investigation (Task 1, Step 4).</b> <c>FormCraftComponent.razor.cs</c> has two other
/// <c>typeof(TModel).GetProperty(...)</c> sites besides the custom-template read fixed here:
/// <list type="bullet">
/// <item>
/// <c>LogSubmissionAuditEventAsync</c> (audit logging, #321's security pipeline). It shares the
/// same reflective, top-level-only lookup, but not this issue's <i>rendering</i> defect — a miss
/// there degrades to a null audit value (<c>property?.GetValue(Model)?.ToString()</c>) rather than
/// an invisible field. Left alone; out of scope for #330 and queued under #321.
/// </item>
/// <item>
/// <c>UpdateFieldValue</c> (the write-back). Was explicitly out of scope per #330's Non-goals —
/// read side only — leaving it with the identical top-level-only lookup and the same asymmetric
/// silent-no-op failure mode. Fixed by #396: it now writes through
/// <see cref="FieldValueSetterCache{TModel}"/> instead, mirroring this file's read-side coverage
/// below.
/// </item>
/// </list>
/// Neither is touched by this fix.
/// </remarks>
public class CustomTemplateTests : MudBlazorTestBase
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
        var custom = component.Find(".my-custom-template");
        custom.TextContent.ShouldBe("Custom: John");
    }

    [Fact]
    public void WithCustomTemplate_Should_Receive_Typed_Configuration()
    {
        // Arrange - the template context must expose the typed field configuration
        var model = new TestModel { Name = "John" };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Display Name")
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "template-label");
                    builder.AddContent(2, context.Configuration.Label);
                    builder.CloseElement();
                }))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find(".template-label").TextContent.ShouldBe("Display Name");
    }

    [Fact]
    public async Task WithCustomTemplate_ValueChanged_Should_Update_Model()
    {
        // Arrange - templates must be able to push values back into the model
        var model = new TestModel();
        IFieldContext<TestModel, string>? captured = null;
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithCustomTemplate(context => builder =>
                {
                    captured = context;
                    builder.AddContent(0, "template");
                }))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        captured.ShouldNotBeNull();

        // Act
        await component.InvokeAsync(() => captured!.ValueChanged.InvokeAsync("Jane"));

        // Assert
        model.Name.ShouldBe("Jane");
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

    [Fact]
    public void WithCustomTemplate_Should_Report_Once_And_Still_Render_When_The_Bound_Path_Is_Unreachable()
    {
        // Arrange - Nested is null, so evaluating x.Nested.Value against this model throws a
        // NullReferenceException. The field must still render (with no value) instead of the
        // exception reaching the render pipeline or the field disappearing, and the failure must be
        // reported once (#330), not once per render.
        var logs = new CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new NestedPropertyModel { Nested = null };
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
        component.Render();
        component.Render();

        // Assert - the template still renders, with no value rather than a crash...
        component.Find(".nested-template").TextContent.ShouldBe("Custom: ");

        // ...and the diagnostic fired exactly once, however many times the form re-renders.
        var warnings = logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Nested value");
        warnings[0].ShouldContain(nameof(NullReferenceException));
    }

    [Fact]
    public void WithCustomTemplate_Should_Not_Crash_For_A_Value_Typed_Field_When_Unresolvable()
    {
        // Arrange - the fallback path returns null from GetCustomTemplateValue for ANY TValue,
        // including a value type like int, where a naive cast would throw. FieldConfigurationWrapper
        // hands the object-typed value to the typed template through a pattern match
        // (`_objectContext.Value is TValue typed ? typed : default!`), not a direct cast, so a null
        // becomes the type's default (0 for int) rather than a NullReferenceException. Pinning this
        // because the diagnostic's whole point is "still renders" - it must hold for value types too.
        var model = new NestedIntModel { Nested = null };
        var config = FormBuilder<NestedIntModel>
            .Create()
            .AddField(x => x.Nested!.Value, field => field
                .WithLabel("Nested int")
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "int-template");
                    builder.AddContent(2, $"Int: {context.Value}");
                    builder.CloseElement();
                }))
            .Build();

        // Act
        var component = Render<FormCraftComponent<NestedIntModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.Find(".int-template").TextContent.ShouldBe("Int: 0");
    }

    [Fact]
    public void WithCustomTemplate_Should_Report_Each_Field_Separately_Even_When_FieldNames_Collide()
    {
        // Arrange - FieldName is only the expression's last member, so x => x.A.Value and
        // x => x.B.Value both report FieldName "Value" even though they are two unrelated fields.
        // Latching on FieldName alone would let whichever field fails first silently swallow the
        // other's warning forever (review finding on #330's diagnostic).
        var logs = new CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new TwoNestedPathsModel { A = null, B = null };
        var config = FormBuilder<TwoNestedPathsModel>
            .Create()
            .AddField(x => x.A!.Value, field => field
                .WithLabel("Field A")
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "field-a");
                    builder.AddContent(2, $"A: {context.Value}");
                    builder.CloseElement();
                }))
            .AddField(x => x.B!.Value, field => field
                .WithLabel("Field B")
                .WithCustomTemplate(context => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "field-b");
                    builder.AddContent(2, $"B: {context.Value}");
                    builder.CloseElement();
                }))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TwoNestedPathsModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - both fields still render...
        component.Find(".field-a").TextContent.ShouldBe("A: ");
        component.Find(".field-b").TextContent.ShouldBe("B: ");

        // ...and BOTH get their own diagnostic, not just whichever failed first.
        var warnings = logs.Warnings;
        warnings.Count.ShouldBe(2);
        warnings.ShouldContain(w => w.Contains("Field A"));
        warnings.ShouldContain(w => w.Contains("Field B"));
    }

    [Fact]
    public async Task WithCustomTemplate_ValueChanged_Should_Update_A_Nested_Property()
    {
        // Arrange - the write-back counterpart to Should_Render_When_Bound_To_A_Nested_Property
        // (#396). Before this fix the write silently no-op'd: typeof(TModel).GetProperty("Value")
        // never resolves against NestedPropertyModel, only against NestedValue.
        var model = new NestedPropertyModel { Nested = new NestedValue { Value = "deep" } };
        IFieldContext<NestedPropertyModel, string>? captured = null;
        var config = FormBuilder<NestedPropertyModel>
            .Create()
            .AddField(x => x.Nested!.Value, field => field
                .WithLabel("Nested value")
                .WithCustomTemplate(context =>
                {
                    captured = context;
                    return builder => builder.AddContent(0, "template");
                }))
            .Build();

        var component = Render<FormCraftComponent<NestedPropertyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        captured.ShouldNotBeNull();

        // Act
        await component.InvokeAsync(() => captured!.ValueChanged.InvokeAsync("shallow"));

        // Assert
        model.Nested.ShouldNotBeNull();
        model.Nested!.Value.ShouldBe("shallow");
    }

    [Fact]
    public async Task WithCustomTemplate_ValueChanged_Should_Not_Throw_And_Should_Warn_Once_When_The_Bound_Path_Is_Unreachable()
    {
        // Arrange - Nested is null, so the write throws the same NullReferenceException the read
        // side already throws (#330). UpdateFieldValue must catch it, report it through the same
        // per-field latch, and never let it escape this event callback (#396). The template body
        // reads context.Value, so the initial render already reads (and fails to read) the value -
        // that consumes the latch before ValueChanged ever fires, and the write must not add a
        // second warning for the same binding.
        var logs = new CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new NestedPropertyModel { Nested = null };
        IFieldContext<NestedPropertyModel, string>? captured = null;
        var config = FormBuilder<NestedPropertyModel>
            .Create()
            .AddField(x => x.Nested!.Value, field => field
                .WithLabel("Nested value")
                .WithCustomTemplate(context =>
                {
                    captured = context;
                    return builder => builder.AddContent(0, $"Custom: {context.Value}");
                }))
            .Build();

        var component = Render<FormCraftComponent<NestedPropertyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        captured.ShouldNotBeNull();

        // The read already fired (and warned) during the render above.
        logs.Warnings.Count.ShouldBe(1);

        // Act - must not throw.
        await component.InvokeAsync(() => captured!.ValueChanged.InvokeAsync("unreachable"));

        // Assert - still exactly one warning total for this binding: the write's own failure hits
        // the same latch slot the read already consumed, so it logs nothing new.
        var warnings = logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Nested value");
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

    private class NestedIntModel
    {
        public NestedIntValue? Nested { get; set; }
    }

    private class NestedIntValue
    {
        public int Value { get; set; }
    }

    private class TwoNestedPathsModel
    {
        public NestedValue? A { get; set; }
        public NestedValue? B { get; set; }
    }
}
