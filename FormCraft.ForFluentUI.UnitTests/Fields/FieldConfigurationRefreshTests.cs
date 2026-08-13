using FormCraft.ForFluentUI.UnitTests.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// A Fluent field component must render the configuration of the field it is <i>currently</i> showing
/// (#335).
/// </summary>
/// <remarks>
/// <para>
/// The same defect #298 fixed for MudBlazor, unfixed in this adapter: components read their
/// configuration once in <c>OnInitialized</c> and never look again, so an instance re-parameterised
/// with a different <c>Context</c> keeps rendering the previous field's settings. Blazor reuses a
/// component instance whenever the render-tree shape matches, which a swapped
/// <c>FormCraftComponent.Configuration</c> — a wizard step, a mode toggle — does routinely.
/// </para>
/// <para>
/// Mirrors <c>FormCraft.ForMudBlazor.UnitTests.Fields.FieldConfigurationRefreshTests</c> deliberately.
/// One behaviour implemented twice and drifting is this library's recurring defect (#146, #177, #184,
/// #190, #203, #279), and the fix for it — the hook on <c>FieldComponentBase</c> — is now shared, so
/// the coverage should be recognisably the same on both sides.
/// </para>
/// </remarks>
public class FieldConfigurationRefreshTests : FluentUITestBase
{
    /// <summary>
    /// The assumption the refresh rests on: <c>Context.Field</c> is the same object across renders.
    /// </summary>
    /// <remarks>
    /// Re-pinned here rather than assumed from the MudBlazor side. Both adapters go through
    /// <c>FieldRendererService.RenderField</c>, which allocates a fresh <c>FieldRenderContext</c> per
    /// render — so the <b>context</b> is not stable — but fills its <c>Field</c> from the built
    /// configuration, which <c>FormBuilder.Build()</c> makes immutable and hands out by reference.
    /// The guard compares that reference, so it has to hold or the refresh either never fires or
    /// fires on every keystroke.
    /// </remarks>
    [Fact]
    public void Context_Field_Should_Be_The_Same_Instance_Across_Renders()
    {
        // Arrange
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, TextConfiguration("text")));

        var first = component.FindComponent<FluentUITextFieldComponent<TestModel>>().Instance.Context.Field;

        // Act
        component.Render();
        component.Render();

        // Assert
        var second = component.FindComponent<FluentUITextFieldComponent<TestModel>>().Instance.Context.Field;
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    /// <summary>
    /// A different field arriving on the same instance re-reads that field's configuration.
    /// </summary>
    /// <remarks>
    /// Both configurations declare a field called <c>Name</c> at the same position, so Blazor reuses
    /// the component — and the input type stayed on whatever the first configuration declared.
    /// </remarks>
    [Fact]
    public void TextField_Should_Rebind_Its_InputType_When_The_Configuration_Is_Swapped()
    {
        // Arrange
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, TextConfiguration("text")));

        component.FindComponent<FluentTextInput>().Instance.TextInputType
            .ShouldBe(TextInputType.Text);

        // Act
        component.Render(parameters => parameters
            .Add(p => p.Configuration, TextConfiguration("password")));

        // Assert
        component.FindComponent<FluentTextInput>().Instance.TextInputType
            .ShouldBe(TextInputType.Password);
    }

    /// <summary>
    /// An attribute the new field does not declare reverts to its default (#335).
    /// </summary>
    /// <remarks>
    /// The complement, and the one that catches a fix that only ever <i>overwrites</i>. A reload
    /// assigning each attribute it finds leaves the previous field's value in place for every
    /// attribute the new field omits — so a field that dropped <c>Lines</c> would keep rendering a
    /// text area.
    /// <para>
    /// Asserted on which component renders, not on a property: the razor picks
    /// <c>FluentTextArea</c> over <c>FluentTextInput</c> on <c>Lines &gt; 1</c>, so the rendered shape
    /// is the honest question.
    /// </para>
    /// </remarks>
    [Fact]
    public void TextField_Should_Revert_To_A_Single_Line_When_The_New_Configuration_Drops_Lines()
    {
        // Arrange
        var multiLine = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithAttribute("Lines", 4))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, multiLine));

        component.FindComponents<FluentTextArea>().Count.ShouldBe(1);

        // Act - the replacement field declares no Lines at all.
        component.Render(parameters => parameters
            .Add(p => p.Configuration, TextConfiguration("text")));

        // Assert
        component.FindComponents<FluentTextArea>().ShouldBeEmpty();
        component.FindComponents<FluentTextInput>().Count.ShouldBe(1);
    }

    /// <summary>
    /// A numeric field drops the previous field's <c>Min</c> rather than accumulating it (#335).
    /// </summary>
    /// <remarks>
    /// The numeric component collects <c>Min</c>/<c>Max</c>/<c>Step</c> into a
    /// <c>Dictionary&lt;string, object&gt;</c> through a helper that only ever <i>adds</i> when the
    /// attribute is configured. That is the patch-not-reload shape in its purest form: nothing removes
    /// a key, so on a reload the new field inherits every bound the old one declared, and the input
    /// silently constrains input the developer never asked it to.
    /// </remarks>
    [Fact]
    public void NumericField_Should_Drop_A_Bound_The_New_Configuration_Does_Not_Declare()
    {
        // Arrange
        var bounded = FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Amount, field => field
                .WithLabel("Amount")
                .WithAttribute("Min", 5))
            .Build();

        var unbounded = FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Amount, field => field.WithLabel("Amount"))
            .Build();

        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel())
            .Add(p => p.Configuration, bounded));

        component.Markup.ShouldContain("min");

        // Act
        component.Render(parameters => parameters.Add(p => p.Configuration, unbounded));

        // Assert - the bound belonged to a field that is no longer on screen.
        component.Markup.ShouldNotContain("min=");
    }

    private static IFormConfiguration<TestModel> TextConfiguration(string inputType) =>
        FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithAttribute("InputType", inputType))
            .Build();

    private class NumericModel
    {
        public int Amount { get; set; }
    }
}
