namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// The canonical models and item-form builders the collection-item attribute suites share (#205).
/// <para>
/// Every such suite exercises the same handful of render paths through
/// <c>CollectionFieldComponent</c>'s imperative RenderTreeBuilder path, and each one used to carry
/// its own copy of the same model pairs and the same two helpers. The copies drifted in incidentals
/// (whether <c>ProductName</c> was seeded, whether <c>BasketLine</c> declared <c>IsGift</c>) and
/// they propagated mistakes as faithfully as they propagated code — a miscounted comment claiming
/// <c>AddCommonFieldAttributes</c> fed "the text and numeric paths" survived a copy and hid the date
/// path from coverage until review caught it.
/// </para>
/// <para>
/// <b>The four paths.</b> The models are chosen so that a suite using this fixture covers all of them
/// by default rather than by remembering to:
/// <list type="bullet">
/// <item><description><see cref="OrderItem"/> — <c>string</c>, the text path (<c>MudTextField</c>).</description></item>
/// <item><description><see cref="BasketLine"/> — <c>int</c>, the numeric path (<c>MudNumericField</c>).</description></item>
/// <item><description><see cref="AppointmentSlot"/> — <c>DateTime</c>, the date path (<c>MudDatePicker</c>).</description></item>
/// <item><description><see cref="BasketLine"/> again — <c>bool</c>, the checkbox path
/// (<c>MudCheckBox</c>), which is the one that <b>bypasses</b> <c>AddCommonFieldAttributes</c> and
/// where attributes are therefore pinned as inert.</description></item>
/// </list>
/// The first three are fed by <c>AddCommonFieldAttributes</c>; the fourth is not. That asymmetry is
/// the whole reason the boolean model lives here rather than being left to each suite.
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

    /// <summary>Creates a basket with a single line, blank unless seeds are given.</summary>
    internal static BasketModel NewBasket(int quantity = 0, bool isGift = false) =>
        new() { Lines = { new BasketLine { Quantity = quantity, IsGift = isGift } } };

    /// <summary>Creates an appointment with a single slot, blank unless a <paramref name="when"/> is given.</summary>
    internal static AppointmentModel NewAppointment(DateTime when = default) =>
        new() { Slots = { new AppointmentSlot { When = when } } };

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
    /// A collection whose item form holds one <c>bool</c> field labelled "Gift" — the checkbox path,
    /// the one that never reaches <c>AddCommonFieldAttributes</c>.
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
/// Item for the numeric and boolean paths. It carries both so a single suite can compare a field fed
/// by <c>AddCommonFieldAttributes</c> against one that bypasses it, without a second model pair.
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

/// <summary>
/// Rendering half of the fixture. It is an extension on <see cref="BunitContext"/> rather than a member
/// of <c>MudBlazorTestBase</c> on purpose: the base class is about how to render anything, and pushing
/// collection-specific helpers into it would burden every MudBlazor test that has nothing to do with
/// collections.
/// </summary>
internal static class CollectionItemFixtureRenderExtensions
{
    /// <summary>
    /// Renders <paramref name="model"/> through <c>FormCraftComponent</c> with the given configuration.
    /// This is the shape every collection-item suite had open-coded per test.
    /// </summary>
    internal static IRenderedComponent<FormCraftComponent<TModel>> RenderItemForm<TModel>(
        this BunitContext context,
        TModel model,
        IFormConfiguration<TModel> configuration)
        where TModel : new() =>
        context.Render<FormCraftComponent<TModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, configuration));
}
