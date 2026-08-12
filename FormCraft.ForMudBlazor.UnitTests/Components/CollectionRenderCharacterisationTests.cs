namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Characterisation tests for the collection item render path (#203), written BEFORE converging it
/// onto <see cref="IFieldRendererService"/>.
/// </summary>
/// <remarks>
/// <para>
/// The existing collection suites each pin one attribute that drifted (#146 Variant, #177
/// ShrinkLabel, #184 adornments, #190 Required, …). What none of them pin is the behaviour the
/// refactor must carry across unchanged regardless of which attribute is in fashion: that a row's
/// input writes to <b>that row's</b> item, that values stay with their rows across add / remove /
/// reorder, that every item field kind notifies the parent <see cref="EditContext"/> under a nested
/// <c>Items[i].Field</c> identifier, and what each of the four item field kinds actually renders.
/// </para>
/// <para>
/// These describe <b>today</b>, not the target. Tests named <c>Today_…</c> pin a divergence from the
/// component path that the convergence is expected to REMOVE — they are the inventory of deliberate
/// behaviour changes, and each one flipping in #203 is the point rather than a regression. Every
/// other test here must survive the refactor untouched.
/// </para>
/// </remarks>
public class CollectionRenderCharacterisationTests : MudBlazorTestBase
{
    // ---------------------------------------------------------------------------------------
    // Per-item value binding: a row's input must write to that row's item, and values must stay
    // with their rows as the collection is mutated. The item form renders in a keyless loop, so
    // Blazor pairs rows positionally — the hazard #184 flagged and nothing yet asserts on values
    // alone.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Editing_A_Row_Should_Write_Only_That_Rows_Item()
    {
        // Arrange
        var model = new OrderModel
        {
            Items = { new OrderItem { ProductName = "first" }, new OrderItem { ProductName = "second" } },
        };

        var component = RenderOrderForm(model);

        // Act - type into the SECOND row
        component.FindAll("input")[1].Input("edited");

        // Assert - only that row's item changed
        model.Items.Select(i => i.ProductName).ShouldBe(new[] { "first", "edited" });
    }

    [Fact]
    public void Adding_A_Row_Should_Preserve_The_Existing_Row_Values()
    {
        // Arrange
        var model = new OrderModel
        {
            Items = { new OrderItem { ProductName = "first" }, new OrderItem { ProductName = "second" } },
        };

        var component = RenderOrderForm(model, allowAdd: true);

        // Act - the collection's own Add button (the only MudButton; the row controls are icon buttons)
        component.FindComponent<MudButton>().Find("button").Click();

        // Assert - the new row is appended empty and the existing values are untouched, in order
        model.Items.Select(i => i.ProductName).ShouldBe(new[] { "first", "second", string.Empty });
        RenderedProductNames(component).ShouldBe(new[] { "first", "second", string.Empty });
    }

    [Fact]
    public void Removing_A_Row_Should_Keep_The_Remaining_Values_With_Their_Rows()
    {
        // Arrange - three rows so removing the middle one can be told apart from removing the last
        var model = new OrderModel
        {
            Items =
            {
                new OrderItem { ProductName = "first" },
                new OrderItem { ProductName = "second" },
                new OrderItem { ProductName = "third" },
            },
        };

        var component = RenderOrderForm(model, allowRemove: true);

        // Act - remove the middle row
        component.FindAll("button[aria-label='Remove item']")[1].Click();

        // Assert - the survivors keep their own values rather than shifting into the gap
        model.Items.Select(i => i.ProductName).ShouldBe(new[] { "first", "third" });
        RenderedProductNames(component).ShouldBe(new[] { "first", "third" });
    }

    [Fact]
    public void Reordering_Should_Move_Values_With_Their_Rows()
    {
        // Arrange - three rows, so a positional-pairing bug cannot pass by symmetry the way a
        // two-row swap can.
        var model = new OrderModel
        {
            Items =
            {
                new OrderItem { ProductName = "first" },
                new OrderItem { ProductName = "second" },
                new OrderItem { ProductName = "third" },
            },
        };

        var component = RenderOrderForm(model, allowReorder: true);

        // Act - move the third row up
        component.FindAll("button[aria-label='Move up']")[2].Click();

        // Assert - both the model and what each row displays
        model.Items.Select(i => i.ProductName).ShouldBe(new[] { "first", "third", "second" });
        RenderedProductNames(component).ShouldBe(new[] { "first", "third", "second" });
    }

    [Fact]
    public void Editing_A_Reordered_Row_Should_Still_Write_To_Its_Own_Item()
    {
        // Arrange - the composition the two tests above cannot catch separately: a value edit
        // AFTER a reorder must follow the row, not the original index the callback was built with.
        var model = new OrderModel
        {
            Items = { new OrderItem { ProductName = "first" }, new OrderItem { ProductName = "second" } },
        };

        var component = RenderOrderForm(model, allowReorder: true);
        component.FindAll("button[aria-label='Move up']")[1].Click();

        // Act - edit what is now the top row (originally "second")
        component.FindAll("input")[0].Input("edited");

        // Assert
        model.Items.Select(i => i.ProductName).ShouldBe(new[] { "edited", "first" });
    }

    // ---------------------------------------------------------------------------------------
    // Nested FieldIdentifier notification (#91). CollectionFieldEditContextTests pins this for a
    // TEXT field only; every item field kind routes through the same UpdateItemFieldValue today,
    // and the refactor must keep all four wired.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Editing_A_Text_Item_Field_Should_Notify_The_Parent_EditContext()
    {
        // Arrange
        var model = new MixedModel { Rows = { new MixedRow() } };
        var (component, editContext) = RenderMixedForm(model);

        // Act
        component.FindComponents<MudTextField<string>>()
            .First(t => t.Instance.Label == "Name")
            .Find("input")
            .Input("typed");

        // Assert - rooted at the PARENT model under the nested path, per Blazor convention
        editContext.ShouldNotBeNull();
        editContext!.IsModified(new FieldIdentifier(model, "Rows[0].Name")).ShouldBeTrue();
        model.Rows[0].Name.ShouldBe("typed");
    }

    [Fact]
    public async Task Editing_A_Numeric_Item_Field_Should_Notify_The_Parent_EditContext()
    {
        // Arrange
        var model = new MixedModel { Rows = { new MixedRow() } };
        var (component, editContext) = RenderMixedForm(model);
        var numeric = component.FindComponent<MudNumericField<int>>();

        // Act
        await component.InvokeAsync(() => numeric.Instance.ValueChanged.InvokeAsync(7));

        // Assert
        editContext.ShouldNotBeNull();
        editContext!.IsModified(new FieldIdentifier(model, "Rows[0].Quantity")).ShouldBeTrue();
        model.Rows[0].Quantity.ShouldBe(7);
    }

    [Fact]
    public async Task Editing_A_Boolean_Item_Field_Should_Notify_The_Parent_EditContext()
    {
        // Arrange
        var model = new MixedModel { Rows = { new MixedRow() } };
        var (component, editContext) = RenderMixedForm(model);
        var checkbox = component.FindComponent<MudCheckBox<bool>>();

        // Act
        await component.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(true));

        // Assert
        editContext.ShouldNotBeNull();
        editContext!.IsModified(new FieldIdentifier(model, "Rows[0].IsGift")).ShouldBeTrue();
        model.Rows[0].IsGift.ShouldBeTrue();
    }

    [Fact]
    public async Task Editing_A_Date_Item_Field_Should_Notify_The_Parent_EditContext()
    {
        // Arrange
        var model = new MixedModel { Rows = { new MixedRow() } };
        var (component, editContext) = RenderMixedForm(model);
        var picker = component.FindComponent<MudDatePicker>();

        // Act
        await component.InvokeAsync(() => picker.Instance.DateChanged.InvokeAsync(new DateTime(2031, 3, 4)));

        // Assert
        editContext.ShouldNotBeNull();
        editContext!.IsModified(new FieldIdentifier(model, "Rows[0].When")).ShouldBeTrue();
        model.Rows[0].When.ShouldBe(new DateTime(2031, 3, 4));
    }

    [Fact]
    public void Every_Item_Field_Kind_Should_Render_Its_Own_Nested_Validation_Slot()
    {
        // Arrange - the validation message component is emitted per item field by RenderItemFields,
        // keyed to the nested identifier. Converging the render path must not drop it for any kind.
        var model = new MixedModel { Rows = { new MixedRow(), new MixedRow() } };
        var (component, _) = RenderMixedForm(model);

        // Assert - one slot per field per row (4 fields x 2 rows)
        component.FindComponents<FieldValidationMessage>()
            .Select(m => m.Instance.FieldName)
            .ShouldBe(new[]
            {
                "Rows[0].Name", "Rows[0].Quantity", "Rows[0].IsGift", "Rows[0].When",
                "Rows[1].Name", "Rows[1].Quantity", "Rows[1].IsGift", "Rows[1].When",
            });
    }

    // ---------------------------------------------------------------------------------------
    // The rendered attribute set, per item field kind. These are the values an unconfigured item
    // field renders today; the refactor must not move them silently.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Text_Item_Field_Should_Render_Its_Attribute_Set()
    {
        // Arrange & Act
        var field = RenderMixedForm(new MixedModel { Rows = { new MixedRow() } }).Component
            .FindComponents<MudTextField<string>>()
            .First(t => t.Instance.Label == "Name")
            .Instance;

        // Assert
        field.Label.ShouldBe("Name");
        field.Variant.ShouldBe(Variant.Outlined);
        field.Margin.ShouldBe(Margin.Dense);
        field.ShrinkLabel.ShouldBeTrue();
        field.Immediate.ShouldBeTrue();
        field.Required.ShouldBeFalse();
        field.Adornment.ShouldBe(Adornment.None);
        field.InputType.ShouldBe(InputType.Text);
        field.Lines.ShouldBe(1);
        field.MaxLength.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void Numeric_Item_Field_Should_Render_Its_Attribute_Set()
    {
        // Arrange & Act
        var field = RenderMixedForm(new MixedModel { Rows = { new MixedRow() } }).Component
            .FindComponent<MudNumericField<int>>().Instance;

        // Assert
        field.Label.ShouldBe("Quantity");
        field.Variant.ShouldBe(Variant.Outlined);
        field.Margin.ShouldBe(Margin.Dense);
        field.ShrinkLabel.ShouldBeTrue();
        field.Immediate.ShouldBeTrue();
        field.Required.ShouldBeFalse();
        field.Adornment.ShouldBe(Adornment.None);
        field.Culture.ShouldBe(System.Globalization.CultureInfo.InvariantCulture);
        field.HideSpinButtons.ShouldBeFalse();
    }

    [Fact]
    public void Date_Item_Field_Should_Render_Its_Attribute_Set()
    {
        // Arrange & Act
        var field = RenderMixedForm(new MixedModel { Rows = { new MixedRow() } }).Component
            .FindComponent<MudDatePicker>().Instance;

        // Assert - MudDatePicker's own End + calendar icon survives, per #217
        field.Label.ShouldBe("When");
        field.Variant.ShouldBe(Variant.Outlined);
        field.Margin.ShouldBe(Margin.Dense);
        field.ShrinkLabel.ShouldBeTrue();
        field.Required.ShouldBeFalse();
        field.Adornment.ShouldBe(Adornment.End);
        field.AdornmentIcon.ShouldBe(Icons.Material.Filled.Event);
    }

    [Fact]
    public void Boolean_Item_Field_Should_Render_Its_Attribute_Set()
    {
        // Arrange & Act
        var field = RenderMixedForm(new MixedModel { Rows = { new MixedRow() } }).Component
            .FindComponent<MudCheckBox<bool>>().Instance;

        // Assert - MudCheckBox has no Variant/Placeholder/HelperText concept, so the shared
        // presentation set is genuinely inapplicable here; what must hold is label and state.
        field.Label.ShouldBe("Gift");
        field.ReadOnly.ShouldBeFalse();
        field.Disabled.ShouldBeFalse();
    }

    // ---------------------------------------------------------------------------------------
    // Today_… : divergences from the component path that #203 is expected to REMOVE. Each of
    // these flipping is a deliberate behaviour change, stated in the release note.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void Today_A_Boolean_Item_Field_Ignores_DisplayStyle()
    {
        // A standalone bool field honours .WithAttribute("DisplayStyle", BooleanDisplayStyle.Switch)
        // and renders a MudSwitch; RenderBooleanField hard-codes MudCheckBox and never reads it, so
        // the same configuration renders differently inside .WithItemForm(...).
        var config = FormBuilder<MixedModel>
            .Create()
            .AddCollectionField(x => x.Rows, collection => collection
                .WithLabel("Rows")
                .WithItemForm(item => item
                    .AddField(x => x.IsGift, field => field
                        .WithLabel("Gift")
                        .WithAttribute("DisplayStyle", BooleanDisplayStyle.Switch))))
            .Build();

        var component = Render<FormCraftComponent<MixedModel>>(parameters => parameters
            .Add(p => p.Model, new MixedModel { Rows = { new MixedRow() } })
            .Add(p => p.Configuration, config));

        component.FindComponents<MudSwitch<bool>>().ShouldBeEmpty();
        component.FindComponents<MudCheckBox<bool>>().Count.ShouldBe(1);
    }

    [Fact]
    public void Today_A_Date_Item_Field_Is_Not_Editable()
    {
        // The component path binds Editable="true" on MudDatePicker; the collection path binds
        // nothing, so MudDatePicker's own default stands and the item field's date cannot be typed.
        RenderMixedForm(new MixedModel { Rows = { new MixedRow() } }).Component
            .FindComponent<MudDatePicker>().Instance.Editable.ShouldBeFalse();
    }

    [Fact]
    public void Today_A_Date_Item_Field_Ignores_MinDate_And_MaxDate()
    {
        // MinDate/MaxDate are honoured by the component path (pinned by
        // RenderPipelineParityTests.DateTimeField_Should_Pass_MinDate_And_MaxDate_To_DatePicker)
        // and never forwarded here.
        var config = FormBuilder<MixedModel>
            .Create()
            .AddCollectionField(x => x.Rows, collection => collection
                .WithLabel("Rows")
                .WithItemForm(item => item
                    .AddField(x => x.When, field => field
                        .WithLabel("When")
                        .WithAttribute("MinDate", new DateTime(2020, 1, 1))
                        .WithAttribute("MaxDate", new DateTime(2030, 12, 31)))))
            .Build();

        var picker = Render<FormCraftComponent<MixedModel>>(parameters => parameters
                .Add(p => p.Model, new MixedModel { Rows = { new MixedRow() } })
                .Add(p => p.Configuration, config))
            .FindComponent<MudDatePicker>().Instance;

        picker.MinDate.ShouldBeNull();
        picker.MaxDate.ShouldBeNull();
    }

    [Fact]
    public void Today_A_Numeric_Item_Field_Ignores_Min_Max_And_Step()
    {
        // The component path resolves Min/Max/Step (falling back to the type's own range); the
        // collection path forwards none of them.
        var config = FormBuilder<MixedModel>
            .Create()
            .AddCollectionField(x => x.Rows, collection => collection
                .WithLabel("Rows")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field => field
                        .WithLabel("Quantity")
                        .WithAttribute("Min", 5)
                        .WithAttribute("Max", 50))))
            .Build();

        var numeric = Render<FormCraftComponent<MixedModel>>(parameters => parameters
                .Add(p => p.Model, new MixedModel { Rows = { new MixedRow() } })
                .Add(p => p.Configuration, config))
            .FindComponent<MudNumericField<int>>().Instance;

        // Asserted as "the configured value never arrives" rather than against MudBlazor's own
        // default, so the test states the divergence itself and does not break when that default
        // changes underneath it.
        numeric.Min.ShouldNotBe(5);
        numeric.Max.ShouldNotBe(50);
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    private static IReadOnlyList<string?> RenderedProductNames(
        IRenderedComponent<FormCraftComponent<OrderModel>> component) =>
        component.FindComponents<MudTextField<string>>()
            .Select(t => t.Instance.Value ?? string.Empty)
            .ToList();

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderOrderForm(
        OrderModel model,
        bool allowReorder = false,
        bool allowAdd = false,
        bool allowRemove = false)
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection =>
            {
                collection.WithLabel("Items");
                if (allowReorder)
                {
                    collection.AllowReorder();
                }

                if (allowAdd)
                {
                    collection.AllowAdd();
                }

                if (allowRemove)
                {
                    collection.AllowRemove();
                }

                collection.WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product")));
            })
            .Build();

        return Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));
    }

    /// <summary>
    /// Renders a four-kind item form (text / numeric / bool / date) next to a
    /// <see cref="MudPopoverProvider"/> so the date picker works, and hands back the form's
    /// <see cref="EditContext"/>.
    /// </summary>
    private (IRenderedComponent<FormCraftComponent<MixedModel>> Component, EditContext? EditContext)
        RenderMixedForm(MixedModel model)
    {
        var config = FormBuilder<MixedModel>
            .Create()
            .AddCollectionField(x => x.Rows, collection => collection
                .WithLabel("Rows")
                .WithItemForm(item => item
                    .AddField(x => x.Name, field => field.WithLabel("Name"))
                    .AddField(x => x.Quantity, field => field.WithLabel("Quantity"))
                    .AddField(x => x.IsGift, field => field.WithLabel("Gift"))
                    .AddField(x => x.When, field => field.WithLabel("When"))))
            .Build();

        EditContext? editContext = null;

        var host = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<FormCraftComponent<MixedModel>>(1);
            builder.AddComponentParameter(2, "Model", model);
            builder.AddComponentParameter(3, "Configuration", config);
            builder.AddComponentParameter(4, "OnEditContextCreated",
                EventCallback.Factory.Create<EditContext>(this, ctx => editContext = ctx));
            builder.CloseComponent();
        });

        return (host.FindComponent<FormCraftComponent<MixedModel>>(), editContext);
    }

    private class OrderModel
    {
        public List<OrderItem> Items { get; set; } = new();
    }

    private class OrderItem
    {
        public string ProductName { get; set; } = string.Empty;
    }

    private class MixedModel
    {
        public List<MixedRow> Rows { get; set; } = new();
    }

    private class MixedRow
    {
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public bool IsGift { get; set; }

        public DateTime When { get; set; }
    }
}
