namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// The canonical models and item-form builders the collection-item attribute suites share (#205).
/// <para>
/// Every such suite exercises the same handful of field types inside <c>.WithItemForm(...)</c>, and
/// each one used to carry its own copy of the same model pairs and the same two helpers. The copies
/// drifted in incidentals (whether <c>ProductName</c> was seeded, whether <c>BasketLine</c> declared
/// <c>IsGift</c>) and they propagated mistakes as faithfully as they propagated code — a miscounted
/// comment claiming the shared attribute block fed "the text and numeric paths" survived a copy and
/// hid the date path from coverage until review caught it.
/// </para>
/// <para>
/// <b>The five field types.</b> The models are chosen so that a suite using this fixture covers all
/// of them by default rather than by remembering to:
/// <list type="bullet">
/// <item><description><see cref="OrderItem"/> — <c>string</c>, rendered by <c>MudTextField</c>.</description></item>
/// <item><description><see cref="BasketLine"/> — <c>int</c>, rendered by <c>MudNumericField</c>.</description></item>
/// <item><description><see cref="AppointmentSlot"/> — <c>DateTime</c>, rendered by <c>MudDatePicker</c>.</description></item>
/// <item><description><see cref="BasketLine"/> again — <c>bool</c>, rendered by <c>MudCheckBox</c>,
/// which binds neither adornments nor <c>Required</c> and where those are therefore pinned as
/// inert.</description></item>
/// <item><description><see cref="PricedLine"/> — <c>decimal</c>, rendered by
/// <c>MudNumericField&lt;decimal&gt;</c>. A distinct closed generic from the <c>int</c> one, and the
/// only one where culture-sensitive parsing is observable (#218, #258).</description></item>
/// </list>
/// All but the boolean bind the shared presentation attributes; the checkbox does not. That asymmetry
/// is the whole reason the boolean model lives here rather than being left to each suite.
/// </para>
/// <para>
/// <b>One shape beyond the field types.</b> Every builder above produces a form containing nothing but
/// a collection. <see cref="NamedOrderModel"/> / <see cref="NamedOrderItem"/> and
/// <see cref="RootFieldAndItemForm"/> are the exception: a root-level field <i>beside</i> the
/// collection, with the two members sharing the name <c>Name</c>. Look here before hand-rolling a
/// model for a form that is not collection-only (#213, #258).
/// </para>
/// <para>
/// ⚠️ <b>These were four separate render paths when this fixture was written (#205).</b> Item fields
/// went through a hand-written <c>RenderTreeBuilder</c> in <c>CollectionFieldComponent</c>, with a
/// shared <c>AddCommonFieldAttributes</c> feeding the first three and a bespoke method for the
/// fourth — so a suite could pass for a standalone field and fail for the same field in an item
/// form. #203 deleted that renderer: every field now goes through <c>IFieldRendererService</c> and
/// the same per-type component regardless of placement. The groupings above survive because they are
/// still distinct <i>components</i>, but they are no longer distinct <i>paths</i>, and a suite built
/// on this fixture is now checking that the item placement keeps inheriting the component's
/// behaviour rather than that a second implementation matches it. The decimal pair was added in #258
/// under that later reading — not a fifth path, a fifth component a real suite needed to name.
/// </para>
/// <para>
/// <b>Seeds are the caller's choice.</b> The factories default to the model's own default and take an
/// explicit seed, because the suites genuinely disagree: <c>CollectionRequiredTests</c> needs a blank
/// <c>ProductName</c> so its validator fails, while <c>CollectionAdornmentTests</c> renders against a
/// populated one. Picking one for everybody would silently change a test's meaning.
/// </para>
/// <para>
/// Models are deliberately dumb and stable — no behaviour, no validation attributes, no computed
/// members. Everything a suite wants to vary goes through the <c>configure</c> callback on the item-form
/// builders instead, so a change here cannot ripple into what an unrelated suite asserts.
/// </para>
/// </summary>
internal static class CollectionItemFixture
{
    /// <summary>Creates an order with a single item, blank unless a <paramref name="productName"/> is given.</summary>
    internal static OrderModel NewOrder(string productName = "") =>
        new() { Items = { new OrderItem { ProductName = productName } } };

    /// <summary>
    /// Creates an order with one item per <paramref name="productNames"/> entry — the multi-row
    /// counterpart of <see cref="NewOrder"/>.
    /// </summary>
    /// <remarks>
    /// Several suites need more than one row: to prove a per-row handler is told which row it came
    /// from, or that a warning about a field's configuration is reported once rather than once per
    /// row. Each was building its own <c>Items = { new OrderItem { … }, new OrderItem { … } }</c>
    /// literal, which is the duplication this fixture exists to remove — so the row count is a
    /// parameter here rather than a reason to hand-roll the model.
    /// </remarks>
    internal static OrderModel NewOrderWithItems(params string[] productNames)
    {
        var model = new OrderModel();
        foreach (var productName in productNames)
        {
            model.Items.Add(new OrderItem { ProductName = productName });
        }

        return model;
    }

