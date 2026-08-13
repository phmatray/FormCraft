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

    /// <summary>
    /// Removing a row leaves the survivors showing their own values (#298).
    /// </summary>
    /// <remarks>
    /// Recorded because it is the half that already worked, and knowing which half is which is the
    /// point of the exercise. The item loop is a plain <c>@for</c> and there was no <c>@key</c>
    /// anywhere in the repository, so Blazor matches item components by <b>position</b> — but the
    /// displayed <i>value</i> survives that anyway, because <c>FieldComponentBase.ShouldReloadValue()</c>
    /// reloads from the model whenever the two diverge. The field <i>configuration</i> is likewise
    /// safe here, for a different reason: one configuration object is shared by every row, so a row
    /// shift never hands a component a different field.
    /// <para>
    /// What positional matching does break is component <b>identity</b> — see
    /// <see cref="Rows_Whose_Items_Compare_Equal_Should_Render_Without_A_Duplicate_Key_Error"/>.
    /// </para>
    /// </remarks>
    [Fact]
    public void Removing_A_Collection_Row_Should_Leave_The_Others_Showing_Their_Own_Values()
    {
        // Arrange - three rows with distinguishable values.
        var model = new OrderModel
        {
            Items =
            [
                new OrderItem { ProductName = "first" },
                new OrderItem { ProductName = "second" },
                new OrderItem { ProductName = "third" },
            ],
        };

        var component = this.RenderItemForm(model, CollectionItemFixture.TextItemForm());

        component.FindAll("input").Select(i => i.GetAttribute("value"))
            .ShouldBe(["first", "second", "third"]);

        // Act - drop the FIRST row, so every surviving component shifts position. Done on the model
        // rather than through the delete button because `Items` IS the model's list (the component
        // reads it through `Configuration.CollectionAccessor`), so this is the same mutation the
        // button performs — without pinning the test to the icon button's markup.
        model.Items.RemoveAt(0);
        component.Render();

        // Assert - the model lost "first", and the inputs show what the model now holds.
        model.Items.Select(i => i.ProductName).ShouldBe(["second", "third"]);
        component.FindAll("input").Select(i => i.GetAttribute("value"))
            .ShouldBe(["second", "third"]);
    }

    /// <summary>
    /// Rows whose items compare equal render without throwing — the collection loop stays unkeyed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The regression test for a fix that was tried and reverted. #298 briefly added
    /// <c>@key="Items[index]"</c> to the item loop, to stop positional matching from re-pointing a
    /// surviving component at its neighbour's data. But Blazor matches keys by <c>Equals</c>, not by
    /// reference, and the item type is constrained only to <c>new()</c> — so for a <c>record</c>, a
    /// <c>struct</c>, or any class overriding <c>Equals</c>, two rows holding equal content are a
    /// <i>duplicate key</i> and <c>RenderTreeDiffBuilder</c> throws.
    /// </para>
    /// <para>
    /// <c>AddItem()</c> adds <c>new TItem()</c>, so on a record-typed item form clicking "Add item"
    /// twice was enough to crash the render before the user had typed anything — a hard failure
    /// where there had been none, traded for a subtle state-preservation improvement. The key came
    /// out; this is what stops it going back in unexamined.
    /// </para>
    /// <para>
    /// The item type here is a <c>record</c> precisely because the shared fixture's models are all
    /// plain classes with reference equality, which is why the original suite stayed green.
    /// </para>
    /// </remarks>
    [Fact]
    public void Rows_Whose_Items_Compare_Equal_Should_Render_Without_A_Duplicate_Key_Error()
    {
        // Arrange - two rows equal by value, the shape a keyed loop rejects.
        var model = new EqualityItemModel
        {
            Items = [new EqualityItem(), new EqualityItem()],
        };

        var config = FormBuilder<EqualityItemModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();

        // Act & Assert - rendering, and then growing the collection, must not throw.
        var component = this.RenderItemForm(model, config);
        component.FindAll("input").Count.ShouldBe(2);

        model.Items.Add(new EqualityItem());
        Should.NotThrow(() => component.Render());
        component.FindAll("input").Count.ShouldBe(3);
    }

    private sealed record EqualityItem
    {
        public string ProductName { get; set; } = string.Empty;
    }

    private sealed class EqualityItemModel
    {
        public List<EqualityItem> Items { get; set; } = [];
    }

    // ---------------------------------------------------------------------------------------------
    // One representative bound attribute per field type. The text field is covered above; these are
    // the other component families, each migrated to the hook by the same mechanical edit — which is
    // exactly the kind of change that is easy to get subtly wrong in one file out of eleven and never
    // notice, because nothing fails until someone swaps a configuration in production.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void NumericField_Should_Rebind_Its_Format_When_The_Configuration_Is_Swapped()
    {
        // Arrange
        var model = new NumericModel { Amount = 3 };

        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, NumericConfiguration("N0")));

        component.FindComponent<MudNumericField<int>>().Instance.Format.ShouldBe("N0");

        // Act
        component.Render(parameters => parameters.Add(p => p.Configuration, NumericConfiguration("N2")));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Format.ShouldBe("N2");
    }

    [Fact]
    public void BooleanField_Should_Swap_Between_Checkbox_And_Switch_With_The_Configuration()
    {
        // Arrange - the boolean component wires the hook locally, because it derives from
        // FieldComponentBase directly rather than from either MudBlazor base. That makes it the one
        // most likely to be missed, and DisplayStyle is visible in the render tree rather than merely
        // in a property.
        var model = new BooleanModel();

        var component = Render<FormCraftComponent<BooleanModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, BooleanConfiguration(BooleanDisplayStyle.Checkbox)));

        component.FindComponents<MudCheckBox<bool>>().Count.ShouldBe(1);
        component.FindComponents<MudSwitch<bool>>().ShouldBeEmpty();

        // Act
        component.Render(parameters => parameters
            .Add(p => p.Configuration, BooleanConfiguration(BooleanDisplayStyle.Switch)));

        // Assert
        component.FindComponents<MudSwitch<bool>>().Count.ShouldBe(1);
        component.FindComponents<MudCheckBox<bool>>().ShouldBeEmpty();
    }

    [Fact]
    public void SelectField_Should_Rebind_Its_Options_When_The_Configuration_Is_Swapped()
    {
        // Arrange - the nastier half of the reload contract. `ResolveOptions` returns the CURRENT
        // Options for a field that configures none, so a component that did not clear first would
        // offer the user choices belonging to a field no longer on screen.
        var model = new SelectModel { Choice = "a" };

        var component = Render<FormCraftComponent<SelectModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, SelectConfiguration("a", "b", "c")));

        component.FindComponent<MudSelect<string>>().Instance.ShouldNotBeNull();

        // Act - the replacement field offers a single, different option.
        component.Render(parameters => parameters
            .Add(p => p.Configuration, SelectConfiguration("z")));

        // Assert
        var options = component.FindComponent<MudBlazorSelectFieldComponent<SelectModel, string>>()
            .Instance.Options
            .Select(o => o.Value)
            .ToList();
        options.ShouldBe(["z"]);
    }

    /// <summary>
    /// A revealed password does not stay revealed when a different field arrives (#298).
    /// </summary>
    /// <remarks>
    /// The state leak with actual consequences, and the one a "reload the properties" fix misses.
    /// <c>_passwordVisible</c> is not read from the field — the user sets it by clicking the eye — so
    /// nothing in the attribute reload touches it, and <c>GetInputType()</c> keeps returning
    /// <c>Text</c>. The new field's secret then renders in clear text, while the adornment rebuild
    /// resets the icon to the "show" glyph, so the control simultaneously displays the value and
    /// claims to be hiding it.
    /// </remarks>
    [Fact]
    public void TextField_Should_Rehide_A_Revealed_Password_When_The_Configuration_Is_Swapped()
    {
        // Arrange
        var model = new TestModel { Phone = "s3cret" };

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, PasswordConfiguration()));

        component.Find("input").GetAttribute("type").ShouldBe("password");

        // Reveal it, the way a user does.
        component.Find("button").Click();
        component.Find("input").GetAttribute("type").ShouldBe("text");

        // Act - a different configuration object declaring the same password field.
        component.Render(parameters => parameters.Add(p => p.Configuration, PasswordConfiguration()));

        // Assert - the new field's value is hidden again, matching the icon that was just reset.
        component.Find("input").GetAttribute("type").ShouldBe("password");
    }

    private static IFormConfiguration<TestModel> PasswordConfiguration() =>
        FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Phone, field => field
                .WithLabel("Secret")
                .AsPassword()
                .WithAttribute("EnablePasswordToggle", true))
            .Build();

    private static IFormConfiguration<NumericModel> NumericConfiguration(string format) =>
        FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Amount, field => field
                .WithLabel("Amount")
                .WithAttribute("Format", format))
            .Build();

    private static IFormConfiguration<BooleanModel> BooleanConfiguration(BooleanDisplayStyle style) =>
        FormBuilder<BooleanModel>
            .Create()
            .AddField(x => x.IsActive, field => field
                .WithLabel("Active")
                .WithAttribute("DisplayStyle", style))
            .Build();

    private static IFormConfiguration<SelectModel> SelectConfiguration(params string[] values) =>
        FormBuilder<SelectModel>
            .Create()
            .AddField(x => x.Choice, field => field
                .WithLabel("Choice")
                .WithAttribute(
                    "Options",
                    values.Select(v => new SelectOption<string>(v, v.ToUpperInvariant())).ToList()))
            .Build();

    private class NumericModel
    {
        public int Amount { get; set; }
    }

    private class BooleanModel
    {
        public bool IsActive { get; set; }
    }

    private class SelectModel
    {
        public string Choice { get; set; } = string.Empty;
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
