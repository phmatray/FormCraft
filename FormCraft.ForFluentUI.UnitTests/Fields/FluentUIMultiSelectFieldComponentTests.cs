namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// A field configured with <c>.AsMultiSelect(...)</c> renders a multiple-selection Fluent select
/// whose selected set round-trips to the model (#278).
/// </summary>
public class FluentUIMultiSelectFieldComponentTests : FluentUITestBase
{
    [Fact]
    public void MultiSelect_Field_Should_Render_A_Fluent_Select_In_Multiple_Mode()
    {
        // Act
        var component = RenderCategoriesField();

        // Assert
        var select = component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance;
        select.Multiple.ShouldBeTrue();
        select.Label.ShouldBe("Categories");
    }

    [Fact]
    public void MultiSelect_Field_Should_Offer_Every_Configured_Option()
    {
        // Act
        var component = RenderCategoriesField();

        // Assert
        var select = component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance;
        select.Items.ShouldNotBeNull();
        select.Items!.Select(o => o.Label).ShouldBe(["Technology", "Healthcare", "Finance"]);
    }

    [Fact]
    public void MultiSelect_Field_Should_Preselect_The_Models_Current_Values()
    {
        // Arrange
        var model = new CategoryModel { Categories = ["tech", "finance"] };

        // Act
        var component = RenderCategoriesField(model);

        // Assert
        var select = component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance;
        select.SelectedItems.ShouldNotBeNull();
        select.SelectedItems!.Select(o => o.Value).ShouldBe(["tech", "finance"]);
    }

    [Fact]
    public async Task Changing_The_Selection_Should_Round_Trip_To_The_Model()
    {
        // Arrange
        var model = new CategoryModel();
        var component = RenderCategoriesField(model);
        var select = component.FindComponent<FluentSelect<SelectOption<string>, string>>();

        // Act
        await component.InvokeAsync(() => select.Instance.SelectedItemsChanged.InvokeAsync(
        [
            new SelectOption<string>("tech", "Technology"),
            new SelectOption<string>("health", "Healthcare"),
        ]));

        // Assert
        model.Categories.ShouldBe(["tech", "health"]);
    }

    [Fact]
    public void A_Required_MultiSelect_Should_Announce_Itself()
    {
        // Act
        var component = RenderCategoriesField(configure: f => f
            .WithLabel("Categories")
            .Required("Pick at least one")
            .AsMultiSelect(("tech", "Technology"), ("health", "Healthcare"), ("finance", "Finance")));

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void An_Optional_MultiSelect_Should_Not_Announce_Itself_As_Required()
    {
        // Act
        var component = RenderCategoriesField();

        // Assert
        component.FindAll("[aria-required='true']").ShouldBeEmpty();
    }

    private IRenderedComponent<FormCraftComponent<CategoryModel>> RenderCategoriesField(
        CategoryModel? model = null,
        Action<FieldBuilder<CategoryModel, IEnumerable<string>>>? configure = null)
    {
        configure ??= f => f
            .WithLabel("Categories")
            .AsMultiSelect(("tech", "Technology"), ("health", "Healthcare"), ("finance", "Finance"));

        var config = FormBuilder<CategoryModel>.Create()
            .AddField(x => x.Categories, configure)
            .Build();

        return Render<FormCraftComponent<CategoryModel>>(p => p
            .Add(c => c.Model, model ?? new CategoryModel())
            .Add(c => c.Configuration, config));
    }

    /// <summary>Model with a multi-select field.</summary>
    public class CategoryModel
    {
        /// <summary>The multi-selected values.</summary>
        public IEnumerable<string> Categories { get; set; } = [];
    }
}
