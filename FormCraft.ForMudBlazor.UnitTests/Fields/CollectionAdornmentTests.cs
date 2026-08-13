using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields honor <c>.WithAdornment(...)</c> (#184).
/// <para>
/// Written when item fields rendered through CollectionFieldComponent's own imperative
/// RenderTreeBuilder path, which resolved presentation attributes in <c>AddCommonFieldAttributes</c>
/// rather than through <c>MudBlazorFieldComponentBase</c> — so it needed coverage of its own. Before
/// #184 the three adornment attributes were silently dropped there while the component path
/// forwarded them, so the same builder call rendered differently depending on whether the field sat
/// inside <c>.WithItemForm(...)</c>.
/// </para>
/// <para>
/// #203 deleted that second path: item fields go through <c>IFieldRendererService</c> like every
/// other field, so these assertions now exercise the same component a standalone field does. They
/// are kept — and kept passing unmodified — precisely because that is the claim worth holding: the
/// behaviour #184 had to add by hand is now inherited, and this suite is what would notice if the
/// item placement ever stopped inheriting it.
/// </para>
/// <para>
/// Models and item-form builders come from <see cref="CollectionItemFixture"/> (#205). The text-path
/// tests render <c>NewOrder("Widget")</c> — the <b>seeded</b> value — because an adornment is about
/// what a populated field looks like; the sibling Required suite deliberately renders the blank seed
/// instead. Passing it at each call site keeps that difference visible rather than burying it in a
/// local helper, which is how the two suites' copies drifted in the first place.
/// </para>
/// <para>
/// The reorder test at the bottom keeps its own <c>MudPopoverProvider</c> wrapper, but its model now
/// comes from the fixture too (#282). It needs rows of <i>differing</i> field types in one item form
/// — which the one-field-per-path builders cannot express — so the fixture grew
/// <see cref="CollectionItemFixture.MultiFieldItemForm"/> for it and for
/// <c>CollectionRenderCharacterisationTests</c>, the two suites that were each carrying a private
/// <c>MixedRow</c>. The two copies had already drifted apart, which is what made the shared row worth
/// adding rather than a speculative generalisation.
/// </para>
/// </summary>
public class CollectionAdornmentTests : MudBlazorTestBase
{
    [Fact]
    public void ItemField_Should_Render_A_Start_Adornment()
    {
        // Arrange & Act
        var component = this.RenderItemForm(NewOrder("Widget"), TextItemForm(field => field
            .WithAdornment(Icons.Material.Filled.Search, Adornment.Start)));

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.Start);
        textField.AdornmentIcon.ShouldBe(Icons.Material.Filled.Search);
    }

    [Fact]
    public void ItemField_Should_Render_The_Configured_Adornment_Color()
    {
        // Arrange & Act - WithAdornment always writes all three attributes, colour included
        var component = this.RenderItemForm(NewOrder("Widget"), TextItemForm(field => field
            .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, Color.Secondary)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.AdornmentColor
            .ShouldBe(Color.Secondary);
    }

    [Fact]
    public void ItemField_Without_An_Adornment_Should_Render_None()
    {
        // Arrange & Act - unchanged from before #184: no adornment configured, none rendered
        var component = this.RenderItemForm(NewOrder("Widget"), TextItemForm());

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.None);
        textField.AdornmentIcon.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void ItemField_With_Only_An_Adornment_Position_Should_Default_Its_Color()
    {
        // Arrange & Act - a field that set "Adornment" through raw WithAttribute has no colour to
        // read, so the resolver must supply one rather than assume all three are present.
        var component = this.RenderItemForm(NewOrder("Widget"), TextItemForm(field => field
            .WithAttribute("Adornment", Adornment.End)));

        // Assert
        var textField = component.FindComponent<MudTextField<string>>().Instance;
        textField.Adornment.ShouldBe(Adornment.End);
        textField.AdornmentColor.ShouldBe(Color.Default);
    }

    [Fact]
    public void NumericItemField_Should_Render_An_End_Adornment()
    {
        // Arrange & Act - WithAdornment is declared on FieldBuilder<TModel, string> only, so a numeric
        // field configures the same three attributes through raw WithAttribute. The renderer must
        // honour them either way: it reads AdditionalAttributes, not the builder method.
        var component = this.RenderItemForm(NewBasket(), NumericItemForm(field => field
            .WithAttribute("Adornment", Adornment.End)
            .WithAttribute("AdornmentIcon", Icons.Material.Filled.Numbers)
            .WithAttribute("AdornmentColor", Color.Primary)));

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.End);
        numeric.AdornmentIcon.ShouldBe(Icons.Material.Filled.Numbers);
        numeric.AdornmentColor.ShouldBe(Color.Primary);
    }

    [Fact]
    public void NumericItemField_Without_An_Adornment_Should_Render_None()
    {
        // Arrange & Act - unchanged from before #184
        var component = this.RenderItemForm(NewBasket(), NumericItemForm());

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Adornment.ShouldBe(Adornment.None);
    }

    [Fact]
    public void BooleanItemField_With_An_Adornment_Should_Render_Without_One()
    {
        // Arrange - MudCheckBox has no adornment concept at all, so configuring one must be inert
        // rather than a throw.
        //
        // The REASON changed in #203, and the change is the point. This used to be inert because
        // RenderBooleanField was a bespoke renderer that set its own attributes and took no part in
        // the adornment forward — inert by omission, on a path that also silently ignored
        // DisplayStyle and every other shared setting. It is now inert because
        // MudBlazorBooleanFieldComponent — the very same component a standalone bool field renders
        // through — binds no adornment either. Same observable result, arrived at by inheritance
        // instead of by a second implementation happening to lack the feature.
        //
        // So the assertion is strengthened to say what actually matters now: not merely "no icon",
        // but "the same as the standalone field", which is what stops this becoming a divergence
        // again the day MudCheckBox grows an adornment.
        var standaloneConfig = FormBuilder<BasketLine>
            .Create()
            .AddField(x => x.IsGift, field => field
                .WithLabel("Gift")
                .WithAttribute("Adornment", Adornment.Start)
                .WithAttribute("AdornmentIcon", Icons.Material.Filled.Search))
            .Build();

        // Act
        var component = this.RenderItemForm(NewBasket(), BooleanItemForm(field => field
            .WithAttribute("Adornment", Adornment.Start)
            .WithAttribute("AdornmentIcon", Icons.Material.Filled.Search)));

        var standalone = Render<FormCraftComponent<BasketLine>>(parameters => parameters
            .Add(p => p.Model, new BasketLine())
            .Add(p => p.Configuration, standaloneConfig));

        // Assert
        var itemCheckbox = component.FindComponent<MudCheckBox<bool>>().Instance;
        itemCheckbox.Label.ShouldBe("Gift");
        component.Markup.ShouldNotContain(Icons.Material.Filled.Search);

        // ...and the standalone field is inert in exactly the same way, which is the convergence.
        standalone.Markup.ShouldNotContain(Icons.Material.Filled.Search);
        itemCheckbox.Label.ShouldBe(standalone.FindComponent<MudCheckBox<bool>>().Instance.Label);
    }

    [Fact]
    public void DateItemField_Should_Keep_Its_Calendar_Icon()
    {
        // Arrange & Act - MudDatePicker's own default is Adornment.End with a calendar icon, unlike
        // the text and numeric fields whose default is None. Binding an UNSET adornment onto it
        // would silently erase that icon, which is why the date component supplies MudDatePicker's
        // own defaults rather than the base class's None (#217, moved into the component by #203).
        // The sibling test below pins the other half: a configured adornment still wins.
        var component = this.RenderItemForm(NewAppointment(), DateItemForm());

        // Assert
        var picker = component.FindComponent<MudDatePicker>().Instance;
        picker.Adornment.ShouldBe(Adornment.End);
        picker.AdornmentIcon.ShouldNotBeNullOrEmpty();
        picker.AdornmentIcon.ShouldBe(Icons.Material.Filled.Event);
    }

    [Fact]
    public void DateItemField_Should_Honour_A_Configured_Adornment()
    {
        // Arrange & Act - #217. The date path used to pass `rendersAdornment: false`, so `.WithAdornment(...)`
        // on a date item field was accepted and silently dropped — the same class of silent discard
        // #184, #191 and #192 each closed elsewhere. It now forwards a configured adornment while
        // keeping MudDatePicker's own End + calendar icon as the DEFAULT (pinned above), so the two
        // cases no longer trade off against each other.
        var component = this.RenderItemForm(NewAppointment(), DateItemForm(field => field
            .WithAttribute("Adornment", Adornment.Start)
            .WithAttribute("AdornmentIcon", Icons.Material.Filled.Search)
            .WithAttribute("AdornmentColor", Color.Secondary)));

        // Assert - the configured adornment wins over MudDatePicker's default.
        var picker = component.FindComponent<MudDatePicker>().Instance;
        picker.Adornment.ShouldBe(Adornment.Start);
        picker.AdornmentIcon.ShouldBe(Icons.Material.Filled.Search);
        picker.AdornmentColor.ShouldBe(Color.Secondary);
    }

    [Fact]
    public void Reordering_A_Mixed_Item_Form_Should_Move_Values_With_Their_Rows()
    {
        // Arrange - the adornment forward makes a text row emit more render-tree frames than a date
        // row, so a mixed item form now has rows of differing frame counts in a keyless loop. If the
        // sequence numbers were computed rather than source-position constants, Blazor would pair
        // frames positionally across a reorder and a value could stay on the row it was on.
        //
        // This needs one item form holding fields of DIFFERENT types, which the fixture's
        // one-field-per-path builders cannot express — so it goes through MultiFieldItemForm, the
        // shared four-field row added for exactly this (#282). Only the text and collection
        // callbacks are used here; the numeric and boolean fields render at their defaults and are
        // incidental to this test, which asserts on the adorned text fields and the date pickers.
        var model = NewMixedItems(
            new MixedItem { Name = "first", When = new DateTime(2020, 1, 1) },
            new MixedItem { Name = "second", When = new DateTime(2030, 12, 31) });

        var config = MultiFieldItemForm(
            configureText: field => field.WithAdornment(Icons.Material.Filled.Search, Adornment.Start),
            configureCollection: collection => collection.AllowReorder());

        var component = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<FormCraftComponent<MixedItemModel>>(1);
            builder.AddComponentParameter(2, "Model", model);
            builder.AddComponentParameter(3, "Configuration", config);
            builder.CloseComponent();
        });

        // Act - move the second row up
        component.FindAll("button[aria-label='Move up']")[1].Click();

        // Assert - the model reordered, and each rendered row shows its own row's values
        model.Rows.Select(r => r.Name).ShouldBe(new[] { "second", "first" });

        // MudDatePicker embeds a MudTextField<string> of its own, so select only our name fields
        var texts = component.FindComponents<MudTextField<string>>()
            .Select(t => t.Instance)
            .Where(t => t.Label == "Name")
            .ToList();

        texts.Select(t => t.Value).ShouldBe(new[] { "second", "first" });
        texts.Select(t => t.Adornment).ShouldAllBe(a => a == Adornment.Start);

        component.FindComponents<MudDatePicker>()
            .Select(p => p.Instance.Date)
            .ShouldBe(new DateTime?[] { new DateTime(2030, 12, 31), new DateTime(2020, 1, 1) });
    }
}
