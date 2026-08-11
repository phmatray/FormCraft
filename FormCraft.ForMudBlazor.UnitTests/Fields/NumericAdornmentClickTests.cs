namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests the numeric adornment click handler (#215) on both render paths.
/// </summary>
/// <remarks>
/// #191 added numeric <c>WithAdornment</c> overloads without an <c>onClick</c>, because at the time
/// the string overload's handler was read by neither render path — mirroring a dead parameter onto
/// new API would have been wrong. #192 made that parameter live on both paths, so the reason expired
/// and the gap remained.
/// <para>
/// The handler is <c>Action&lt;TValue?&gt;</c>, not the string overload's <c>Action&lt;string?&gt;</c>:
/// that shape is right there only because the value happens to be a string.
/// </para>
/// </remarks>
public class NumericAdornmentClickTests : MudBlazorTestBase
{
    [Fact]
    public void NumericField_Adornment_Should_Invoke_The_Handler_With_The_Value()
    {
        // Arrange
        int? received = null;
        var config = FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Quantity, field => field
                .WithLabel("Quantity")
                .WithAdornment(Icons.Material.Filled.Numbers, Adornment.End, onClick: v => received = v))
            .Build();

        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel { Quantity = 7 })
            .Add(p => p.Configuration, config));

        // Act
        component.Find(".mud-input-adornment button").Click();

        // Assert - typed to the field's own value, which is the whole point of the decision.
        received.ShouldBe(7);
    }

    [Fact]
    public void NumericField_Adornment_Without_A_Handler_Should_Not_Render_A_Button()
    {
        // Arrange - the #216 invariant: a decorative icon must not become a focus stop for keyboard
        // and screen-reader users. Binding a method group would make EventCallback.HasDelegate always
        // true and MudBlazor would draw a real <button>, so the callback must be `default`.
        var config = FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Quantity, field => field
                .WithLabel("Quantity")
                .WithAdornment(Icons.Material.Filled.Numbers, Adornment.End))
            .Build();

        // Act
        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel())
            .Add(p => p.Configuration, config));

        // Assert
        component.FindAll(".mud-input-adornment").Count.ShouldBe(1);
        component.FindAll(".mud-input-adornment button").ShouldBeEmpty();
    }

    [Fact]
    public void NullableNumericField_Adornment_Should_Invoke_The_Handler_With_The_Value()
    {
        // Arrange - the nullable component is a separate file and needs its own coverage.
        decimal? received = null;
        var config = FormBuilder<NumericModel>
            .Create()
            .AddField(x => x.Discount, field => field
                .WithLabel("Discount")
                .WithAdornment(Icons.Material.Filled.Percent, Adornment.End, onClick: v => received = v))
            .Build();

        var component = Render<FormCraftComponent<NumericModel>>(parameters => parameters
            .Add(p => p.Model, new NumericModel { Discount = 12.5m })
            .Add(p => p.Configuration, config));

        // Act
        component.Find(".mud-input-adornment button").Click();

        // Assert
        received.ShouldBe(12.5m);
    }

    [Fact]
    public void NumericItemField_Adornment_Should_Invoke_The_Handler_With_The_Row_Value()
    {
        // Arrange - the collection path passed `default` for numeric item fields, so the handler was
        // inert inside .WithItemForm(...). Fixing only the component path would have opened the
        // divergence RenderPipelineParityTests exists to close.
        int? received = null;
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field => field
                        .WithLabel("Quantity")
                        .WithAdornment(Icons.Material.Filled.Numbers, Adornment.End,
                            onClick: v => received = v))))
            .Build();

        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine { Quantity = 3 } } })
            .Add(p => p.Configuration, config));

        // Act
        component.Find(".mud-input-adornment button").Click();

        // Assert - the ROW's value, read at click time rather than captured at render time.
        received.ShouldBe(3);
    }

    [Fact]
    public void NumericItemField_Adornment_Without_A_Handler_Should_Not_Render_A_Button()
    {
        // Arrange - the #216 invariant on the item path too.
        var config = FormBuilder<BasketModel>
            .Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .WithItemForm(item => item
                    .AddField(x => x.Quantity, field => field
                        .WithLabel("Quantity")
                        .WithAdornment(Icons.Material.Filled.Numbers, Adornment.End))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<BasketModel>>(parameters => parameters
            .Add(p => p.Model, new BasketModel { Lines = { new BasketLine() } })
            .Add(p => p.Configuration, config));

        // Assert
        component.FindAll(".mud-input-adornment button").ShouldBeEmpty();
    }

    private class NumericModel
    {
        public int Quantity { get; set; }
        public decimal? Discount { get; set; }
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
