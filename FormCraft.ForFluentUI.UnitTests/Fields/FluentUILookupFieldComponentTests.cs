using FormCraft.ForFluentUI.Extensions;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// A field configured with <c>.AsLookup(...)</c> renders a read-only display plus a browsable grid
/// of candidate rows, and writes the chosen row's value back to the model (#278).
/// </summary>
/// <remarks>
/// The picker is an <b>inline panel</b>, not a modal, which is a deliberate departure from the
/// MudBlazor adapter - see <c>FluentUILookupFieldComponent</c> for why v5's dialog service was
/// rejected here.
/// </remarks>
public class FluentUILookupFieldComponentTests : FluentUITestBase
{
    private static readonly City[] AllCities =
    [
        new(1, "Paris", "France"),
        new(2, "Porto", "Portugal"),
        new(3, "Berlin", "Germany"),
    ];

    [Fact]
    public void A_Lookup_Field_Should_Render_A_ReadOnly_Display_And_A_Browse_Control()
    {
        // Act
        var component = RenderLookup();

        // Assert
        component.FindComponent<FluentTextInput>().Instance.ReadOnly.ShouldBe(true);
        component.Find("[data-testid=formcraft-lookup-open]").ShouldNotBeNull();
    }

    [Fact]
    public void The_Candidate_Grid_Should_Be_Closed_Until_Asked_For()
    {
        // Act
        var component = RenderLookup();

        // Assert
        component.FindAll("[data-testid=formcraft-lookup-row]").ShouldBeEmpty();
    }

    [Fact]
    public async Task Opening_The_Picker_Should_Show_The_Providers_Rows()
    {
        // Arrange
        var component = RenderLookup();

        // Act
        await component.Find("[data-testid=formcraft-lookup-open]").ClickAsync(new());

        // Assert
        component.FindAll("[data-testid=formcraft-lookup-row]").Count.ShouldBe(3);
    }

    [Fact]
    public async Task Choosing_A_Row_Should_Write_Its_Value_And_Display_Text()
    {
        // Arrange
        var model = new TripModel();
        var component = RenderLookup(model);
        await component.Find("[data-testid=formcraft-lookup-open]").ClickAsync(new());

        // Act - pick Porto
        await component.FindAll("[data-testid=formcraft-lookup-row]")[1].ClickAsync(new());

        // Assert
        model.CityId.ShouldBe(2);
        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("Porto");
    }

    [Fact]
    public async Task Choosing_A_Row_Should_Run_The_OnItemSelected_Mapping()
    {
        // Arrange - the multi-field mapping hook
        var model = new TripModel();
        var component = RenderLookup(model, mapCountry: true);
        await component.Find("[data-testid=formcraft-lookup-open]").ClickAsync(new());

        // Act
        await component.FindAll("[data-testid=formcraft-lookup-row]")[0].ClickAsync(new());

        // Assert
        model.Country.ShouldBe("France");
    }

    [Fact]
    public async Task Choosing_A_Row_Should_Close_The_Picker()
    {
        // Arrange
        var component = RenderLookup();
        await component.Find("[data-testid=formcraft-lookup-open]").ClickAsync(new());

        // Act
        await component.FindAll("[data-testid=formcraft-lookup-row]")[0].ClickAsync(new());

        // Assert
        component.FindAll("[data-testid=formcraft-lookup-row]").ShouldBeEmpty();
    }

    [Fact]
    public async Task A_Row_Should_Be_Selectable_From_The_Keyboard()
    {
        // Arrange - the inline grid is the ONLY selection path, so a row that is focusable but not
        // operable by keyboard leaves the field unusable without a pointer.
        var model = new TripModel();
        var component = RenderLookup(model);
        await component.Find("[data-testid=formcraft-lookup-open]").ClickAsync(new());

        // Act
        await component.FindAll("[data-testid=formcraft-lookup-row]")[1]
            .KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        // Assert
        model.CityId.ShouldBe(2);
    }

    [Fact]
    public async Task A_Row_Should_Also_Accept_Space()
    {
        // Arrange
        var model = new TripModel();
        var component = RenderLookup(model);
        await component.Find("[data-testid=formcraft-lookup-open]").ClickAsync(new());

        // Act
        await component.FindAll("[data-testid=formcraft-lookup-row]")[0]
            .KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = " " });

        // Assert
        model.CityId.ShouldBe(1);
    }

    [Fact]
    public async Task An_Unrelated_Key_Should_Not_Select_A_Row()
    {
        // Arrange - arrow keys move focus; they must not commit a value
        var model = new TripModel();
        var component = RenderLookup(model);
        await component.Find("[data-testid=formcraft-lookup-open]").ClickAsync(new());

        // Act
        await component.FindAll("[data-testid=formcraft-lookup-row]")[1]
            .KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowDown" });

        // Assert
        model.CityId.ShouldBe(0);
    }

    [Fact]
    public void A_Required_Lookup_Should_Announce_Itself()
    {
        // Act
        var component = RenderLookup(required: true);

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void An_Optional_Lookup_Should_Not_Announce_Itself_As_Required()
    {
        // Act
        var component = RenderLookup();

        // Assert
        component.FindAll("[aria-required='true']").ShouldBeEmpty();
    }

    private IRenderedComponent<FormCraftComponent<TripModel>> RenderLookup(
        TripModel? model = null,
        bool mapCountry = false,
        bool required = false)
    {
        var config = FormBuilder<TripModel>.Create()
            .AddField(x => x.CityId, f =>
            {
                f.WithLabel("City");
                if (required)
                {
                    f.Required("City is required");
                }

                // Called as a static method rather than as an extension on purpose. This project
                // references BOTH adapters (to prove they refuse to co-register), and the
                // MudBlazor package publishes an .AsLookup(...) of the same name into namespace
                // FormCraft - so the extension-method form here would be CS0121-ambiguous. A real
                // Fluent-only application has only one of them in scope and writes
                // `.AsLookup(...)` normally after `using FormCraft.ForFluentUI.Extensions;`.
                FluentUIFieldBuilderExtensions.AsLookup<TripModel, int, City>(
                    f,
                    dataProvider: _ => Task.FromResult(new LookupResult<City>
                    {
                        Items = AllCities,
                        TotalCount = AllCities.Length,
                    }),
                    valueSelector: c => c.Id,
                    displaySelector: c => c.Name,
                    configureColumns: cols =>
                    {
                        cols.Add(new LookupColumn<City> { Title = "Name", ValueSelector = c => c.Name });
                        cols.Add(new LookupColumn<City> { Title = "Country", ValueSelector = c => c.Country });
                    },
                    onItemSelected: mapCountry ? (m, c) => m.Country = c.Country : null);
            })
            .Build();

        return Render<FormCraftComponent<TripModel>>(p => p
            .Add(c => c.Model, model ?? new TripModel())
            .Add(c => c.Configuration, config));
    }

    /// <summary>Model with a looked-up foreign key and a mapped companion field.</summary>
    public class TripModel
    {
        /// <summary>The looked-up value.</summary>
        public int CityId { get; set; }

        /// <summary>Populated by the lookup's onItemSelected mapping.</summary>
        public string Country { get; set; } = string.Empty;
    }

    /// <summary>A row in the lookup grid.</summary>
    /// <param name="Id">The value written to the model.</param>
    /// <param name="Name">The display text.</param>
    /// <param name="Country">A second column, and the mapping target.</param>
    public record City(int Id, string Name, string Country);
}
