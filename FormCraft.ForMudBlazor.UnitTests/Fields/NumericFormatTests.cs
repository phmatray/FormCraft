namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that <c>Format</c> and <c>ShowSpinButtons</c> reach MudBlazor on both render paths (#208).
/// </summary>
/// <remarks>
/// Both were resolved in the numeric components' <c>OnInitialized</c> and then never bound in the
/// <c>.razor</c> — read and dropped, the same defect #191 fixed for adornments three lines above
/// them. <c>ShowSpinButtons</c> is even part of the public
/// <c>INumericFieldComponent&lt;TModel, TValue&gt;</c> contract, so the library advertised a setting
/// it discarded.
/// <para>
/// The spin-button assertions look at the rendered markup rather than the component parameter. A
/// parameter that is set but never forwarded is exactly the failure being fixed, so asserting the
/// parameter alone would have passed both before and after.
/// </para>
/// </remarks>
public class NumericFormatTests : MudBlazorTestBase
{
    [Fact]
    public void NumericField_Should_Honour_ShowSpinButtons_False()
    {
        // Arrange & Act
        var component = RenderNumeric(f => f.WithAttribute("ShowSpinButtons", false));

        // Assert - MudBlazor draws the spin buttons as adornment buttons inside the input; with the
        // setting honoured there are none.
        component.FindComponent<MudNumericField<int>>().Instance.HideSpinButtons.ShouldBeTrue();
        component.FindAll(".mud-input-numeric-spin").ShouldBeEmpty();
    }

    [Fact]
    public void NumericField_Should_Show_Spin_Buttons_By_Default()
    {
        // Arrange & Act - the default must stay MudBlazor's own `true`, so an unconfigured field is
        // untouched by this change. That is what keeps the behaviour change scoped to forms that
        // actually pass the attribute.
        var component = RenderNumeric(_ => { });

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.HideSpinButtons.ShouldBeFalse();
        component.FindAll(".mud-input-numeric-spin").ShouldNotBeEmpty();
    }

    [Fact]
    public void NumericField_Should_Honour_Format()
    {
        // Arrange & Act
        var component = RenderNumeric(f => f.WithAttribute("Format", "N2"));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Format.ShouldBe("N2");
    }

    [Fact]
    public void NullableNumericField_Should_Honour_ShowSpinButtons_False()
    {
        // Arrange & Act - the nullable component is a separate file with its own copy of the
        // read-and-drop, so it needs its own coverage rather than inheriting confidence.
        var component = RenderNullableNumeric(f => f.WithAttribute("ShowSpinButtons", false));

        // Assert
        component.FindComponent<MudNumericField<int?>>().Instance.HideSpinButtons.ShouldBeTrue();
        component.FindAll(".mud-input-numeric-spin").ShouldBeEmpty();
    }

    [Fact]
    public void NullableNumericField_Should_Honour_Format()
    {
        // Arrange & Act
        var component = RenderNullableNumeric(f => f.WithAttribute("Format", "N2"));

        // Assert
        component.FindComponent<MudNumericField<int?>>().Instance.Format.ShouldBe("N2");
    }

    [Fact]
    public void NumericItemField_Should_Honour_ShowSpinButtons_False()
    {
        // Arrange & Act - the collection path builds its tree imperatively and forwards its own
        // attribute set, so fixing only the component path would open a fresh divergence — the exact
        // thing RenderPipelineParityTests exists to close.
        var component = RenderNumericItem(f => f.WithAttribute("ShowSpinButtons", false));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.HideSpinButtons.ShouldBeTrue();
    }

    [Fact]
    public void NumericItemField_Should_Honour_Format()
    {
        // Arrange & Act
        var component = RenderNumericItem(f => f.WithAttribute("Format", "N2"));

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.Format.ShouldBe("N2");
    }

    [Fact]
    public void NumericItemField_Should_Show_Spin_Buttons_By_Default()
    {
        // Arrange & Act - parity on the default too, not just on the configured value.
        var component = RenderNumericItem(_ => { });

        // Assert
        component.FindComponent<MudNumericField<int>>().Instance.HideSpinButtons.ShouldBeFalse();
    }

    private IRenderedComponent<FormCraftComponent<NumericModel>> RenderNumeric(
        Action<FieldBuilder<NumericModel, int>> configure)
    {
        var config = FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Quantity, field =>
            {
                field.WithLabel("Quantity");
                configure(field);
            })
            .Build();

        return Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel())
            .Add(p => p.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<NullableNumericModel>> RenderNullableNumeric(
        Action<FieldBuilder<NullableNumericModel, int?>> configure)
    {
        var config = FormBuilder<NullableNumericModel>
            .Create()
            .AddField(x => x.Quantity, field =>
            {
                field.WithLabel("Quantity");
                configure(field);
            })
            .Build();

        return Render<FormCraftComponent<NullableNumericModel>>(parameters => parameters
            .Add(p => p.Model, new NullableNumericModel())
            .Add(p => p.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<BasketModel>> RenderNumericItem(
        Action<FieldBuilder<BasketLine, int>> configure)
    {
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field =>
                    {
                        field.WithLabel("Quantity");
                        configure(field);
                    })))
            .Build();

        return Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));
    }

    private class NumericModel
    {
        public int Quantity { get; set; }
    }

    private class NullableNumericModel
    {
        public int? Quantity { get; set; }
    }

    private class BasketModel
    {
        public List<BasketLine> Lines { get; set; } = new();
    }

    private class BasketLine
    {
        public int Quantity { get; set; }
    }
}
