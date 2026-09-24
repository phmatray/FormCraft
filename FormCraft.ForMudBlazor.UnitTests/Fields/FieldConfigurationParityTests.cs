using FormCraft.ForMudBlazor.UnitTests.TestSupport;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// One coverage row per MudBlazor field-type component, proving each one honours the
/// configuration-refresh contract (<c>OnFieldConfigurationChanged</c>, #298) — plus a build-failing
/// guard (#349) so a component added without a row here, or without an explicit "nothing to refresh"
/// exemption, cannot go uncovered silently.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FieldConfigurationRefreshTests"/> already proves the hook exists and pins several
/// components in depth (text, numeric, boolean, select) — this suite does not re-pin those in the
/// same depth. Its job is completeness: every component gets a row here, or is named as exempt,
/// so a reviewer (or the build) can answer "does X have coverage" without searching two files. Rows
/// for types the sister suite already covers deliberately reuse the same swap shape rather than
/// inventing a second observable — the point is the row exists, not that it is novel.
/// </para>
/// <para>
/// <b>Context.Field's reference stability</b> — the assumption every swap-on-one-instance row
/// depends on — is pinned once, by
/// <see cref="FieldConfigurationRefreshTests.Context_Field_Should_Be_The_Same_Instance_Across_Renders"/>.
/// Not re-pinned here (per the issue's own Assumptions section).
/// </para>
/// </remarks>
public class FieldConfigurationParityTests : MudBlazorTestBase
{
    // -----------------------------------------------------------------------------------------
    // Task 1 — bound-attribute rows.
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void TextField_Row()
    {
        var model = new TextModel();
        var config = FormBuilder<TextModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithInputType("email"))
            .Build();

        var component = Render<FormCraftComponent<TextModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        component.FindComponent<MudTextField<string>>().Instance.InputType.ShouldBe(InputType.Email);

