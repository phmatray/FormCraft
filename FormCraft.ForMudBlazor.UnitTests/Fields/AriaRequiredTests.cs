using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that a <c>.Required(...)</c> field is announced as required to assistive technology, on
/// both render paths (#199). WCAG 2.1 <b>3.3.2 Labels or Instructions</b> (Level A) expects required
/// fields to be identified; before this they were not, on either path.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the HTML5 attribute comes back.</b> The issue asked for <c>aria-required="true"</c>
/// <i>without</i> HTML5 <c>required</c>. Measured against MudBlazor 9.8.0, that is unreachable:
/// <c>MudInput</c> splats <c>UserAttributes</c> into the element and then writes its own
/// <c>required</c> and <c>aria-required</c> afterwards, both off the single <c>Required</c> bool.
/// Blazor resolves duplicate attributes last-write-wins, so a caller-supplied <c>aria-required</c>
/// is always overwritten and the two attributes cannot be separated. The owner's decision was to
/// drive <c>Required</c> from <c>IsRequired</c> and accept the HTML5 attribute, which is inert here:
/// FormCraft forms render <c>novalidate</c>, a guarantee #206 pins with tests.
/// </para>
/// <para>
/// This deliberately reverses the collection-path half of #190. What #190 actually fixed was the
/// <i>divergence</i> — the same <c>.Required("…")</c> call decorating an item field and not an
/// ordinary one — and that stays fixed, because both paths now resolve the flag the same way. What
/// it also did, and what this undoes, is level the two paths down to silence.
/// </para>
/// <para>
/// ⚠️ <c>aria-required="false"</c> is asserted for optional fields rather than the attribute's
/// absence. MudBlazor emits it unconditionally, and <c>false</c> is the correct ARIA value for an
/// optional field, so its presence is not the defect — the defect was a <i>required</i> field
/// saying <c>"false"</c>, which is an affirmatively wrong statement to a screen reader rather than
/// merely a missing one.
/// </para>
/// </remarks>
public class AriaRequiredTests : MudBlazorTestBase
{
    [Fact]
    public void Required_Field_Should_Announce_Itself_To_Assistive_Technology()
    {
        // Arrange & Act - the plain .Required(...) call, on an ordinary (non-collection) field
        var component = RenderField(f => f.WithLabel("Name").Required("Name is required"));

        // Assert - the attribute a screen reader actually reads, on the element it reads it from
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_Field_Should_Not_Be_Announced_As_Required()
    {
        // Arrange & Act - the overwhelmingly common case must stay untouched
        var component = RenderField(f => f.WithLabel("Name"));

        // Assert - "false" rather than absent: MudBlazor emits it either way, and false is correct
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public void Required_Field_Should_Also_Carry_The_Visible_Required_Marker()
    {
        // Arrange & Act - MudBlazor's asterisk is a CSS ::after on .mud-input-required, so the CLASS
        // is the measurable proxy. The spec listed the asterisk as a non-goal; under this mechanism
        // it is not separable from the ARIA flag, so it ships - and it is itself a WCAG 3.3.2
        // *visible* identification. Pinned so the pairing is a decision on record, not an accident.
        var component = RenderField(f => f.WithLabel("Name").Required("Name is required"));

        // Assert
        component.FindAll(".mud-input-required").ShouldNotBeEmpty();
    }

    [Fact]
    public void Explicit_Native_Required_Opt_Out_Should_Win_Over_Required()
    {
        // Arrange & Act - the escape hatch has to work in BOTH directions once .Required(...) drives
        // the flag. Without this, a caller who deliberately suppressed the decoration in #204 would
        // silently get it back.
        var component = RenderField(f => f
            .WithLabel("Name")
            .Required("Name is required")
            .WithNativeRequired(false));

        // Assert
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
        component.Find("input").HasAttribute("required").ShouldBeFalse();
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    [Fact]
    public void Explicit_Native_Required_Opt_In_Should_Still_Work_Without_Required()
    {
        // Arrange & Act - #204's opt-in keeps working on a field that never called .Required(...)
        var component = RenderField(f => f.WithLabel("Name").WithNativeRequired());

        // Assert
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Numeric_Field_Should_Announce_Itself()
    {
        // Arrange - the shared base property feeds every component-path renderer that binds
        // Required, so the numeric one must follow the text one rather than be fixed separately.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Quantity, f => f.WithLabel("Quantity").Required("Quantity is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Date_Field_Should_Announce_Itself()
    {
        // Arrange - MudDatePicker is the third component-path family binding Required, and the one
        // #190 missed first time round.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.When, f => f.WithLabel("When").Required("When is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Text_Item_Field_Should_Announce_Itself()
    {
        // Arrange & Act - the collection path builds its tree imperatively, so it has to resolve the
        // flag by the same rule rather than inherit it. AddCommonFieldAttributes feeds three
        // renderers and #190 fixed only the one it was measured on, so all three are covered here.
        var component = this.RenderItemForm(NewOrder(), TextItemForm(f => f.Required("Product is required")));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Numeric_Item_Field_Should_Announce_Itself()
    {
        // Arrange & Act - the second of the three renderers
        var component = this.RenderItemForm(NewBasket(), NumericItemForm(f => f.Required("Quantity is required")));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Date_Item_Field_Should_Announce_Itself()
    {
        // Arrange & Act - the third, MudDatePicker, which #190 missed on the first pass
        var component = this.RenderItemForm(NewAppointment(), DateItemForm(f => f.Required("When is required")));

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_Item_Field_Should_Not_Be_Announced_As_Required()
    {
        // Arrange & Act - the item-path counterpart of the ordinary-field case above
        var component = this.RenderItemForm(NewOrder(), TextItemForm());

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeFalse();
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public void Explicit_Native_Required_Opt_Out_Should_Win_On_The_Item_Path_Too()
    {
        // Arrange & Act - the escape hatch has to behave identically on both paths, or it becomes
        // the next divergence. GetItemFieldRequired tests presence separately from value precisely
        // so this case does not collapse into the "not configured" fallback.
        var component = this.RenderItemForm(NewOrder(), TextItemForm(f => f
            .Required("Product is required")
            .WithNativeRequired(false)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeFalse();
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public void Required_Boolean_Item_Field_Should_Announce_Itself()
    {
        // Arrange - THE DECIDED BOOLEAN CASE (#199 Task 3 Step 1): announced, not pinned inert.
        // RenderBooleanField takes none of AddCommonFieldAttributes' set (MudCheckBox shares almost
        // none of those parameters), so it resolves the flag itself by the same rule.
        //
        // A required consent checkbox is the single most common required control that is NOT a text
        // field, so leaving it silent would have left the headline case of this issue unfixed —
        // and worse than silent once every required text field carries an asterisk, because absence
        // would then read as "optional".
        var component = this.RenderItemForm(NewBasket(), BooleanItemForm(f => f.Required("Gift is required")));

        // Assert - the parameter, the asterisk class, and the attribute on the real <input>
        component.FindComponent<MudCheckBox<bool>>().Instance.Label.ShouldBe("Gift");
        component.FindComponent<MudCheckBox<bool>>().Instance.Required.ShouldBeTrue();
        component.FindAll(".mud-input-required").ShouldNotBeEmpty();
        component.Find("input[type=checkbox]").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Boolean_Field_Should_Announce_Itself_On_The_Component_Path_Too()
    {
        // Arrange - the ordinary-field half of the same case. MudBlazorBooleanFieldComponent derives
        // from FieldComponentBase rather than MudBlazorFieldComponentBase, so it has no inherited
        // EffectiveNativeRequired and had to resolve the rule itself — precisely the shape of
        // divergence this library keeps re-filing, so both halves are asserted.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Accepted, f => f.WithLabel("Accept").Required("You must accept"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudCheckBox<bool>>().Instance.Required.ShouldBeTrue();
        component.Find("input[type=checkbox]").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_Boolean_Field_Should_Not_Be_Announced_As_Required()
    {
        // Arrange & Act - the common case stays untouched on the checkbox path too
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Accepted, f => f.WithLabel("Accept"))
            .Build();

        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudCheckBox<bool>>().Instance.Required.ShouldBeFalse();
        component.Find("input[type=checkbox]").GetAttribute("aria-required").ShouldBe("false");
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    [Fact]
    public void Required_Select_Field_Should_Announce_Itself()
    {
        // Arrange - a required dropdown is one of the commonest required controls, and was the
        // headline example of the gap: with every required text field carrying an asterisk, an
        // unmarked Country select reads as OPTIONAL rather than merely unannounced.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Country, f => f
                .WithLabel("Country")
                .WithOptions(("US", "United States"), ("BE", "Belgium"))
                .Required("Country is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - MudSelect forwards Required to its inner MudInput, which is what emits the ARIA
        component.FindComponent<MudSelect<string>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_Select_Field_Should_Not_Be_Announced_As_Required()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Country, f => f
                .WithLabel("Country")
                .WithOptions(("US", "United States"), ("BE", "Belgium")))
            .Build();

        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudSelect<string>>().Instance.Required.ShouldBeFalse();
        component.Find("input").GetAttribute("aria-required").ShouldBe("false");
    }

    [Fact]
    public void Required_Autocomplete_Field_Should_Announce_Itself()
    {
        // Arrange - MudAutocomplete forwards Required to its inner MudInput like MudSelect does.
        // Lookup and LOV fields are not separately asserted here: both render a plain MudTextField,
        // so they are the already-covered text case with an adornment attached.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Country, f => f
                .WithLabel("City")
                .AsAutocomplete(SearchAsync)
                .Required("City is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudAutocomplete<string>>().Instance.Required.ShouldBeTrue();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_FileUpload_Field_Should_Announce_Itself_At_The_Label_And_The_Button()
    {
        // Arrange - THE FILE-UPLOAD CASE, now answered rather than pinned (#262). The rationale that
        // kept it unannotated under #199 is kept here, because it is still the reason the hidden
        // <input type="file"> is NOT the element being annotated:
        //
        //   MudFileUpload does accept Required and would emit aria-required on its <input
        //   type="file">. But FormCraft renders that input with `tabindex="-1"` and `opacity-0`
        //   beneath a custom drop zone, so it is deliberately OUT of the tab order: a screen-reader
        //   user never lands on the element the annotation would sit on, and the affordance they do
        //   reach is a MudButton that takes no such attribute. Annotating the hidden input would
        //   satisfy a DOM assertion while telling no user anything — the "forwarded but inert"
        //   failure this suite's sibling tests exist to catch.
        //
        // What changed is that #199 made absence *mean* something. Every other required field now
        // renders `*` and aria-required="true", so a silent upload beside them reads as OPTIONAL —
        // a stronger wrong signal than the uniform silence it replaced. So the requirement is
        // identified where the user actually is: the field's own <MudText> label (visible) and
        // aria-describedby on the focusable MudButton (announced on focus).
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - the visible marker, in the label FormCraft itself renders.
        //
        // `span` is load-bearing, not decoration. Task 3 also binds Required on MudFileUpload, which
        // puts the bare `mud-input-required` class on MudBlazor's own input-control <div> — so a
        // selector of `.mud-input-required` alone would pass even with FormCraft's marker deleted,
        // i.e. it would assert nothing. The <span> is FormCraft's, and only FormCraft's.
        component.FindAll("span.mud-input-required").ShouldNotBeEmpty();

        // Assert - the programmatic association, on the element that actually receives focus
        var browse = component.FindAll(".mud-toolbar button")[0];
        var describedBy = browse.GetAttribute("aria-describedby");
        describedBy.ShouldNotBeNullOrWhiteSpace();

        // Assert - and it resolves to text that names the requirement
        var hint = component.Find($"#{describedBy}");
        hint.TextContent.ShouldContain("Passport scan");
        hint.TextContent.ShouldContain("required");
    }

    [Fact]
    public void Required_FileUpload_Field_With_No_Label_Should_Still_Describe_The_Requirement()
    {
        // Arrange - the component renders its <MudText> label only inside an @if, so an unlabelled
        // required field has no visible marker to attach one to. The button's description is then
        // the only channel left, and it has to say something without a label to name.
        //
        // The empty WithLabel is what actually reaches that branch: FieldConfiguration's constructor
        // defaults Label to the property name, so simply omitting .WithLabel(...) still yields the
        // labelled path ("Upload"). Only an explicitly blank label is label-free.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel(string.Empty).Required("A scan is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - no label, so no visible marker to render. Scoped to the <span> because MudBlazor's
        // input-control <div> carries the bare class once Required is bound (see the labelled test).
        component.FindAll("span.mud-input-required").ShouldBeEmpty();

        // ...but the requirement still reaches the element that receives focus
        var browse = component.FindAll(".mud-toolbar button")[0];
        var describedBy = browse.GetAttribute("aria-describedby");
        describedBy.ShouldNotBeNullOrWhiteSpace();
        component.Find($"#{describedBy}").TextContent.ShouldContain("required");
    }

    [Fact]
    public void Optional_FileUpload_Field_Should_Carry_Neither_Channel()
    {
        // Arrange & Act - the overwhelmingly common case must stay exactly as it was
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan"))
            .Build();

        var component = RenderConfig(config);

        // Assert - no marker, and no dangling aria-describedby pointing at nothing. The selector is
        // deliberately the BARE class here: on an optional field neither FormCraft's <span> nor
        // MudBlazor's input-control <div> may carry it, and asserting both at once is stronger.
        component.FindAll(".mud-input-required").ShouldBeEmpty();
        component.FindAll(".mud-toolbar button")[0].HasAttribute("aria-describedby").ShouldBeFalse();
    }

    [Fact]
    public void Required_MultipleFileUpload_Field_Should_Announce_Itself_The_Same_Way()
    {
        // Arrange - the two upload components diverging is the exact failure class this library keeps
        // re-filing (#146, #177, #184, #189), so the multiple-file component gets the single-file
        // component's assertions verbatim rather than a weaker version of them.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Attachments, f => f
                .WithLabel("Supporting documents")
                .Required("At least one document is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - the visible marker (the <span> is FormCraft's own; see the single-file test)
        component.FindAll("span.mud-input-required").ShouldNotBeEmpty();

        // Assert - and the programmatic association on the focusable button
        var browse = component.FindAll(".mud-toolbar button")[0];
        var describedBy = browse.GetAttribute("aria-describedby");
        describedBy.ShouldNotBeNullOrWhiteSpace();

        var hint = component.Find($"#{describedBy}");
        hint.TextContent.ShouldContain("Supporting documents");
        hint.TextContent.ShouldContain("required");
    }

    [Fact]
    public void Two_Required_Upload_Fields_On_One_Form_Should_Not_Share_A_Description_Id()
    {
        // Arrange - the hint is referenced by id, so two upload fields colliding on one id would
        // point both buttons at the same description and mislabel one of them
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f
                .WithLabel("Passport scan")
                .Required("A scan is required"))
            .AddField(x => x.Attachments, f => f
                .WithLabel("Supporting documents")
                .Required("At least one document is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - one describedby per upload field, and the two differ
        var describedBy = component
            .FindAll(".mud-toolbar button[aria-describedby]")
            .Select(b => b.GetAttribute("aria-describedby"))
            .ToList();

        describedBy.Count.ShouldBe(2);
        describedBy.Distinct().Count().ShouldBe(2);

        // Assert - and each resolves to its OWN field's requirement
        component.Find($"#{describedBy[0]}").TextContent.ShouldContain("Passport scan");
        component.Find($"#{describedBy[1]}").TextContent.ShouldContain("Supporting documents");
    }

    [Fact]
    public void FileUpload_Field_With_WithNativeRequired_False_Should_Suppress_Both_Channels()
    {
        // Arrange - the per-field opt-out has to reach this field type too, or ".WithNativeRequired
        // (false)" would mean something different here than on the other eight (#199)
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f
                .WithLabel("Passport scan")
                .Required("A scan is required")
                .WithNativeRequired(false))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - all THREE channels suppressed: the visible marker, the announced description,
        // and the belt-and-braces flag on the hidden input. The bare class covers the first two
        // sources at once (FormCraft's <span> and MudBlazor's input-control <div>).
        component.FindAll(".mud-input-required").ShouldBeEmpty();
        component.FindAll(".mud-toolbar button")[0].HasAttribute("aria-describedby").ShouldBeFalse();
        component.FindComponent<MudFileUpload<IBrowserFile>>().Instance.Required.ShouldBeFalse();
    }

    [Fact]
    public void Required_FileUpload_Should_Also_Flag_The_Hidden_Input_As_Belt_And_Braces()
    {
        // Arrange - THE DECIDED "also bind Required" QUESTION (#262 Task 3), settled as YES.
        //
        // This is deliberately NOT the mechanism the issue relies on: the input it annotates is
        // opacity-0 and tabindex="-1", so no keyboard or screen-reader user navigating by focus
        // order ever lands on it. The label marker and the button's aria-describedby remain the
        // answer. But some assistive technology walks the accessibility TREE rather than the tab
        // order — a screen reader's forms/controls list is the common case — and for those the flag
        // on the real input is free extra signal.
        //
        // It is safe here for two measured reasons: FormCraft forms render `novalidate` (#206), so
        // the HTML5 `required` that comes with the flag is inert, and MudFileUpload's own error slot
        // stays empty (pinned by the sibling test below), so no second message appears.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - the flag reached the component, and the attribute reached the element
        component.FindComponent<MudFileUpload<IBrowserFile>>().Instance.Required.ShouldBeTrue();
        component.Find("input[type=file]").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Optional_FileUpload_Should_Not_Flag_The_Hidden_Input()
    {
        // Arrange & Act - the opt-out and the plain optional field must both leave it alone
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan"))
            .Build();

        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudFileUpload<IBrowserFile>>().Instance.Required.ShouldBeFalse();
    }

    [Fact]
    public async Task Blank_Required_FileUpload_Should_Surface_Exactly_One_Message()
    {
        // Arrange - the same behavioural risk Blank_Required_Field_Should_Surface_Exactly_One_Message
        // pins for text fields, re-asked for the field type this issue newly sets the flag on.
        // MudFormComponent can run a required check of its OWN, differently worded; if binding
        // Required woke that up, every required upload would report twice.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        var component = RenderConfig(config);

        // Act
        await component.InvokeAsync(() => component.Instance.ValidateAsync());

        // Assert - exactly one error message, and it is the developer's own wording
        component.WaitForAssertion(() =>
            component.FindComponents<FieldValidationMessage>()
                .Single(m => m.Instance.FieldName == nameof(TestModel.Upload))
                .FindComponents<MudText>()
                .Count(t => t.Instance.Color == Color.Error)
                .ShouldBe(1));

        // ...and MudBlazor's own error slot stays empty, so nothing is queued behind it
        var upload = component.FindComponent<MudFileUpload<IBrowserFile>>().Instance;
        upload.Error.ShouldBeFalse();
        upload.ErrorText.ShouldBeNullOrEmpty();
    }

    private static Task<IEnumerable<SelectOption<string>>> SearchAsync(
        string value,
        CancellationToken cancellationToken) =>
        Task.FromResult<IEnumerable<SelectOption<string>>>(
        [
            new SelectOption<string>("BE", "Brussels"),
            new SelectOption<string>("FR", "Paris"),
        ]);

    [Fact]
    public async Task Blank_Required_Field_Should_Surface_Exactly_One_Message()
    {
        // Arrange - the behavioural risk of driving MudBlazor's Required from .Required(...):
        // MudFormComponent can run a required check of its OWN, differently worded, which would
        // double every required message on every form in the library. CollectionRequiredTests pins
        // this for item fields; nothing pinned it for ordinary ones, and ordinary fields are the
        // ones this issue newly sets the flag on — so the guard belongs here too.
        //
        // It does not fire today (fields carry no `For` and sit in no MudForm), but "does not fire
        // today" is exactly the kind of claim that needs a test rather than a comment.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").Required("Name is required"))
            .Build();

        var component = RenderConfig(config);

        // Act
        await component.InvokeAsync(() => component.Instance.ValidateAsync());

        // Assert - exactly one error message, and it is the developer's own wording
        component.WaitForAssertion(() =>
            component.FindComponents<FieldValidationMessage>()
                .Single(m => m.Instance.FieldName == nameof(TestModel.Name))
                .FindComponents<MudText>()
                .Count(t => t.Instance.Color == Color.Error)
                .ShouldBe(1));

        // ...and MudBlazor's own error slot stays empty, so nothing is queued behind it
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Error.ShouldBeFalse();
        textField.ErrorText.ShouldBeNullOrEmpty();
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderField(
        Action<FieldBuilder<TestModel, string>> configure)
    {
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, configure)
            .Build();

        return RenderConfig(config);
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderConfig(
        IFormConfiguration<TestModel> config) =>
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel())
            .Add(p => p.Configuration, config));

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public DateTime When { get; set; }

        public bool Accepted { get; set; }

        public string Country { get; set; } = string.Empty;

        public IBrowserFile? Upload { get; set; }

        // Exactly IReadOnlyList<IBrowserFile> — that is what MudBlazorMultipleFileUploadRenderer
        // matches on, so a different list type would silently render the single-file component.
        public IReadOnlyList<IBrowserFile>? Attachments { get; set; }
    }
}
