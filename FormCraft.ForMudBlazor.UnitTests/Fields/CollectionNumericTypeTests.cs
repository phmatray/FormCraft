namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that every numeric type the component path renders also renders inside
/// <c>.WithItemForm(...)</c> (#209).
/// </summary>
/// <remarks>
/// <c>RenderItemField</c> dispatched on <c>string</c>/<c>int</c>/<c>decimal</c>/<c>double</c>/
/// <c>bool</c>/<c>DateTime</c> only, while <c>MudBlazorNumericFieldRenderer.CanRender</c> accepts
/// <c>float</c>, <c>long</c>, <c>short</c> and <c>byte</c> as well. An item field of one of those four
/// emitted **no frames at all** — no input, no label, no validation message. The user saw an empty
/// row, and the identical field outside a collection worked.
/// <para>
/// #191 made it worse: its numeric <c>WithAdornment</c> overloads are constrained to
/// <c>INumber&lt;T&gt;</c>, so they compile on exactly these types — an adornment could be configured,
/// without a warning, on a field that rendered nothing.
/// </para>
/// </remarks>
public class CollectionNumericTypeTests : MudBlazorTestBase
{
    [Fact]
    public void LongItemField_Should_Render()
    {
        var component = RenderItemForm<NumericsModel>(item => item
            .AddField(x => x.AsLong, f => f.WithLabel("Long")));

        component.FindComponents<MudNumericField<long>>().ShouldNotBeEmpty();
        component.FindAll("input").ShouldNotBeEmpty();
    }

    [Fact]
    public void FloatItemField_Should_Render()
    {
        var component = RenderItemForm<NumericsModel>(item => item
            .AddField(x => x.AsFloat, f => f.WithLabel("Float")));

        component.FindComponents<MudNumericField<float>>().ShouldNotBeEmpty();
        component.FindAll("input").ShouldNotBeEmpty();
    }

    [Fact]
    public void ShortItemField_Should_Render()
    {
        var component = RenderItemForm<NumericsModel>(item => item
            .AddField(x => x.AsShort, f => f.WithLabel("Short")));

        component.FindComponents<MudNumericField<short>>().ShouldNotBeEmpty();
        component.FindAll("input").ShouldNotBeEmpty();
    }

    [Fact]
    public void ByteItemField_Should_Render()
    {
        var component = RenderItemForm<NumericsModel>(item => item
            .AddField(x => x.AsByte, f => f.WithLabel("Byte")));

        component.FindComponents<MudNumericField<byte>>().ShouldNotBeEmpty();
        component.FindAll("input").ShouldNotBeEmpty();
    }

    [Fact]
    public void Every_Type_The_Component_Path_Accepts_Should_Render_As_An_Item_Field()
    {
        // Arrange - the drift guard, and the reason it is driven off CanRender rather than a
        // hand-written list: a copied list drifts exactly the way the dispatch it mirrors did. If a
        // type is added to the renderer and not to RenderItemField, this goes red.
        var renderer = new MudBlazorNumericFieldRenderer();
        var candidates = new[]
        {
            typeof(int), typeof(decimal), typeof(double),
            typeof(float), typeof(long), typeof(short), typeof(byte)
        };

        var accepted = candidates.Where(t => renderer.CanRender(t, null!)).ToList();
        accepted.Count.ShouldBe(7, "the component path's accepted set changed — update this guard");

        // Act & Assert - one item form per accepted type, each must produce an input.
        var config = FormBuilder<NumericsModel>
            .Create()
            .AddCollectionField(x => x.Rows, collection => collection
                .WithLabel("Rows")
                .WithItemForm(item => item
                    .AddField(x => x.AsInt, f => f.WithLabel("Int"))
                    .AddField(x => x.AsDecimal, f => f.WithLabel("Decimal"))
                    .AddField(x => x.AsDouble, f => f.WithLabel("Double"))
                    .AddField(x => x.AsFloat, f => f.WithLabel("Float"))
                    .AddField(x => x.AsLong, f => f.WithLabel("Long"))
                    .AddField(x => x.AsShort, f => f.WithLabel("Short"))
                    .AddField(x => x.AsByte, f => f.WithLabel("Byte"))))
            .Build();

        var component = Render<FormCraftComponent<NumericsModel>>(parameters => parameters
            .Add(p => p.Model, new NumericsModel { Rows = { new NumericsRow() } })
            .Add(p => p.Configuration, config));

        // One input per accepted type. A missing dispatch arm emits no frames at all, so the count
        // is what catches it — asserting "some input exists" would pass with six of seven rendered.
        component.FindAll("input").Count.ShouldBe(accepted.Count);
    }

    private IRenderedComponent<FormCraftComponent<NumericsModel>> RenderItemForm<T>(
        Action<FormBuilder<NumericsRow>> configureItemForm)
    {
        var config = FormBuilder<NumericsModel>
            .Create()
            .AddCollectionField(x => x.Rows, collection => collection
                .WithLabel("Rows")
                .WithItemForm(configureItemForm))
            .Build();

        return Render<FormCraftComponent<NumericsModel>>(parameters => parameters
            .Add(p => p.Model, new NumericsModel { Rows = { new NumericsRow() } })
            .Add(p => p.Configuration, config));
    }

    private class NumericsModel
    {
        public List<NumericsRow> Rows { get; set; } = new();
    }

    private class NumericsRow
    {
        public int AsInt { get; set; }
        public decimal AsDecimal { get; set; }
        public double AsDouble { get; set; }
        public float AsFloat { get; set; }
        public long AsLong { get; set; }
        public short AsShort { get; set; }
        public byte AsByte { get; set; }
    }
}
