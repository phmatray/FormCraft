using FormCraft.ForFluentUI.Extensions;
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
    /// A numeric field rebinds its <c>Min</c> when a different field declares another one (#335).
    /// </summary>
    /// <remarks>
    /// The numeric component now binds <c>Min</c>/<c>Max</c>/<c>Step</c> as real parameters,
    /// assigned unconditionally every reload (#348) — see
    /// <see cref="Bound_FluentNumberInput_Min_Should_Be_A_Non_Nullable_TValue"/> for why a splatted
    /// dictionary could not express "unset" in the first place.
    /// </remarks>
    [Fact]
    public void NumericField_Should_Rebind_Its_Min_When_The_Configuration_Is_Swapped()
    {
        // Arrange
        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel())
            .Add(p => p.Configuration, BoundedConfiguration(5)));

        component.FindComponent<FluentNumberInput<int>>().Instance.Min.ShouldBe(5);

        // Act
        component.Render(parameters => parameters
            .Add(p => p.Configuration, BoundedConfiguration(9)));

        // Assert
        component.FindComponent<FluentNumberInput<int>>().Instance.Min.ShouldBe(9);
    }

    /// <summary>
    /// Characterisation (#348): <c>FluentNumberInput&lt;int&gt;.Min</c>/<c>Max</c>/<c>Step</c> are
    /// typed <c>TValue</c>, not <c>TValue?</c> — there is no value that means "unset" for a
    /// non-nullable numeric type, so "no bound configured" can only be expressed by supplying
    /// Fluent's own per-type default explicitly, never by leaving the parameter unassigned.
    /// </summary>
    /// <remarks>
    /// Pins the spelling of "unset" this fix relies on (Task 1 Step 4): reflection over Fluent's own
    /// declared parameter types, plus the values its constructor assigns before any FormCraft
    /// parameter is applied (decompiled under #348).
    /// </remarks>
    [Fact]
    public void Bound_FluentNumberInput_Min_Should_Be_A_Non_Nullable_TValue()
    {
        // Assert - the declared parameter type leaves no "unset" value to fall back on
        typeof(FluentNumberInput<int>).GetProperty("Min")!.PropertyType.ShouldBe(typeof(int));
        typeof(FluentNumberInput<int>).GetProperty("Max")!.PropertyType.ShouldBe(typeof(int));
        typeof(FluentNumberInput<int>).GetProperty("Step")!.PropertyType.ShouldBe(typeof(int));

        // Arrange / Act - an unconfigured field renders Fluent's own defaults, not default(int)
        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel())
            .Add(p => p.Configuration, UnboundedConfiguration()));

        // Assert
        var input = component.FindComponent<FluentNumberInput<int>>().Instance;
        input.Min.ShouldBe(int.MinValue);
        input.Max.ShouldBe(int.MaxValue);
        input.Step.ShouldBe(1);
    }

    /// <summary>
    /// The drop case #335 could not fix (#348): a numeric field that declares no bound must render
    /// unbounded even on a component instance that previously rendered a field which did.
    /// </summary>
    /// <remarks>
    /// Blazor retains a component parameter that a later render stops supplying, so before this fix
    /// <c>FluentNumberInput.Min</c> kept the previous field's <c>5</c> forever, even though
    /// FormCraft's own splatted dictionary was correctly empty.
    /// </remarks>
    [Fact]
    public void NumericField_Should_Render_Unbounded_When_The_New_Configuration_Drops_Min()
    {
        // Arrange
        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel())
            .Add(p => p.Configuration, BoundedConfiguration(5)));

        component.FindComponent<FluentNumberInput<int>>().Instance.Min.ShouldBe(5);

        // Act - the replacement field declares no Min at all.
        component.Render(parameters => parameters
            .Add(p => p.Configuration, UnboundedConfiguration()));

        // Assert
        component.FindComponent<FluentNumberInput<int>>().Instance.Min.ShouldBe(int.MinValue);
    }

    /// <summary>
    /// The nullable component's own copy of the drop case (#348) — <c>FluentNumberInput&lt;int?&gt;</c>
    /// rather than <c>FluentNumberInput&lt;int&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Targeted at <c>Max</c> specifically, per this component's own doc comment: a regression that
    /// let a dropped <c>Max</c> reach <c>FluentNumberInput</c> as <c>null</c> (for example
    /// <c>Max="@Max"</c> without the <c>?? TypeMaxValue</c> fallback, since the parameter is already
    /// nullable here and it would still compile) would silently clamp every entered value to
    /// <c>null</c> rather than leaving the field unbounded — a failure this suite would otherwise
    /// only catch through the doc comment's own reasoning, never through a run.
    /// </remarks>
    [Fact]
    public void NullableNumericField_Should_Render_Unbounded_When_The_New_Configuration_Drops_Max()
    {
        // Arrange
        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel())
            .Add(p => p.Configuration, NullableBoundedConfiguration(50)));

        component.FindComponent<FluentNumberInput<int?>>().Instance.Max.ShouldBe(50);

        // Act - the replacement field declares no Max at all.
        component.Render(parameters => parameters
            .Add(p => p.Configuration, NullableUnboundedConfiguration()));

        // Assert - unbounded, not clamped to null
        component.FindComponent<FluentNumberInput<int?>>().Instance.Max.ShouldBe(int.MaxValue);
    }

    /// <summary>
    /// A lookup keeps showing its stored value after a configuration swap (#335).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The regression test for a fix that was almost worse than the bug. The first attempt reset
    /// <c>_displayText</c> to empty in the hook — correct for staleness, and catastrophic on its own,
    /// because nothing else in this component repopulates it from the model. The MudBlazor lookup
    /// gets away with clearing because its <c>OnParametersSet</c> calls <c>UpdateDisplayText()</c> on
    /// every render and repairs the blank on the same pass; the Fluent one has no such call, so a
    /// field with a perfectly good stored value rendered empty for ever.
    /// </para>
    /// <para>
    /// The hook now re-derives the text rather than clearing it, which is what "reload, not patch"
    /// means when the property is derived rather than read.
    /// </para>
    /// </remarks>
    [Fact]
    public void LookupField_Should_Keep_Displaying_Its_Value_After_A_Configuration_Swap()
    {
        // Arrange
        var model = new TripModel { CityId = 7 };

        var component = Render<FormCraftComponent<TripModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, LookupConfiguration()));

        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("7");

        // Act - a different configuration object declaring the same lookup field.
        component.Render(parameters => parameters
            .Add(p => p.Configuration, LookupConfiguration()));

        // Assert - the display still reflects the model, rather than having been blanked.
        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("7");
    }

    private static IFormConfiguration<TripModel> LookupConfiguration() =>
        FormBuilder<TripModel>
            .Create()
            .AddField(x => x.CityId, field =>
            {
                field.WithLabel("City");

                // Called as a static method rather than as an extension on purpose: this project
                // references BOTH adapters, and the MudBlazor package publishes an .AsLookup(...) of
                // the same name into namespace FormCraft, so the extension form would be
                // CS0121-ambiguous here. Same reasoning as FluentUILookupFieldComponentTests.
                FluentUIFieldBuilderExtensions.AsLookup<TripModel, int, City>(
                    field,
                    dataProvider: _ => Task.FromResult(new LookupResult<City>
                    {
                        Items = [new City(7, "Lisbon")],
                        TotalCount = 1,
                    }),
                    valueSelector: c => c.Id,
                    displaySelector: c => c.Name,
                    configureColumns: cols =>
                        cols.Add(new LookupColumn<City> { Title = "Name", ValueSelector = c => c.Name }));
            })
            .Build();

    private class TripModel
    {
        public int CityId { get; set; }
    }

    private record City(int Id, string Name);

    private static IFormConfiguration<NumericModel> BoundedConfiguration(int min) =>
        FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Amount, field => field
                .WithLabel("Amount")
                .WithAttribute("Min", (int?)min))
            .Build();

    private static IFormConfiguration<NumericModel> UnboundedConfiguration() =>
        FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Amount, field => field.WithLabel("Amount"))
            .Build();

    private static IFormConfiguration<NumericModel> NullableBoundedConfiguration(int max) =>
        FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.OptionalAmount, field => field
                .WithLabel("Optional Amount")
                .WithAttribute("Max", (int?)max))
            .Build();

    private static IFormConfiguration<NumericModel> NullableUnboundedConfiguration() =>
        FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.OptionalAmount, field => field.WithLabel("Optional Amount"))
            .Build();

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

        public int? OptionalAmount { get; set; }
    }
}