    /// <summary>Creates a basket with a single line, blank unless seeds are given.</summary>
    internal static BasketModel NewBasket(int quantity = 0, bool isGift = false) =>
        new() { Lines = { new BasketLine { Quantity = quantity, IsGift = isGift } } };

    /// <summary>Creates an appointment with a single slot, blank unless a <paramref name="when"/> is given.</summary>
    internal static AppointmentModel NewAppointment(DateTime when = default) =>
        new() { Slots = { new AppointmentSlot { When = when } } };

    /// <summary>Creates a priced basket with a single line, blank unless a <paramref name="price"/> is given.</summary>
    internal static PricedBasketModel NewPricedBasket(decimal price = 0m) =>
        new() { Lines = { new PricedLine { Price = price } } };

    /// <summary>
    /// Creates an order carrying a root-level <c>Name</c> beside a single item that is also called
    /// <c>Name</c>, both blank unless seeded.
    /// </summary>
    internal static NamedOrderModel NewNamedOrder(string name = "", string itemName = "") =>
        new() { Name = name, Items = { new NamedOrderItem { Name = itemName } } };

    /// <summary>
    /// A collection whose item form holds one <c>string</c> field labelled "Product" — the text path.
    /// </summary>
    internal static IFormConfiguration<OrderModel> TextItemForm(
        Action<FieldBuilder<OrderItem, string>>? configure = null) =>
        FormBuilder<OrderModel>
            .Create()
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field =>
                    {
                        field.WithLabel("Product");
                        configure?.Invoke(field);
                    })))
            .Build();

    /// <summary>
    /// A collection whose item form holds one <c>int</c> field labelled "Quantity" — the numeric path.
    /// </summary>
    internal static IFormConfiguration<BasketModel> NumericItemForm(
        Action<FieldBuilder<BasketLine, int>>? configure = null) =>
        FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field =>
                    {
                        field.WithLabel("Quantity");
                        configure?.Invoke(field);
                    })))
            .Build();

    /// <summary>
    /// A collection whose item form holds one <c>DateTime</c> field labelled "When" — the date path.
    /// </summary>
    internal static IFormConfiguration<AppointmentModel> DateItemForm(
        Action<FieldBuilder<AppointmentSlot, DateTime>>? configure = null) =>
        FormBuilder<AppointmentModel>
            .Create()
            .AddCollectionField(x => x.Slots, collection => collection
                .WithLabel("Slots")
                .WithItemForm(item => item
                    .AddField(x => x.When, field =>
                    {
                        field.WithLabel("When");
                        configure?.Invoke(field);
                    })))
            .Build();

    /// <summary>
    /// A collection whose item form holds one <c>bool</c> field labelled "Gift" — the checkbox, the
    /// one component that binds neither adornments nor <c>Required</c>.
    /// </summary>
    internal static IFormConfiguration<BasketModel> BooleanItemForm(
        Action<FieldBuilder<BasketLine, bool>>? configure = null) =>
        FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.IsGift, field =>
                    {
                        field.WithLabel("Gift");
                        configure?.Invoke(field);
                    })))
            .Build();

    /// <summary>
    /// A collection whose item form holds one <c>decimal</c> field labelled "Price" — the decimal
    /// component. <c>MudNumericField&lt;decimal&gt;</c> is a different closed generic from
    /// <c>MudNumericField&lt;int&gt;</c>, so a suite that asserts on one has said nothing about the
    /// other; culture-sensitive parsing in particular only shows up on the decimal one.
    /// </summary>
    internal static IFormConfiguration<PricedBasketModel> DecimalItemForm(
        Action<FieldBuilder<PricedLine, decimal>>? configure = null) =>
        FormBuilder<PricedBasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Price, field =>
                    {
                        field.WithLabel("Price");
                        configure?.Invoke(field);
                    })))
            .Build();

    /// <summary>
    /// A root-level <c>string</c> field <i>beside</i> a collection whose item form holds a
    /// <c>string</c> field of the same name — the only shape here that is not a form containing
    /// nothing but a collection.
    /// <para>
    /// Both fields are called <c>Name</c> deliberately. Form-wide diagnostics key conflicts by field
    /// identity, and a bare field name is unique only within one item form, so a top-level field and
    /// an item field sharing a name are exactly what an under-qualified key merges (#213). Flattening
    /// this onto the plain <see cref="OrderModel"/> would delete the subject.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The two default <i>labels</i> differ ("Name" and "Item name") even though the two
    /// <i>members</i> deliberately share the name <c>Name</c>. Only the member names are
    /// load-bearing: the collector keys conflicts by field identity, not by label — pinned by
    /// <c>ShrinkLabelDiagnosticsTests.Should_Count_Two_Fields_That_Share_A_Label</c>, which is the
    /// test asserting that two fields labelled alike are still counted as two. Identical default
    /// labels would buy nothing and would leave a caller unable to tell the root field from the item
    /// field in a rendered form.
    /// </remarks>
    internal static IFormConfiguration<NamedOrderModel> RootFieldAndItemForm(
        Action<FieldBuilder<NamedOrderModel, string>>? configureRoot = null,
        Action<FieldBuilder<NamedOrderItem, string>>? configureItem = null) =>
        FormBuilder<NamedOrderModel>
            .Create()
            .AddField(x => x.Name, field =>
            {
                field.WithLabel("Name");
                configureRoot?.Invoke(field);
            })
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.Name, field =>
                    {
                        field.WithLabel("Item name");
                        configureItem?.Invoke(field);
                    })))
            .Build();
}