        component.Render(parameters => parameters.Add(p => p.Configuration, FormBuilder<TextModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithInputType("tel"))
            .Build()));

        component.FindComponent<MudTextField<string>>().Instance.InputType.ShouldBe(InputType.Telephone);
    }

    [Fact]
    public void NumericField_Row()
    {
        var model = new NumericModel();
        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, NumericConfig(step: 1)));

        component.FindComponent<MudNumericField<int>>().Instance.Step.ShouldBe(1);

        component.Render(parameters => parameters.Add(p => p.Configuration, NumericConfig(step: 5)));

        component.FindComponent<MudNumericField<int>>().Instance.Step.ShouldBe(5);
    }

    [Fact]
    public void NullableNumericField_Row()
    {
        var model = new NullableNumericModel();
        var component = Render<FormCraftComponent<NullableNumericModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, NullableNumericConfig(min: 5)));

        component.FindComponent<MudNumericField<int?>>().Instance.Min.ShouldBe(5);

        component.Render(parameters => parameters.Add(p => p.Configuration, NullableNumericConfig(min: 9)));

        component.FindComponent<MudNumericField<int?>>().Instance.Min.ShouldBe(9);
    }

    [Fact]
    public void BooleanField_Row()
    {
        var model = new BooleanModel();
        var component = Render<FormCraftComponent<BooleanModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BooleanConfig(BooleanDisplayStyle.Checkbox)));

        component.FindComponents<MudCheckBox<bool>>().Count.ShouldBe(1);
        component.FindComponents<MudSwitch<bool>>().ShouldBeEmpty();

        component.Render(parameters => parameters
            .Add(p => p.Configuration, BooleanConfig(BooleanDisplayStyle.Switch)));

        component.FindComponents<MudSwitch<bool>>().Count.ShouldBe(1);
        component.FindComponents<MudCheckBox<bool>>().ShouldBeEmpty();
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

        component.FindComponent<MudBlazorSelectFieldComponent<SelectModel, string>>()
            .Instance.Options.Count().ShouldBe(2);

        component.Render(parameters => parameters.Add(p => p.Configuration, FormBuilder<SelectModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithOptions(("z", "Z")))
            .Build()));

        component.FindComponent<MudBlazorSelectFieldComponent<SelectModel, string>>()
            .Instance.Options.Select(o => o.Value).ShouldBe(["z"]);
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

        component.FindComponent<MudBlazorMultiSelectFieldComponent<MultiSelectModel, string>>()
            .Instance.Options.Count().ShouldBe(2);

        component.Render(parameters => parameters.Add(p => p.Configuration, FormBuilder<MultiSelectModel>
            .Create()
            .AddField(x => x.Values, field => field.WithLabel("Values").AsMultiSelect(("x", "X")))
            .Build()));

        component.FindComponent<MudBlazorMultiSelectFieldComponent<MultiSelectModel, string>>()
            .Instance.Options.Select(o => o.Value).ShouldBe(["x"]);
    }

    [Fact]
    public void AutocompleteField_Row()
    {
        var model = new AutocompleteModel();
        var component = Render<FormCraftComponent<AutocompleteModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, AutocompleteConfig(debounceMs: 300)));

        component.FindComponent<MudAutocomplete<string>>().Instance.DebounceInterval.ShouldBe(300);

        component.Render(parameters => parameters.Add(p => p.Configuration, AutocompleteConfig(debounceMs: 600)));

        component.FindComponent<MudAutocomplete<string>>().Instance.DebounceInterval.ShouldBe(600);
    }

    [Fact]
    public void DateOnlyField_Row()
    {
        var model = new DateOnlyModel();
        var component = Render<FormCraftComponent<DateOnlyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, DateOnlyConfig("yyyy-MM-dd")));

        component.FindComponent<MudDatePicker>().Instance.DateFormat.ShouldBe("yyyy-MM-dd");

        component.Render(parameters => parameters.Add(p => p.Configuration, DateOnlyConfig("dd/MM/yyyy")));

        component.FindComponent<MudDatePicker>().Instance.DateFormat.ShouldBe("dd/MM/yyyy");
    }

    [Fact]
    public void DateTimeField_Row()
    {
        var min1 = new DateTime(2020, 1, 1);
        var min2 = new DateTime(2021, 6, 1);
        var model = new DateTimeModel();

        var component = Render<FormCraftComponent<DateTimeModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, DateTimeConfig(min1)));

        component.FindComponent<MudDatePicker>().Instance.MinDate.ShouldBe(min1);

        component.Render(parameters => parameters.Add(p => p.Configuration, DateTimeConfig(min2)));

        component.FindComponent<MudDatePicker>().Instance.MinDate.ShouldBe(min2);
    }

    [Fact]
    public void TimeOnlyField_Row()
    {
        var model = new TimeOnlyModel();
        var component = Render<FormCraftComponent<TimeOnlyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, TimeOnlyConfig(showClearButton: true)));

        component.FindComponent<MudTimePicker>().Instance.Clearable.ShouldBeTrue();

        component.Render(parameters => parameters
            .Add(p => p.Configuration, TimeOnlyConfig(showClearButton: false)));

        component.FindComponent<MudTimePicker>().Instance.Clearable.ShouldBeFalse();
    }

    [Fact]
    public void FileUploadField_Row()
    {
        var model = new FileUploadModel();
        var component = Render<FormCraftComponent<FileUploadModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, FileUploadConfig(maxFileSize: 1024)));

        component.FindComponent<MudBlazorFileUploadFieldComponent<FileUploadModel>>()
            .Instance.MaxFileSize.ShouldBe(1024);

        component.Render(parameters => parameters
            .Add(p => p.Configuration, FileUploadConfig(maxFileSize: 2048)));

        component.FindComponent<MudBlazorFileUploadFieldComponent<FileUploadModel>>()
            .Instance.MaxFileSize.ShouldBe(2048);
    }

    [Fact]
    public void MultipleFileUploadField_Row()
    {
        var model = new MultipleFileUploadModel();
        var component = Render<FormCraftComponent<MultipleFileUploadModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, MultipleFileUploadConfig(maxFiles: 3)));

        component.FindComponent<MudBlazorMultipleFileUploadComponent<MultipleFileUploadModel>>()
            .Instance.MaxFiles.ShouldBe(3);

        component.Render(parameters => parameters
            .Add(p => p.Configuration, MultipleFileUploadConfig(maxFiles: 7)));

        component.FindComponent<MudBlazorMultipleFileUploadComponent<MultipleFileUploadModel>>()
            .Instance.MaxFiles.ShouldBe(7);
    }

    // -----------------------------------------------------------------------------------------
    // Task 2 — derived-state rows: the shapes every real miss in #308 actually lived in.
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// The MudBlazor half of the issue's own table: lookup display text was "skipped by the sweep
    /// entirely" in #308. A different <see cref="LookupConfig"/> <b>object</b> describing the SAME
    /// field must still show the model's current value, not a blank left over from a reset with
    /// nothing to repopulate it.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Same <c>CityId</c> on both sides is not enough.</b> <c>UpdateDisplayText()</c> only
    /// fills <c>_displayText</c> when it is empty, and its fallback is <c>CurrentValue.ToString()</c>
    /// — which recomputes the SAME "7" whether or not the hook resets <c>_displayText</c> first, so a
    /// same-value swap alone cannot tell a working reset from a deleted one. Selecting through the
    /// dialog first gives <c>_displayText</c> the actual display name ("Lisbon"), which the fallback
    /// can never reproduce on its own — only THEN does the swap's outcome ("7" vs. a stale "Lisbon")
    /// depend on whether the hook's reset ran.
    /// </remarks>
    [Fact]
    public async Task LookupField_Row_Keeps_Its_Display_Text_After_A_Configuration_Swap()
    {
        var model = new LookupModel { CityId = 7 };
        Services.AddSingleton(StubDialogService.Returning<MudBlazorLookupDialog>(new LookupCity(7, "Lisbon")));

        var component = Render<FormCraftComponent<LookupModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, LookupConfig()));

        component.FindComponent<MudTextField<string>>().Instance.Value.ShouldBe("7");

        // Act - select through the dialog, the component's own selection path, so the display text
        // becomes something the ToString(CurrentValue) fallback could never reproduce by itself.
        await component.Find("button").ClickAsync(new());
        component.FindComponent<MudTextField<string>>().Instance.Value.ShouldBe("Lisbon");

        // Act - a DIFFERENT configuration object describing the same lookup field.
        component.Render(parameters => parameters.Add(p => p.Configuration, LookupConfig()));

        // Assert - the stale dialog selection does not survive the swap; the reset falls back to
        // the model's raw value instead of leaking the previous field's text forever (#298).
        component.FindComponent<MudTextField<string>>().Instance.Value.ShouldBe("7");
    }

    /// <summary>
    /// The LOV counterpart of the row above, exercising #336's ACTUAL failure mode directly: a
    /// selection made through the dialog must not survive a swap to a different configuration object
    /// for the same field.
    /// </summary>
    /// <remarks>
    /// ⚠️ A row that never selects anything cannot catch this. <c>UpdateDisplayText()</c> prefers
    /// <c>_selectedItems[0]</c>'s display text over <c>CurrentValue.ToString()</c> whenever
    /// <c>_selectedItems</c> is non-empty — so if <c>_selectedItems.Clear()</c> is dropped from the
    /// hook, a stale selected item survives the swap and keeps winning that comparison, showing the
    /// PREVIOUS field's label instead of falling back to the current model value.
    /// </remarks>
    [Fact]
    public async Task LovField_Row_Keeps_Its_Display_Text_After_A_Configuration_Swap()
    {
        var model = new LovModel();
        Services.AddSingleton(StubDialogService.Returning<LovSelectionDialog<LovCustomer, int?>>(
            new LovSelectionResult<LovCustomer> { SelectedItems = [new LovCustomer(7, "ACME")] }));

        var component = Render<FormCraftComponent<LovModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, LovConfig()));

        // Act - select through the dialog, the component's own selection path.
        await component.Find("button").ClickAsync(new());
        component.FindComponent<MudTextField<string>>().Instance.Value.ShouldBe("ACME");
        model.CustomerId.ShouldBe(7);

        // Act - a DIFFERENT configuration object describing the same LOV field.
        component.Render(parameters => parameters.Add(p => p.Configuration, LovConfig()));

        // Assert - the stale selection does not survive: display falls back to the model's raw
        // value instead of the previous field's selected label (#336).
        component.FindComponent<MudTextField<string>>().Instance.Value.ShouldBe("7");
    }

    // -----------------------------------------------------------------------------------------
    // Task 4 — the completeness guard: a component with neither a row above nor an exemption
    // below fails the build (#349).
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Every field-type component with a row above. Kept as the single source of truth the guard
    /// reads, so the suite and the guard cannot drift apart the way #308's hand-maintained coverage
    /// did.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Known ceiling:</b> this list is hand-maintained, not derived from the <c>[Fact]</c>
    /// methods above. The guard proves <c>universe ⊆ CoveredComponents ∪ ExemptComponents</c>; it does
    /// not prove <c>CoveredComponents</c> is exactly the set of types with a real row — a type added
    /// here without a matching row would pass silently. Keep the two in step by hand.
    /// </remarks>
    private static readonly IReadOnlySet<Type> CoveredComponents = new HashSet<Type>
    {
        typeof(MudBlazorTextFieldComponent<>),
        typeof(MudBlazorNumericFieldComponent<,>),
        typeof(MudBlazorNullableNumericFieldComponent<,>),
        typeof(MudBlazorBooleanFieldComponent<>),
        typeof(MudBlazorSelectFieldComponent<,>),
        typeof(MudBlazorMultiSelectFieldComponent<,>),
        typeof(MudBlazorAutocompleteFieldComponent<,>),
        typeof(MudBlazorDateOnlyFieldComponent<>),
        typeof(MudBlazorDateTimeFieldComponent<>),
        typeof(MudBlazorTimeOnlyFieldComponent<>),
        typeof(MudBlazorLookupFieldComponent<,>),
        typeof(MudBlazorLovFieldComponent<,,,>),
        typeof(MudBlazorFileUploadFieldComponent<>),
        typeof(MudBlazorMultipleFileUploadComponent<>),
    };

    /// <summary>
    /// Field-type components that genuinely cache nothing derived from the field's configuration,
    /// so there is nothing for <c>OnFieldConfigurationChanged</c> to get wrong. Each entry names
    /// what makes that true — an omission here is indistinguishable from "we forgot", which is the
    /// whole failure mode #349 exists to close.
    /// </summary>
    private static readonly IReadOnlySet<Type> ExemptComponents = new HashSet<Type>
    {
        // ColorPicker reads no attribute at all - only seeds a default VALUE (not configuration)
        // when the model's current value is empty.
        typeof(MudBlazorColorPickerComponent<>),

        // Rating's markup binds `MaxValue="@GetMaxRating()"`, a method that re-reads the
        // "MaxRating" attribute fresh on every render - never cached into a field the hook would
        // need to reset. MudBlazorRatingComponent DOES also carry a `MaxValue` PROPERTY, set once
        // from the same attribute in OnInitialized - but nothing ever reads it, because the markup
        // calls the method instead. This exemption holds only because that field is dead: if the
        // markup were ever changed to bind the property instead of calling GetMaxRating(), this
        // component would need OnFieldConfigurationChanged() to re-run OnInitialized's assignment
        // and would belong in CoveredComponents, not here.
        typeof(MudBlazorRatingComponent<>),

        // Same shape as Rating: every one of Slider's five configured values (Min/Max/Step/
        // ShowTickMarks/ShowValueLabel) is read through a `GetXxx()` method called directly from
        // markup, recomputed on every render regardless of the hook.
        typeof(MudBlazorSliderComponent<>),
    };

    [Fact]
    public void Every_Field_Component_Should_Have_A_Parity_Row_Or_An_Explicit_Exemption()
    {
        var universe = FieldComponentCoverageGuard.FieldComponentTypes(
            typeof(MudBlazorTextFieldComponent<>).Assembly);

        var gaps = FieldComponentCoverageGuard.FindGaps(universe, CoveredComponents, ExemptComponents);

        gaps.ShouldBeEmpty(
            "These field components have neither a parity row in FieldConfigurationParityTests nor "
            + "an explicit exemption. Add a row proving they honour OnFieldConfigurationChanged, or "
            + "add them to ExemptComponents with a reason:\n  "
            + string.Join("\n  ", gaps.Select(g => g.ToString())));
    }

    /// <summary>
    /// The guard's own guard: withholding a real, currently-covered component must produce a gap,
    /// so a detection path that silently stopped detecting (#308's exact failure mode, one level
    /// up) cannot pass this suite unnoticed.
    /// </summary>
    [Fact]
    public void The_Guard_Should_Flag_A_Component_Withheld_From_Coverage()
    {
        var universe = FieldComponentCoverageGuard.FieldComponentTypes(
            typeof(MudBlazorTextFieldComponent<>).Assembly);

        var withheld = CoveredComponents.Where(t => t != typeof(MudBlazorTextFieldComponent<>)).ToHashSet();

        var gaps = FieldComponentCoverageGuard.FindGaps(universe, withheld, ExemptComponents);

        gaps.ShouldContain(g => g.Component == typeof(MudBlazorTextFieldComponent<>));
    }

    // -----------------------------------------------------------------------------------------
    // Config builders and models, one pair per row.
    // -----------------------------------------------------------------------------------------

    private static IFormConfiguration<NumericModel> NumericConfig(int step) =>
        FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithAttribute("Step", (int?)step))
            .Build();

    private static IFormConfiguration<NullableNumericModel> NullableNumericConfig(int min) =>
        FormBuilder<NullableNumericModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithAttribute("Min", (int?)min))
            .Build();

    private static IFormConfiguration<BooleanModel> BooleanConfig(BooleanDisplayStyle style) =>
        FormBuilder<BooleanModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithAttribute("DisplayStyle", style))
            .Build();

    private static IFormConfiguration<AutocompleteModel> AutocompleteConfig(int debounceMs) =>
        FormBuilder<AutocompleteModel>
            .Create()
            .AddField(x => x.Value, field => field
                .WithLabel("Value")
                .AsAutocomplete(
                    searchFunc: (_, _) => Task.FromResult(Enumerable.Empty<SelectOption<string>>()),
                    debounceMs: debounceMs))
            .Build();

    private static IFormConfiguration<DateOnlyModel> DateOnlyConfig(string format) =>
        FormBuilder<DateOnlyModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithAttribute("Format", format))
            .Build();

    private static IFormConfiguration<DateTimeModel> DateTimeConfig(DateTime minDate) =>
        FormBuilder<DateTimeModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithAttribute("MinDate", minDate))
            .Build();

    private static IFormConfiguration<TimeOnlyModel> TimeOnlyConfig(bool showClearButton) =>
        FormBuilder<TimeOnlyModel>
            .Create()
            .AddField(x => x.Value, field => field
                .WithLabel("Value")
                .WithAttribute("ShowClearButton", showClearButton))
            .Build();

    private static IFormConfiguration<FileUploadModel> FileUploadConfig(long maxFileSize) =>
        FormBuilder<FileUploadModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").AsFileUpload(maxFileSize: maxFileSize))
            .Build();

    private static IFormConfiguration<MultipleFileUploadModel> MultipleFileUploadConfig(int maxFiles) =>
        FormBuilder<MultipleFileUploadModel>
            .Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").AsMultipleFileUpload(maxFiles: maxFiles))
            .Build();

    private static IFormConfiguration<LookupModel> LookupConfig() =>
        FormBuilder<LookupModel>
            .Create()
            .AddField(x => x.CityId, field => field
                .WithLabel("City")
                .AsLookup<LookupModel, int, LookupCity>(
                    dataProvider: _ => Task.FromResult(new LookupResult<LookupCity>
                    {
                        Items = [new LookupCity(7, "Lisbon")],
                        TotalCount = 1,
                    }),
                    valueSelector: c => c.Id,
                    displaySelector: c => c.Name))
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
                    .AddColumn(c => c.Name, "Name")))
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

    private class DateOnlyModel
    {
        public DateOnly Value { get; set; }
    }

    private class DateTimeModel
    {
        public DateTime Value { get; set; }
    }

    private class TimeOnlyModel
    {
        public TimeOnly Value { get; set; }
    }

    private class FileUploadModel
    {
        public IBrowserFile? Value { get; set; }
    }

    private class MultipleFileUploadModel
    {
        public IReadOnlyList<IBrowserFile>? Value { get; set; }
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
}
