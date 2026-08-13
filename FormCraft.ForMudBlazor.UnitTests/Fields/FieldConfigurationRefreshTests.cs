using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// A field component must render the configuration of the field it is <i>currently</i> showing (#298).
/// </summary>
/// <remarks>
/// <para>
/// Every component in this package used to read its configuration once, in <c>OnInitialized</c>, and
/// never look at it again. Blazor reuses a component instance whenever the render-tree shape matches,
/// so an instance could be handed a different <c>Context</c> while those cached attributes still
/// described the field it was first rendered for — and it would go on rendering the old field's mask,
/// adornment and input type indefinitely.
/// </para>
/// <para>
/// The failure is silent and the output looks plausible, which is why it survived so long: nothing
/// throws, nothing logs, the field just quietly shows the wrong thing. #283 made it louder by wiring
/// a diagnostic to the same cached data, so a stale mask could produce a warning naming a pattern the
/// form does not apply.
/// </para>
/// </remarks>
public class FieldConfigurationRefreshTests : MudBlazorTestBase
{
    /// <summary>
    /// The assumption the whole fix rests on: <c>Context.Field</c> is the same object across renders.
    /// </summary>
    /// <remarks>
    /// The refresh is guarded on field <i>identity</i>, compared by reference, so this has to hold or
    /// the guard either never fires (stale for ever) or always fires (re-reading every attribute on
    /// every keystroke, which is what the guard exists to avoid).
    /// <para>
    /// It holds for a specific reason worth recording: <c>FieldRendererService.RenderField</c>
    /// allocates a fresh <c>FieldRenderContext</c> per render — so the <b>context</b> is not stable —
    /// but it fills that context's <c>Field</c> from the built configuration, which
    /// <c>FormBuilder.Build()</c> makes immutable and hands out by reference. #269 relies on exactly
    /// the same property, keying its compiled-getter <c>ConditionalWeakTable</c> on the field object.
    /// </para>
    /// </remarks>
    [Fact]
    public void Context_Field_Should_Be_The_Same_Instance_Across_Renders()
    {
        // Arrange
        var model = new TestModel { Phone = "5551234567" };
        var config = MaskedConfiguration("(000) 000-0000");

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var first = component.FindComponent<MudBlazorTextFieldComponent<TestModel>>().Instance.Context.Field;

        // Act
        component.Render();
        component.Render();

        // Assert - the context object may be new each time; the FIELD must not be.
        var second = component.FindComponent<MudBlazorTextFieldComponent<TestModel>>().Instance.Context.Field;
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    /// <summary>
    /// A different field arriving on the same instance re-reads that field's configuration.
    /// </summary>
    /// <remarks>
    /// The headline case: a wizard step, a mode toggle, anything that renders a different form over
    /// the same component tree. Both configurations declare a field called <c>Phone</c> at the same
    /// position, so Blazor reuses the component — and before #298 the mask stayed on the pattern the
    /// first configuration declared.
    /// <para>
    /// Asserted on the mask MudBlazor is actually bound, not on FormCraft's own property, because the
    /// property being right while the binding is stale is precisely the shape of this bug.
    /// </para>
    /// </remarks>
    [Fact]
    public void TextField_Should_Rebind_Its_Mask_When_The_Configuration_Is_Swapped()
    {
        // Arrange
        var model = new TestModel { Phone = "5551234567" };

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, MaskedConfiguration("(000) 000-0000")));

        component.FindComponent<MudTextField<string>>().Instance.Mask!.Mask.ShouldBe("(000) 000-0000");

        // Act - same model, same field name, different configuration object.
        component.Render(parameters => parameters
            .Add(p => p.Configuration, MaskedConfiguration("0000-0000")));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Mask!.Mask.ShouldBe("0000-0000");
    }

    /// <summary>
    /// Dropping a mask entirely is honoured too, not just changing its pattern.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The complement of the test above, and the one that catches a fix that only ever
    /// <i>overwrites</i> cached values. A reload that assigns each attribute it finds leaves the
    /// previous field's value in place for every attribute the new field does not declare — so a
    /// field that removed its mask would keep masking. That is why
    /// <c>OnFieldConfigurationChanged</c> is documented as a reload, not a patch.
    /// </para>
    /// <para>
    /// ⛔ <b>Asserted on what renders, never on <c>MudTextField.Mask</c>.</b> Measured on 9.8.0: after
    /// the swap that property still returns the old <c>PatternMask</c> even though the field has
    /// correctly stopped masking — MudBlazor retains the object while its rendering moves on. The
    /// same trap the repo already documents for <c>MudFileUpload.Error</c>/<c>ErrorText</c>: assert
    /// the rendered DOM, or the test proves nothing. <c>MudTextField</c> renders a <c>MudMask</c>
    /// instead of its usual input exactly when a mask is in force, so counting those is the honest
    /// question.
    /// </para>
    /// </remarks>
    [Fact]
    public void TextField_Should_Drop_Its_Mask_When_The_New_Configuration_Has_None()
    {
        // Arrange
        var model = new TestModel { Phone = "5551234567" };

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, MaskedConfiguration("(000) 000-0000")));

        component.FindComponents<MudMask>().Count.ShouldBe(1);

        // Act - the replacement field declares no mask at all.
        var unmasked = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field.WithLabel("Phone"))
            .Build();

        component.Render(parameters => parameters.Add(p => p.Configuration, unmasked));

        // Assert - FormCraft stopped caching the old pattern, and the field stopped masking.
        component.FindComponent<MudBlazorTextFieldComponent<TestModel>>().Instance.Mask.ShouldBeNull();
        component.FindComponents<MudMask>().ShouldBeEmpty();
    }

    /// <summary>
    /// The refresh is guarded — an ordinary re-render must not re-read anything.
    /// </summary>
    /// <remarks>
    /// Without the identity guard the fix degenerates into "re-read every attribute on every
    /// <c>OnParametersSet</c>", which runs on every keystroke (<c>Immediate="true"</c>) and costs a
    /// dictionary lookup plus a type test per attribute. Counted through the masked-lines diagnostic,
    /// which is emitted from the configuration-loading path and is latched only per field — so a
    /// second emission means the path ran a second time for the same field.
    /// </remarks>
    [Fact]
    public void Rerendering_The_Same_Field_Should_Not_Reload_Its_Configuration()
    {
        // Arrange - a masked multi-line password field trips MaskedLinesDiagnostic exactly once.
        var logs = new TestSupport.CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .AsPassword()
                .AsTextArea(lines: 4))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, config));

        logs.Warnings.Count.ShouldBe(1);

        // Act
        component.Render();
        component.Render();

        // Assert - still one: the configuration did not change, so it was not re-read.
        logs.Warnings.Count.ShouldBe(1);
    }

    private static IFormConfiguration<TestModel> MaskedConfiguration(string pattern) =>
        FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithAttribute("Mask", pattern))
            .Build();

    private class TestModel
    {
        public string Phone { get; set; } = string.Empty;
    }
}
