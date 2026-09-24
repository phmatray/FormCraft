using FormCraft.ForFluentUI.Extensions;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// One coverage row per Fluent UI field-type component, proving each one honours the
/// configuration-refresh contract (<c>OnFieldConfigurationChanged</c>, #335) — plus a build-failing
/// guard (#349) so a component added without a row here, or without an explicit "nothing to refresh"
/// exemption, cannot go uncovered silently. Mirrors
/// <c>FormCraft.ForMudBlazor.UnitTests.Fields.FieldConfigurationParityTests</c> deliberately.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FieldConfigurationRefreshTests"/> already proves the hook exists and pins several
/// components in depth (text, numeric, lookup) — this suite does not re-pin those in the same
/// depth. Its job is completeness: every component gets a row here, or is named as exempt.
/// </para>
/// <para>
/// <b>Genuine exemptions differ from the MudBlazor side.</b> The three Fluent date components bind
/// no configuration to their pickers at all (the spec's own example), and the single-file upload
/// component exposes its UPLOAD CONSTRAINTS (accepted types, max size, max count) as computed
/// getters re-evaluated on every access — those never cache anything the hook could leave stale.
/// MudBlazor's file-upload components DO cache real properties and are covered rows there.
/// <c>FluentUIMultipleFileUploadComponent</c> shares that same constraint guarantee but also carries
/// <c>TooManyFilesError</c>, state derived from a SELECTION rather than from the configuration — this
/// suite used to claim (wrongly) that it could not go stale; it is now a covered row (#416).
/// </para>
/// </remarks>
public class FieldConfigurationParityTests : FluentUITestBase
{
    // -----------------------------------------------------------------------------------------
    // Task 3 (mirrors Task 1) — bound-attribute rows.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TextField_Row()
    {
        var model = new TextModel();
        var component = Render<FormCraftComponent<TextModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, TextConfig("email")));

        component.FindComponent<FluentTextInput>().Instance.TextInputType.ShouldBe(TextInputType.Email);

        component.Render(parameters => parameters.Add(p => p.Configuration, TextConfig("tel")));

        component.FindComponent<FluentTextInput>().Instance.TextInputType.ShouldBe(TextInputType.Telephone);
    }

    [Fact]
    public void NumericField_Row()
    {
        var model = new NumericModel();
        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, NumericConfig(step: 1)));

        component.FindComponent<FluentNumberInput<int>>().Instance.Step.ShouldBe(1);

        component.Render(parameters => parameters.Add(p => p.Configuration, NumericConfig(step: 5)));

        component.FindComponent<FluentNumberInput<int>>().Instance.Step.ShouldBe(5);
    }

    [Fact]
    public void NullableNumericField_Row()
    {
        var model = new NullableNumericModel();
        var component = Render<FormCraftComponent<NullableNumericModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, NullableNumericConfig(max: 50)));

        component.FindComponent<FluentNumberInput<int?>>().Instance.Max.ShouldBe(50);

        component.Render(parameters => parameters.Add(p => p.Configuration, NullableNumericConfig(max: 80)));

        component.FindComponent<FluentNumberInput<int?>>().Instance.Max.ShouldBe(80);
    }

    [Fact]
    public void BooleanField_Row()
    {
        var model = new BooleanModel();
        var component = Render<FormCraftComponent<BooleanModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BooleanConfig(BooleanDisplayStyle.Checkbox)));

        component.FindComponents<FluentCheckbox>().Count.ShouldBe(1);
        component.FindComponents<FluentSwitch>().ShouldBeEmpty();

        component.Render(parameters => parameters
            .Add(p => p.Configuration, BooleanConfig(BooleanDisplayStyle.Switch)));

        component.FindComponents<FluentSwitch>().Count.ShouldBe(1);
        component.FindComponents<FluentCheckbox>().ShouldBeEmpty();
    }

    [Fact]
    public void SelectField_Row()
    {
        var model = new SelectModel();
        var config = FormBuilder<SelectModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithOptions(("a", "A"), ("b", "B")))
            .Build();

        var component = Render<FormCraftComponent<SelectModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance.Items!.Count().ShouldBe(2);

        component.Render(parameters => parameters.Add(p => p.Configuration, FormBuilder<SelectModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithOptions(("z", "Z")))
            .Build()));

        component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance.Items!
            .Select(o => o.Value).ShouldBe(["z"]);
    }

    [Fact]
    public void MultiSelectField_Row()
    {
        var model = new MultiSelectModel();
        var config = FormBuilder<MultiSelectModel>
            .Create()
            .AddField(x => x.Values, field => field.WithLabel("Values").AsMultiSelect(("a", "A"), ("b", "B")))
            .Build();

        var component = Render<FormCraftComponent<MultiSelectModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance.Items!.Count().ShouldBe(2);

        component.Render(parameters => parameters.Add(p => p.Configuration, FormBuilder<MultiSelectModel>
            .Create()
            .AddField(x => x.Values, field => field.WithLabel("Values").AsMultiSelect(("x", "X")))
            .Build()));

        component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance.Items!
            .Select(o => o.Value).ShouldBe(["x"]);
    }

    /// <summary>
    /// The Fluent half of #336's autocomplete miss: <c>_selectedOption</c>'s label is derived
    /// through <c>AutocompleteToStringFunc</c>, so a config swap that changes only that function
    /// must change the rendered label even though the underlying value did not change.
    /// </summary>
    [Fact]
    public void AutocompleteField_Row()
    {
        var model = new AutocompleteModel { Value = "abc" };
        var component = Render<FormCraftComponent<AutocompleteModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, AutocompleteConfig(v => v)));

        component.FindComponent<FluentAutocomplete<SelectOption<string>, string>>()
            .Instance.SelectedItem.ShouldNotBeNull().Label.ShouldBe("abc");

        // Act - a DIFFERENT configuration for the same field, with a different display function.
        component.Render(parameters => parameters
            .Add(p => p.Configuration, AutocompleteConfig(v => v.ToUpperInvariant())));

        component.FindComponent<FluentAutocomplete<SelectOption<string>, string>>()
            .Instance.SelectedItem.ShouldNotBeNull().Label.ShouldBe("ABC");
    }

    // -----------------------------------------------------------------------------------------
    // Task 3 (mirrors Task 2) — derived-state rows.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// The issue's own headline case for this adapter: an earlier fix cleared
    /// <c>_displayText</c> in the hook with nothing to repopulate it, leaving a valid stored value
    /// blank forever (#335). A different configuration object for the same field must keep showing
    /// the model's current value - and the picker opened for the PREVIOUS field must not survive
    /// the swap either, or a click on it hands the new field's selectors the old field's rows.
    /// </summary>
    [Fact]
    public async Task LookupField_Row_Keeps_Its_Display_Text_After_A_Configuration_Swap()
    {
        var model = new LookupModel { CityId = 7 };
        var component = Render<FormCraftComponent<LookupModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, LookupConfig()));

        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("7");

        // Act - open the picker through its own control. _isOpen and _rows belong to the field
        // that opened them, and must not outlive a swap to a different field's configuration.
        await component.Find("[data-testid=formcraft-lookup-open]").ClickAsync(new());
        component.FindAll("[data-testid=formcraft-lookup-panel]").ShouldNotBeEmpty();

        // Act - a DIFFERENT configuration object describing the same lookup field.
        component.Render(parameters => parameters.Add(p => p.Configuration, LookupConfig()));

        // Assert - the display text survives the swap, and the previous field's open picker does not.
        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("7");
        component.FindAll("[data-testid=formcraft-lookup-panel]").ShouldBeEmpty();
    }

    /// <summary>
    /// LOV's own miss in #336: <c>_selectedItems</c> leaked the previous field's selection into the
    /// new field's model. A row selected through the picker's own multi-select path must not survive
    /// a swap to a different configuration object for the same field.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Single-select cannot catch this.</b> This component's hook recomputes
    /// <c>DisplayText</c> unconditionally from <c>CurrentValue.ToString()</c> regardless of
    /// <c>_selectedItems</c>, so a dropped <c>_selectedItems.Clear()</c> is invisible there. It IS
    /// visible in multi-select: the chip list renders whenever <c>IsMultiSelect &amp;&amp;
    /// _selectedItems.Count &gt; 0</c>, so a stale, uncleared item renders as a leaked chip belonging
    /// to a field the user never selected anything on.
    /// </remarks>
    [Fact]
    public async Task LovField_Row_Keeps_Its_Display_Text_After_A_Configuration_Swap()
    {
        var model = new LovModel { CustomerId = 7 };
        var component = Render<FormCraftComponent<LovModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, LovConfig()));

        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("7");

        // Act - select a row through the picker's own control, the actual #336 failure mode.
        await component.Find("[data-testid=formcraft-lov-open]").ClickAsync(new());
        await component.Find("[data-testid=formcraft-lov-row]").ClickAsync(new());
        component.FindAll("[data-testid=formcraft-lov-chip]").Count.ShouldBe(1);

        // Act - a DIFFERENT configuration object describing the same LOV field.
        component.Render(parameters => parameters.Add(p => p.Configuration, LovConfig()));

        // Assert - the display text survives the swap, and the stale selection does not: a leaked
        // chip would mean the new field inherited a selection it never made.
        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("7");
        component.FindAll("[data-testid=formcraft-lov-chip]").ShouldBeEmpty();
    }

    /// <summary>
    /// This adapter's own #416: <c>TooManyFilesError</c> is derived from the last selection
    /// ATTEMPT rather than from the configuration, so - unlike the constraints on
    /// <c>FluentUIFileUploadComponentBase</c>, which are computed getters re-evaluated on every
    /// access - it needs its own reset in the hook or a stale error from the previous field keeps
    /// rendering after a configuration swap.
    /// </summary>
    [Fact]
    public async Task MultipleFileUploadField_Row_Clears_Its_Too_Many_Files_Error_After_A_Configuration_Swap()
    {
        var model = new MultipleFileUploadModel();
        var component = Render<FormCraftComponent<MultipleFileUploadModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, MultipleFileUploadConfig(maxFiles: 1)));

        // Act - select more files than the field allows.
        var tooManyFiles = new List<IBrowserFile> { A.Fake<IBrowserFile>(), A.Fake<IBrowserFile>() };
        var fileInput = component.FindComponent<FluentInputFile>();
        await component.InvokeAsync(() =>
            fileInput.Instance.OnInputFileChange.InvokeAsync(new InputFileChangeEventArgs(tooManyFiles)));

        component.FindAll("[data-testid=formcraft-upload-too-many]").ShouldNotBeEmpty();

        // Act - a DIFFERENT configuration object describing the same field.
        component.Render(parameters => parameters
            .Add(p => p.Configuration, MultipleFileUploadConfig(maxFiles: 1)));

        // Assert - the stale error from the previous field's selection attempt does not survive.
        component.FindAll("[data-testid=formcraft-upload-too-many]").ShouldBeEmpty();
    }

    // -----------------------------------------------------------------------------------------
    // Task 4 — the completeness guard.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Every field-type component with a row above. Kept as the single source of truth the guard
    /// reads, so the suite and the guard cannot drift apart.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Known ceiling:</b> this list is hand-maintained, not derived from the <c>[Fact]</c>
    /// methods above. The guard proves <c>universe ⊆ CoveredComponents ∪ ExemptComponents</c>; it does
    /// not prove <c>CoveredComponents</c> is exactly the set of types with a real row — a type added
    /// here without a matching row would pass silently. Keep the two in step by hand.
    /// </remarks>
    private static readonly IReadOnlySet<Type> CoveredComponents = new HashSet<Type>
    {
        typeof(FluentUITextFieldComponent<>),
        typeof(FluentUINumericFieldComponent<,>),
        typeof(FluentUINullableNumericFieldComponent<,>),
        typeof(FluentUIBooleanFieldComponent<>),
        typeof(FluentUISelectFieldComponent<,>),
        typeof(FluentUIMultiSelectFieldComponent<,>),
        typeof(FluentUIAutocompleteFieldComponent<,>),
        typeof(FluentUILookupFieldComponent<,>),
        typeof(FluentUILovFieldComponent<,,,>),
        typeof(FluentUIMultipleFileUploadComponent<>),
    };

    /// <summary>
    /// Field-type components that genuinely cache nothing derived from the field's configuration.
    /// Each entry names what makes that true.
    /// </summary>
    private static readonly IReadOnlySet<Type> ExemptComponents = new HashSet<Type>
    {
        // The three date components bind no Min/Max/Format to their pickers at all - confirmed by
        // reading both the .razor markup (no such parameters are wired) and the .razor.cs (no
        // OnFieldConfigurationChanged override, no GetAttribute call anywhere). This is the spec's
        // own worked example of a legitimate "nothing to refresh" row.
        typeof(FluentUIDateOnlyFieldComponent<>),
        typeof(FluentUIDateTimeFieldComponent<>),
        typeof(FluentUITimeOnlyFieldComponent<>),

        // The single-file upload component exposes its constraints (accepted types, max size, max
        // count) as COMPUTED GETTERS on FluentUIFileUploadComponentBase, re-evaluating
        // UploadConstraintResolver against Context.Field live on every access - never cached into a
        // field the hook would need to reset, and it sets no error/message field of its own either.
        // Unlike MudBlazor's upload components (covered rows there), it cannot go stale by
        // construction. FluentUIMultipleFileUploadComponent<> shares this same base class but is NOT
        // exempt: it also carries TooManyFilesError, state derived from a selection rather than from
        // the configuration, which is now covered by
        // MultipleFileUploadField_Row_Clears_Its_Too_Many_Files_Error_After_A_Configuration_Swap
        // (#416).
        typeof(FluentUIFileUploadFieldComponent<>),

        // ColorPicker reads no attribute at all.
        typeof(FluentUIColorPickerComponent<>),

        // Rating's MaxValue and Slider's Min/Max/Step are all `=>` computed properties, re-evaluated
        // fresh on every render - never cached into a field.
        typeof(FluentUIRatingComponent<>),
        typeof(FluentUISliderComponent<>),
    };

    [Fact]
    public void Every_Field_Component_Should_Have_A_Parity_Row_Or_An_Explicit_Exemption()
    {
        var universe = FieldComponentCoverageGuard.FieldComponentTypes(
            typeof(FluentUITextFieldComponent<>).Assembly);

        var gaps = FieldComponentCoverageGuard.FindGaps(universe, CoveredComponents, ExemptComponents);

        gaps.ShouldBeEmpty(
            "These field components have neither a parity row in FieldConfigurationParityTests nor "
            + "an explicit exemption. Add a row proving they honour OnFieldConfigurationChanged, or "
            + "add them to ExemptComponents with a reason:\n  "
            + string.Join("\n  ", gaps.Select(g => g.ToString())));
    }

    /// <summary>
    /// The guard's own guard: withholding a real, currently-covered component must produce a gap.
    /// </summary>
    [Fact]
    public void The_Guard_Should_Flag_A_Component_Withheld_From_Coverage()
    {
        var universe = FieldComponentCoverageGuard.FieldComponentTypes(
            typeof(FluentUITextFieldComponent<>).Assembly);

        var withheld = CoveredComponents.Where(t => t != typeof(FluentUITextFieldComponent<>)).ToHashSet();

        var gaps = FieldComponentCoverageGuard.FindGaps(universe, withheld, ExemptComponents);

        gaps.ShouldContain(g => g.Component == typeof(FluentUITextFieldComponent<>));
    }

    // -----------------------------------------------------------------------------------------
    // Config builders and models, one pair per row.
    // -----------------------------------------------------------------------------------------

    private static IFormConfiguration<TextModel> TextConfig(string inputType) =>
        FormBuilder<TextModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithInputType(inputType))
            .Build();

    private static IFormConfiguration<NumericModel> NumericConfig(int step) =>
        FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithAttribute("Step", (int?)step))
            .Build();

    private static IFormConfiguration<NullableNumericModel> NullableNumericConfig(int max) =>
        FormBuilder<NullableNumericModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithAttribute("Max", (int?)max))
            .Build();

    private static IFormConfiguration<BooleanModel> BooleanConfig(BooleanDisplayStyle style) =>
        FormBuilder<BooleanModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithAttribute("DisplayStyle", style))
            .Build();

    private static IFormConfiguration<AutocompleteModel> AutocompleteConfig(Func<string, string> toStringFunc) =>
        FormBuilder<AutocompleteModel>
            .Create()
            .AddField(x => x.Value, field => field
                .WithLabel("Value")
                .AsAutocomplete(
                    searchFunc: (_, _) => Task.FromResult(Enumerable.Empty<SelectOption<string>>()),
                    toStringFunc: toStringFunc))
            .Build();

    private static IFormConfiguration<LookupModel> LookupConfig() =>
        FormBuilder<LookupModel>
            .Create()
            .AddField(x => x.CityId, field =>
            {
                field.WithLabel("City");

                // Static call, not the extension form: this test project references BOTH adapters,
                // and MudBlazor publishes an .AsLookup(...) of the same name into namespace
                // FormCraft, so the extension form is CS0121-ambiguous here (matches
                // FluentUILookupFieldComponentTests / FieldConfigurationRefreshTests).
                FluentUIFieldBuilderExtensions.AsLookup<LookupModel, int, LookupCity>(
                    field,
                    dataProvider: _ => Task.FromResult(new LookupResult<LookupCity>
                    {
                        Items = [new LookupCity(7, "Lisbon")],
                        TotalCount = 1,
                    }),
                    valueSelector: c => c.Id,
                    displaySelector: c => c.Name);
            })
            .Build();

    private static IFormConfiguration<LovModel> LovConfig() =>
        FormBuilder<LovModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .WithLabel("Customer")
                .AsLov<LovModel, int?, LovCustomer>(lov => lov
                    .WithKey(c => (int?)c.Id)
                    .WithDisplay(c => c.Name)
                    .WithDataSource(() => new List<LovCustomer> { new(7, "ACME") })
                    .AddColumn(c => c.Name, "Name")
                    // Multi-select, not because this field needs it, but because it is the only
                    // mode in which a dropped _selectedItems.Clear() is DOM-observable here - see
                    // the remark on LovField_Row_Keeps_Its_Display_Text_After_A_Configuration_Swap.
                    // ResolveSelectionValue()'s pattern-matched fallback (`values is TValue
                    // typedList`, else the first value) keeps a scalar int? field working
                    // correctly even in multi-select mode.
                    .AllowMultipleSelection()))
            .Build();

    private static IFormConfiguration<MultipleFileUploadModel> MultipleFileUploadConfig(int maxFiles) =>
        FormBuilder<MultipleFileUploadModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").AsMultipleFileUpload(maxFiles: maxFiles))
            .Build();

    private class TextModel
    {
        public string Value { get; set; } = string.Empty;
    }

    private class NumericModel
    {
        public int Value { get; set; }
    }

    private class NullableNumericModel
    {
        public int? Value { get; set; }
    }

    private class BooleanModel
    {
        public bool Value { get; set; }
    }

    private class SelectModel
    {
        public string Value { get; set; } = string.Empty;
    }

    private class MultiSelectModel
    {
        public IEnumerable<string>? Values { get; set; }
    }

    private class AutocompleteModel
    {
        public string Value { get; set; } = string.Empty;
    }

    private class LookupModel
    {
        public int CityId { get; set; }
    }

    private record LookupCity(int Id, string Name);

    private class LovModel
    {
        public int? CustomerId { get; set; }
    }

    private record LovCustomer(int Id, string Name);

    private class MultipleFileUploadModel
    {
        public IReadOnlyList<IBrowserFile>? Value { get; set; }
    }
}
