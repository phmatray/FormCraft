using Microsoft.Extensions.Logging;

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
/// <c>UpdateFieldValue</c> (the write-back). Was explicitly out of scope per #330's Non-goals —
/// read side only — leaving it with the identical top-level-only lookup and the same asymmetric
/// silent-no-op failure mode. Fixed by #396: it now writes through
/// <see cref="FieldValueSetterCache{TModel}"/> instead, mirroring this file's read-side coverage
/// below.
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
    public async Task WithCustomTemplate_ValueChanged_Should_Update_Model()
    {
        // Arrange - templates must be able to push values back into the model. Mirrors the
        // MudBlazor adapter's identical top-level regression test.
        var model = new TestModel();
        IFieldContext<TestModel, string>? captured = null;
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithCustomTemplate(context =>
                {
                    captured = context;
                    return builder => builder.AddContent(0, "template");
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
        // per-field latch, and never let it escape this event callback (#396). The initial render
        // already reads (and fails to read) the value, so the latch is consumed before ValueChanged
        // ever fires - the write must not add a second warning for the same binding.
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
                    return builder => builder.AddContent(0, "template");
                }))
            .Build();

        var component = Render<FormCraftComponent<NestedPropertyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        captured.ShouldNotBeNull();

        // Act - must not throw.
        await component.InvokeAsync(() => captured!.ValueChanged.InvokeAsync("unreachable"));

        // Assert - exactly one warning total for this binding (the read already warned once).
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

    private class TwoNestedPathsModel
    {
        public NestedValue? A { get; set; }
        public NestedValue? B { get; set; }
    }

    /// <summary>
    /// Collects warning-level log messages so a diagnostic can be asserted on. This adapter has no
    /// shared diagnostics infrastructure (unlike <c>FormCraft.ForMudBlazor</c>'s
    /// <c>DiagnosticLog</c>/<c>CapturingLoggerProvider</c>) and this is the only test needing one, so
    /// it stays local rather than starting a new shared TestSupport type for a single call site.
    /// </summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _warnings = [];

        public IReadOnlyList<string> Warnings
        {
            get
            {
                lock (_warnings)
                {
                    return _warnings.ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_warnings);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<string> warnings) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (logLevel < LogLevel.Warning)
                {
                    return;
                }

                lock (warnings)
                {
                    warnings.Add(formatter(state, exception));
                }
            }
        }
    }
}