/// <summary>Root model for the text path.</summary>
internal sealed class OrderModel
{
    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>Item for the text path — one <c>string</c>, nothing else.</summary>
internal sealed class OrderItem
{
    public string ProductName { get; set; } = string.Empty;
}

/// <summary>Root model for the numeric and boolean paths.</summary>
internal sealed class BasketModel
{
    public List<BasketLine> Lines { get; set; } = new();
}

/// <summary>
/// Item for the numeric and boolean field types. It carries both so a single suite can compare a
/// field that binds the shared presentation attributes against one that binds none, without a second
/// model pair.
/// </summary>
internal sealed class BasketLine
{
    public int Quantity { get; set; }

    public bool IsGift { get; set; }
}

/// <summary>Root model for the date path.</summary>
internal sealed class AppointmentModel
{
    public List<AppointmentSlot> Slots { get; set; } = new();
}

/// <summary>Item for the date path — one <c>DateTime</c>, nothing else.</summary>
internal sealed class AppointmentSlot
{
    public DateTime When { get; set; }
}

/// <summary>Root model for the decimal path.</summary>
internal sealed class PricedBasketModel
{
    public List<PricedLine> Lines { get; set; } = new();
}

/// <summary>
/// Item for the decimal field type — one <c>decimal</c>, nothing else. A separate pair rather than a
/// <c>Price</c> added to <see cref="BasketLine"/>: growing a shared model one field per consumer is
/// how a fixture becomes load-bearing and frightening to touch, which is the property #205 set out
/// to protect.
/// </summary>
internal sealed class PricedLine
{
    public decimal Price { get; set; }
}

/// <summary>
/// Root model for the root-field-beside-a-collection shape. Its own <c>Name</c> and its item's
/// <c>Name</c> collide by design — see <see cref="CollectionItemFixture.RootFieldAndItemForm"/>.
/// </summary>
internal sealed class NamedOrderModel
{
    public string Name { get; set; } = string.Empty;

    public List<NamedOrderItem> Items { get; set; } = new();
}

/// <summary>Item for the collision shape — one <c>string</c> named to clash with the root field.</summary>
internal sealed class NamedOrderItem
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Rendering half of the fixture. It is an extension on <see cref="BunitContext"/> rather than a member
/// of <c>MudBlazorTestBase</c> on purpose: the base class is about how to render anything, and pushing
/// collection-specific helpers into it would burden every MudBlazor test that has nothing to do with
/// collections.
/// </summary>
internal static class CollectionItemFixtureRenderExtensions
{
    /// <summary>
    /// Renders <paramref name="model"/> through <c>FormCraftComponent</c> with the given configuration,
    /// plus any extra component parameters <paramref name="configure"/> adds.
    /// This is the shape every collection-item suite had open-coded per test.
    /// </summary>
    /// <remarks>
    /// <paramref name="configure"/> exists because Model and Configuration are not quite all a suite
    /// ever needs: some also pass <c>DefaultShrinkLabel</c> (the form-level cascade) or
    /// <c>OnEditContextCreated</c> (to capture the form's <c>EditContext</c>). Without a way to add
    /// those, every such test re-opened <c>Render&lt;FormCraftComponent&lt;T&gt;&gt;</c> by hand and
    /// re-implemented the Model/Configuration wiring this helper owns — so the "shape every suite
    /// shares" was in fact shared by only some of them. It runs after the two required parameters, so
    /// existing call sites are unaffected.
    /// </remarks>
    internal static IRenderedComponent<FormCraftComponent<TModel>> RenderItemForm<TModel>(
        this BunitContext context,
        TModel model,
        IFormConfiguration<TModel> configuration,
        Action<ComponentParameterCollectionBuilder<FormCraftComponent<TModel>>>? configure = null)
        where TModel : new() =>
        context.Render<FormCraftComponent<TModel>>(parameters =>
        {
            parameters
                .Add(p => p.Model, model)
                .Add(p => p.Configuration, configuration);
            configure?.Invoke(parameters);
        });
}
