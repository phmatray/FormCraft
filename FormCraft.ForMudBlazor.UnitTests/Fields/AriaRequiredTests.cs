using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that a <c>.Required(...)</c> field is announced as required to assistive technology (#199),
/// and — since #263 — that it is announced <i>without</i> acquiring the HTML5 <c>required</c>
/// attribute. WCAG 2.1 <b>3.3.2 Labels or Instructions</b> (Level A) expects required fields to be
/// identified; before #199 they were not.
/// </summary>
/// <remarks>
/// <para>
/// <b>How the two attributes came apart.</b> #199 asked for <c>aria-required="true"</c> without
/// HTML5 <c>required</c> and could not have it: measured against MudBlazor 9.8.0, <c>MudInput</c>
/// splatted <c>UserAttributes</c> and then wrote its own <c>required</c> and <c>aria-required</c>
/// afterwards, both off the single <c>Required</c> bool, and Blazor resolves duplicate attributes
/// last-write-wins — so a caller-supplied <c>aria-required</c> was always overwritten. #199 shipped
/// the compromise: drive <c>Required</c> from <c>IsRequired</c> and accept the HTML5 attribute,
/// inert here because FormCraft forms render <c>novalidate</c> (#206).
/// </para>
/// <para>
/// <see href="https://github.com/MudBlazor/MudBlazor/pull/13613">MudBlazor#13613</see>, released in
/// <b>9.9.0</b>, moved those ARIA writes above the splat so callers win, while leaving
/// <c>required</c> below it and deliberately not overridable. #263 is FormCraft taking that up:
/// the announcement travels through <c>UserAttributes</c>, and MudBlazor's <c>Required</c> parameter
/// is reserved for the explicit <c>.WithNativeRequired()</c> opt-in. These tests will go red on any
/// MudBlazor older than 9.9.0 — that version floor is the point, not an accident.
/// </para>
/// <para>
/// ⚠️ <b>The asterisk is the cost.</b> MudBlazor's visible marker is driven by the same
/// <c>Required</c> parameter as the HTML5 attribute, so it goes too. That trade is the spec's
/// explicit decision and is pinned by
/// <c>Required_Field_Should_No_Longer_Carry_MudBlazors_Visible_Marker</c> together with
/// <c>Explicit_Native_Required_Should_Restore_The_Marker_And_The_Html5_Attribute</c> — read as a
/// pair, they are the decision, not an accident anyone has to reverse-engineer later.
/// </para>
/// <para>
/// ⚠️ <c>aria-required="false"</c> is asserted for optional fields rather than the attribute's
/// absence. <c>false</c> is the correct ARIA value for an optional field, so its presence was never
/// the defect — the defect was a <i>required</i> field saying <c>"false"</c>, which is an
/// affirmatively wrong statement to a screen reader rather than merely a missing one.
/// </para>
/// </remarks>
public class AriaRequiredTests : MudBlazorTestBase
{
    [Fact]
    public void Required_Field_Should_Announce_Itself_To_Assistive_Technology()
    {
        // Arrange & Act - the plain .Required(...) call, on an ordinary (non-collection) field
        var component = RenderField(f => f.WithLabel("Name").Required("Name is required"));

        // Assert - the attribute a screen reader actually reads, on the element it reads it from,
        // and NOT the HTML5 one. Both halves matter: #199 could only deliver the first because
        // MudBlazor fused them, and #263 exists to separate them.
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
        component.Find("input").HasAttribute("required").ShouldBeFalse();
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
    public void Required_Field_Should_No_Longer_Carry_MudBlazors_Visible_Marker()
    {
        // Arrange & Act - THE ASTERISK DECISION (#263 Task 3 Step 4), recorded as a test rather than
        // left to be discovered. MudBlazor's asterisk is a CSS ::after on .mud-input-required, and
        // that class comes from the same Required parameter as the HTML5 attribute - one flag, both
        // effects, still fused after MudBlazor#13613 (which separated only the ARIA write). So
        // dropping the attribute drops the asterisk with it; they cannot be had separately.
        //
        // The spec chose to accept that rather than invent a FormCraft-owned marker: its behaviour
        // diagram ends "no asterisk unless .WithNativeRequired()", and its non-goals keep the
        // decoration available through that opt-in. #199 had shipped the asterisk as a *visible*
        // WCAG 3.3.2 identification, so this is a real trade, not a free win - which is exactly why
        // it is pinned here and in the test below rather than merely inverted in silence.
        var component = RenderField(f => f.WithLabel("Name").Required("Name is required"));

        // Assert - announced to assistive technology, but no longer marked for sighted users
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    [Fact]
    public void Explicit_Native_Required_Should_Restore_The_Marker_And_The_Html5_Attribute()
    {
        // Arrange & Act - the other half of that decision: .WithNativeRequired() is the documented
        // way back to MudBlazor's native semantics, and "native" means the whole package. A caller
        // who wants the asterisk has to accept the HTML5 attribute that comes with it, and this
        // test is what makes that promise checkable.
        var component = RenderField(f => f
            .WithLabel("Name")
            .Required("Name is required")
            .WithNativeRequired());

        // Assert
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
        component.Find("input").HasAttribute("required").ShouldBeTrue();
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
        component.FindComponent<MudNumericField<int>>().Instance.Required.ShouldBeFalse();
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
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeFalse();
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
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeFalse();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Numeric_Item_Field_Should_Announce_Itself()
    {
        // Arrange & Act - the second of the three renderers
        var component = this.RenderItemForm(NewBasket(), NumericItemForm(f => f.Required("Quantity is required")));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Required.ShouldBeFalse();
        component.Find("input").GetAttribute("aria-required").ShouldBe("true");
    }

    [Fact]
    public void Required_Date_Item_Field_Should_Announce_Itself()
    {
        // Arrange & Act - the third, MudDatePicker, which #190 missed on the first pass
        var component = this.RenderItemForm(NewAppointment(), DateItemForm(f => f.Required("When is required")));

        // Assert
        component.FindComponent<MudDatePicker>().Instance.Required.ShouldBeFalse();
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
        component.FindComponent<MudCheckBox<bool>>().Instance.Required.ShouldBeFalse();
        component.FindAll(".mud-input-required").ShouldBeEmpty();
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
        component.FindComponent<MudCheckBox<bool>>().Instance.Required.ShouldBeFalse();
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
        component.FindComponent<MudSelect<string>>().Instance.Required.ShouldBeFalse();
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
        component.FindComponent<MudAutocomplete<string>>().Instance.Required.ShouldBeFalse();
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
        // Selected by FormCraft's OWN class rather than the shared `mud-input-required`: the marker
        // is a real text node, whereas MudBlazor's only rule for its own class is an ::after on a
        // `.mud-input-label` descendant that never matches this span. Asserting the owned class
        // keeps the test pointed at the thing that actually renders.
        component.FindAll(".formcraft-required-marker").ShouldNotBeEmpty();

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

        // Assert - no label, so no visible marker to render
        component.FindAll(".formcraft-required-marker").ShouldBeEmpty();

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

        // Assert - the visible marker (FormCraft's own class; see the single-file test)
        component.FindAll(".formcraft-required-marker").ShouldNotBeEmpty();

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
    public void The_Same_Upload_Field_Rendered_Twice_Should_Not_Share_A_Description_Id()
    {
        // Arrange - THE case that can actually collide, and the one the sibling test above cannot
        // reach: two different fields have different names, so `formcraft-{FieldName}-required`
        // separates them under any implementation, including a broken one. The same field rendered
        // twice does not — and that is not hypothetical. Item fields render through these very
        // components since #203, so a required upload inside .WithItemForm(...) emits one hint per
        // row; two forms over one model on a page do the same. Duplicate ids are invalid HTML and
        // point every later button at the first one's description.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f
                .WithLabel("Passport scan")
                .Required("A scan is required"))
            .Build();

        // Act - two independent instances of the same form, in one document
        var component = Render(builder =>
        {
            for (var i = 0; i < 2; i++)
            {
                builder.OpenComponent<FormCraftComponent<TestModel>>(i);
                builder.AddAttribute(i + 1, nameof(FormCraftComponent<TestModel>.Model), new TestModel());
                builder.AddAttribute(i + 2, nameof(FormCraftComponent<TestModel>.Configuration), config);
                builder.CloseComponent();
            }
        });

        // Assert - both rendered, and their hint ids are distinct
        var describedBy = component
            .FindAll(".mud-toolbar button[aria-describedby]")
            .Select(b => b.GetAttribute("aria-describedby"))
            .ToList();

        describedBy.Count.ShouldBe(2);
        describedBy.Distinct().Count().ShouldBe(2);

        // Assert - and each id resolves to exactly one element, i.e. no duplicate ids in the document
        foreach (var id in describedBy)
        {
            component.FindAll($"#{id}").Count.ShouldBe(1);
        }
    }

    [Fact]
    public void MultipleFileUpload_Should_Honour_The_Opt_Out_And_Leave_MudFileUpload_Unbound()
    {
        // Arrange - the parity half of the opt-out. Every other assertion about .WithNativeRequired
        // (false) and the unbound MudFileUpload targets the SINGLE-file component, so the multiple
        // component could regress on both while the suite stayed green — the divergence this issue's
        // shared base exists to prevent.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Attachments, f => f
                .WithLabel("Supporting documents")
                .Required("At least one document is required")
                .WithNativeRequired(false))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - marker and description both suppressed
        component.FindAll(".formcraft-required-marker").ShouldBeEmpty();
        component.FindAll(".mud-toolbar button")[0].HasAttribute("aria-describedby").ShouldBeFalse();

        // Assert - and this component leaves MudFileUpload unbound too, exactly like the single one
        component
            .FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>()
            .Instance.Required.ShouldBeFalse();
    }

    [Fact]
    public void Required_MultipleFileUpload_Should_Not_Bind_Required_On_MudFileUpload()
    {
        // Arrange & Act - the required case of the same parity claim
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Attachments, f => f
                .WithLabel("Supporting documents")
                .Required("At least one document is required"))
            .Build();

        var component = RenderConfig(config);

        // Assert
        component
            .FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>()
            .Instance.Required.ShouldBeFalse();
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

        // Assert - both channels suppressed: the visible marker and the announced description.
        // The bare `mud-input-required` is asserted too, which is the stronger statement: nothing
        // anywhere in the field claims required-ness, FormCraft's marker included.
        component.FindAll(".mud-input-required").ShouldBeEmpty();
        component.FindAll(".formcraft-required-marker").ShouldBeEmpty();
        component.FindAll(".mud-toolbar button")[0].HasAttribute("aria-describedby").ShouldBeFalse();
    }

    [Fact]
    public void Required_FileUpload_Should_Not_Bind_Required_On_MudFileUpload()
    {
        // Arrange - THE DECIDED "also bind Required" QUESTION (#262 Task 3), settled as NO, after
        // measuring it. Binding it was attempted first, as belt-and-braces for assistive technology
        // that walks the accessibility tree rather than the tab order, and reverted because it is
        // not free:
        //
        //   MudFormComponent.ValidateValue() raises its OWN RequiredError ("Required") when
        //   `Required && Touched && !HasValue`. MudFileUpload.ClearAsync() sets Touched and
        //   validates, and the resulting error only goes to an EditContext when one is cascaded.
        //   Rendered standalone — a supported path, IFieldRendererService.RenderField, exercised by
        //   this suite — clearing a required upload therefore printed MudBlazor's own "Required"
        //   under the drop zone, in different words from the developer's message.
        //   Pinned by Clearing_A_Standalone_Required_Upload_Should_Not_Surface_MudBlazors_Own_Error.
        //
        // The annotation would have landed on an opacity-0, tabindex="-1" input nobody reaches, so
        // the trade was a real wrong message for a speculative benefit. The label marker and the
        // button's aria-describedby remain the mechanism, and they are enough.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - deliberately unbound
        component.FindComponent<MudFileUpload<IBrowserFile>>().Instance.Required.ShouldBeFalse();

        // ...while the two channels that DO reach a user are present
        component.FindAll(".formcraft-required-marker").ShouldNotBeEmpty();
        component.FindAll(".mud-toolbar button")[0].HasAttribute("aria-describedby").ShouldBeTrue();
    }

    [Fact]
    public async Task Clearing_A_Standalone_Required_Upload_Should_Not_Surface_MudBlazors_Own_Error()
    {
        // Arrange - the regression this pins was REAL and measured, not theoretical: with Required
        // bound, this exact sequence rendered `mud-input-error` twice and helper text reading
        // "Required" — MudBlazor's wording, not the developer's.
        //
        // Standalone (no cascaded EditContext) is the case that matters. Inside FormCraftComponent
        // the EditForm supplies one and the error is swallowed, so a test that only renders the
        // whole form green-lights a guarantee it never exercises.
        var model = new TestModel { Upload = new StubBrowserFile() };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IBrowserFile),
            CurrentValue = model.Upload,
        };

        var component = Render<MudBlazorFileUploadFieldComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));

        // Act - a file is present, so the toolbar carries Browse then Clear; clear it
        var buttons = component.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert - the field is now empty and unsatisfied, and MudBlazor says nothing about it
        component.FindAll(".mud-input-error").ShouldBeEmpty();
        component
            .FindAll(".mud-input-helper-text")
            .Select(e => e.TextContent.Trim())
            .ShouldNotContain("Required");
    }

    [Fact]
    public void Optional_FileUpload_Should_Not_Flag_The_Hidden_Input()
    {
        // Arrange & Act - belt-and-braces was dropped, so this holds for required fields too; it is
        // asserted on the optional field as the baseline the required case is compared against
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan"))
            .Build();

        var component = RenderConfig(config);

        // Assert
        component.FindComponent<MudFileUpload<IBrowserFile>>().Instance.Required.ShouldBeFalse();
    }

    [Fact]
    public void Blank_Label_Should_Not_Leave_The_File_Input_Without_An_Accessible_Name()
    {
        // Arrange - `Label ?? "File upload"` treated a configured blank label as a real name, so the
        // fallback never fired and the input was left with aria-label="" — no accessible name at all
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel(string.Empty).Required("A scan is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert
        component.Find("input[type=file]").GetAttribute("aria-label").ShouldBe("File upload");
    }

    [Fact]
    public void Whitespace_Label_Should_Not_Make_The_Two_Channels_Contradict_Each_Other()
    {
        // Arrange - the label gate and the description used to disagree (IsNullOrEmpty vs
        // IsNullOrWhiteSpace), so " " rendered a lone asterisk with no text beside it while the
        // description simultaneously announced the field as having no label
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("   ").Required("A scan is required"))
            .Build();

        // Act
        var component = RenderConfig(config);

        // Assert - one predicate now, so: no label rendered, hence no orphan marker...
        component.FindAll(".formcraft-required-marker").ShouldBeEmpty();

        // ...and the description agrees that there is no label to name
        var describedBy = component.FindAll(".mud-toolbar button")[0].GetAttribute("aria-describedby");
        component.Find($"#{describedBy}").TextContent.ShouldContain("This file upload is required.");
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

    /// <summary>
    /// Minimal <see cref="IBrowserFile"/> so a test can start from a field that already holds a file
    /// and then clear it — the transition from satisfied to unsatisfied.
    /// </summary>
    private sealed class StubBrowserFile : IBrowserFile
    {
        public string Name => "passport.png";

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => 1024;

        public string ContentType => "image/png";

        public Stream OpenReadStream(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default) => new MemoryStream();
    }
}
