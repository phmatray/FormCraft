namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// A field configured with <c>.AsLov(...)</c> renders a read-only display plus a browsable grid of
/// candidate rows, honouring the LOV configuration's columns, selection mode and field mappings
/// (#278).
/// </summary>
public class FluentUILovFieldComponentTests : FluentUITestBase
{
    private static readonly Product[] AllProducts =
    [
        new(10, "Widget", 4.5m),
        new(20, "Gadget", 9.0m),
        new(30, "Doohickey", 1.25m),
    ];

    [Fact]
    public void A_Lov_Field_Should_Render_A_ReadOnly_Display_And_A_Browse_Control()
    {
        // Act
        var component = RenderLov();

        // Assert
        component.FindComponent<FluentTextInput>().Instance.ReadOnly.ShouldBe(true);
        component.Find("[data-testid=formcraft-lov-open]").ShouldNotBeNull();
    }

    [Fact]
    public async Task Opening_The_Picker_Should_Show_The_Configured_Rows()
    {
        // Arrange
        var component = RenderLov();

        // Act
        await component.Find("[data-testid=formcraft-lov-open]").ClickAsync(new());

        // Assert
        component.FindAll("[data-testid=formcraft-lov-row]").Count.ShouldBe(3);
    }

    [Fact]
    public async Task Choosing_A_Row_Should_Write_Its_Value_And_Display_Text()
    {
        // Arrange
        var model = new OrderModel();
        var component = RenderLov(model);
        await component.Find("[data-testid=formcraft-lov-open]").ClickAsync(new());

        // Act - pick Gadget
        await component.FindAll("[data-testid=formcraft-lov-row]")[1].ClickAsync(new());

        // Assert
        model.ProductId.ShouldBe(20);
        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("Gadget");
    }

    [Fact]
    public async Task Searching_Should_Narrow_The_Rows()
    {
        // Arrange
        var component = RenderLov();
        await component.Find("[data-testid=formcraft-lov-open]").ClickAsync(new());

        // Act - `change`, not `input`: Fluent's text input exposes onchange/ontextimmediate and no
        // oninput handler at all, so InputAsync would throw rather than search.
        var search = component.Find("[data-testid=formcraft-lov-search]");
        await search.ChangeAsync(new() { Value = "Gadget" });

        // Assert
        component.FindAll("[data-testid=formcraft-lov-row]").Count.ShouldBe(1);
    }

    [Fact]
    public void A_Required_Lov_Should_Announce_Itself()
    {
        // Act
        var component = RenderLov(required: true);

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void An_Optional_Lov_Should_Not_Announce_Itself_As_Required()
    {
        // Act
        var component = RenderLov();

        // Assert
        component.FindAll("[aria-required='true']").ShouldBeEmpty();
    }

    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderLov(
        OrderModel? model = null,
        bool required = false)
    {
        var config = FormBuilder<OrderModel>.Create()
            .AddField(x => x.ProductId, f =>
            {
                f.WithLabel("Product");
                if (required)
                {
                    f.Required("Product is required");
                }

                f.AsLov<OrderModel, int, Product>(lov => lov
                    .WithDataSource((query, _) =>
                    {
                        var items = AllProducts.Where(p =>
                            string.IsNullOrEmpty(query.SearchText) ||
                            p.Name.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        return Task.FromResult(new LovDataResult<Product>
                        {
                            Items = items,
                            TotalCount = items.Count,
                        });
                    })
                    .WithKey(p => p.Id)
                    .WithDisplay(p => p.Name)
                    .AddColumn(p => p.Name, "Name")
                    .AddColumn(p => p.Price, "Price"));
            })
            .Build();

        return Render<FormCraftComponent<OrderModel>>(p => p
            .Add(c => c.Model, model ?? new OrderModel())
            .Add(c => c.Configuration, config));
    }

    [Fact]
    public async Task A_Multi_Select_Lov_Should_Write_Every_Selected_Key()
    {
        // Arrange - AsMultiSelectLov binds IEnumerable<int> while its key selector yields int (#480):
        // before the renderer resolved TValue from the bound type, only the first pick was written.
        var model = new BasketModel();
        var component = RenderMultiSelectLov(model);
        await component.Find("[data-testid=formcraft-lov-open]").ClickAsync(new());

        // Act - pick Widget, then Doohickey
        await component.FindAll("[data-testid=formcraft-lov-row]")[0].ClickAsync(new());
        await component.FindAll("[data-testid=formcraft-lov-row]")[2].ClickAsync(new());

        // Assert
        model.ProductIds.ShouldBe([10, 30]);
    }

    [Fact]
    public async Task A_Multi_Select_Lov_Should_Show_No_Type_Name_And_Write_An_Empty_List_Once_Emptied()
    {
        // Arrange
        var model = new BasketModel();
        var component = RenderMultiSelectLov(model);

        // Assert - the bound collection's ToString() ("System.Int32[]") is not display text
        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBeNullOrEmpty();

        // Act - pick one row, then remove its chip
        await component.Find("[data-testid=formcraft-lov-open]").ClickAsync(new());
        await component.FindAll("[data-testid=formcraft-lov-row]")[0].ClickAsync(new());
        await component.Find("[data-testid=formcraft-lov-chip-remove]").ClickAsync(new());

        // Assert - an empty list, not null, as the MudBlazor component writes
        model.ProductIds.ShouldNotBeNull();
        model.ProductIds.ShouldBeEmpty();
    }

    private IRenderedComponent<FormCraftComponent<BasketModel>> RenderMultiSelectLov(BasketModel model)
    {
        var config = FormBuilder<BasketModel>.Create()
            .AddField(x => x.ProductIds, f => f
                .WithLabel("Products")
                .AsMultiSelectLov<BasketModel, int, Product>(lov => lov
                    .WithKey(p => p.Id)
                    .WithDisplay(p => p.Name)
                    .WithDataSource(() => AllProducts)
                    .AddColumn(p => p.Name, "Name")))
            .Build();
        return Render<FormCraftComponent<BasketModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));
    }

    /// <summary>Model with a multi-select LOV.</summary>
    public class BasketModel
    {
        /// <summary>The LOV-selected keys.</summary>
        public IEnumerable<int> ProductIds { get; set; } = [];
    }

    /// <summary>Model with a LOV-selected foreign key.</summary>
    public class OrderModel
    {
        /// <summary>The LOV-selected value.</summary>
        public int ProductId { get; set; }
    }

    /// <summary>A row in the LOV grid.</summary>
    /// <param name="Id">The value written to the model.</param>
    /// <param name="Name">The display text.</param>
    /// <param name="Price">A second column.</param>
    public record Product(int Id, string Name, decimal Price);
}
