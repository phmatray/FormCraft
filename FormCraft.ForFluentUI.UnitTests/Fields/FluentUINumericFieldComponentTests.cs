namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>Model covering both a non-nullable and a nullable numeric property.</summary>
public class NumericTestModel
{
    /// <summary>A non-nullable numeric field.</summary>
    public int Quantity { get; set; }

    /// <summary>A nullable numeric field.</summary>
    public int? OptionalCount { get; set; }

    /// <summary>A nullable decimal, for the invariant-formatting check.</summary>
    public decimal? Price { get; set; }
}

/// <summary>
/// Covers numeric rendering, and specifically that a nullable field keeps <c>null</c> rather than
/// coercing it to zero (#150).
/// </summary>
public class FluentUINumericFieldComponentTests : FluentUITestBase
{
    private IRenderedComponent<FormCraftComponent<NumericTestModel>> Render(
        NumericTestModel model, IFormConfiguration<NumericTestModel> config) =>
        Render<FormCraftComponent<NumericTestModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));

    [Fact]
    public void Int_Field_Should_Render_A_Number_Input()
    {
        // Arrange
        var config = FormBuilder<NumericTestModel>.Create()
            .AddField(x => x.Quantity, f => f.WithLabel("Quantity"))
            .Build();

        // Act
        var component = Render(new NumericTestModel { Quantity = 7 }, config);

        // Assert
        component.FindComponent<FluentNumberInput<int>>().Instance.Value.ShouldBe(7);
    }

    [Fact]
    public void Nullable_Numeric_Field_Should_Keep_Null_Rather_Than_Coercing_To_Zero()
    {
        // Arrange - the #150 guarantee: a null int? is null, not 0
        var config = FormBuilder<NumericTestModel>.Create()
            .AddField(x => x.OptionalCount, f => f.WithLabel("Count"))
            .Build();

        // Act
        var component = Render(new NumericTestModel { OptionalCount = null }, config);

        // Assert - the nullable-aware component was selected, and it is holding null
        component.FindComponent<FluentNumberInput<int?>>().Instance.Value.ShouldBeNull();
    }

    [Fact]
    public void Nullable_Numeric_Field_Should_Load_A_Present_Value()
    {
        // Arrange
        var config = FormBuilder<NumericTestModel>.Create()
            .AddField(x => x.OptionalCount, f => f.WithLabel("Count"))
            .Build();

        // Act
        var component = Render(new NumericTestModel { OptionalCount = 42 }, config);

        // Assert
        component.FindComponent<FluentNumberInput<int?>>().Instance.Value.ShouldBe(42);
    }

    [Fact]
    public async Task Editing_A_Numeric_Field_Should_Write_Back_To_The_Model()
    {
        // Arrange
        var model = new NumericTestModel();
        var config = FormBuilder<NumericTestModel>.Create()
            .AddField(x => x.Quantity, f => f.WithLabel("Quantity"))
            .Build();
        var component = Render(model, config);
        var input = component.FindComponent<FluentNumberInput<int>>();

        // Act
        await component.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync(13));

        // Assert
        model.Quantity.ShouldBe(13);
    }

    [Fact]
    public void Required_Numeric_Field_Should_Announce_Itself()
    {
        // Arrange
        var config = FormBuilder<NumericTestModel>.Create()
            .AddField(x => x.Quantity, f => f.WithLabel("Quantity").Required("Required"))
            .Build();

        // Act
        var component = Render(new NumericTestModel(), config);

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void Configured_Bounds_Should_Reach_The_Input()
    {
        // Arrange
        var config = FormBuilder<NumericTestModel>.Create()
            .AddField(x => x.Price, f => f
                .WithLabel("Price")
                .WithAttribute("Min", (decimal?)0m)
                .WithAttribute("Max", (decimal?)100m)
                .WithAttribute("Step", (decimal?)0.01m))
            .Build();

        // Act
        var component = Render(new NumericTestModel(), config);

        // Assert - Fluent types these as TValue, so they arrive as decimals rather than strings
        var input = component.FindComponent<FluentNumberInput<decimal?>>().Instance;
        input.Min.ShouldBe(0m);
        input.Max.ShouldBe(100m);
        input.Step.ShouldBe(0.01m);
    }

    [Fact]
    public void Unconfigured_Bounds_Should_Be_Left_Alone()
    {
        // Arrange - a field that asked for no bounds must not have any invented for it
        var config = FormBuilder<NumericTestModel>.Create()
            .AddField(x => x.Quantity, f => f.WithLabel("Quantity"))
            .Build();

        // Act
        var component = Render(new NumericTestModel(), config);

        // Assert - Fluent's own defaults for the type survive untouched. They are the full range of
        // int rather than 0, which is the point: the splat adds nothing when nothing was configured.
        var input = component.FindComponent<FluentNumberInput<int>>().Instance;
        input.Min.ShouldBe(int.MinValue);
        input.Max.ShouldBe(int.MaxValue);
    }
}
