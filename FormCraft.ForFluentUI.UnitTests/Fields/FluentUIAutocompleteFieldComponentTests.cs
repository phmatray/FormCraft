namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// A field configured with <c>.AsAutocomplete(...)</c> renders a Fluent autocomplete and searches
/// through the configured function (#278).
/// </summary>
public class FluentUIAutocompleteFieldComponentTests : FluentUITestBase
{
    private static readonly string[] Cities = ["Paris", "Prague", "Porto", "Berlin"];

    [Fact]
    public void Autocomplete_Field_Should_Render_A_Fluent_Autocomplete()
    {
        // Act
        var component = RenderCityField();

        // Assert - and NOT the plain text field a string would otherwise fall through to, which is
        // what happens if the autocomplete renderer is registered below the type-based block.
        //
        // Asserted on FormCraft's own component, not on the absence of FluentTextInput: Fluent's
        // autocomplete renders one internally, so "no FluentTextInput" would fail even when the
        // routing is correct.
        component.FindComponents<FluentAutocomplete<SelectOption<string>, string>>().ShouldNotBeEmpty();
        component.FindComponents<FluentUITextFieldComponent<CityModel>>().ShouldBeEmpty();
    }

    [Fact]
    public void Autocomplete_Field_Should_Render_Its_Label_And_Placeholder()
    {
        // Act
        var component = RenderCityField();

        // Assert
        var autocomplete = component.FindComponent<FluentAutocomplete<SelectOption<string>, string>>().Instance;
        autocomplete.Label.ShouldBe("City");
        autocomplete.Placeholder.ShouldBe("Start typing...");
    }

    [Fact]
    public async Task Searching_Should_Return_The_Matches_The_Configured_Function_Yields()
    {
        // Arrange
        var component = RenderCityField();
        var autocomplete = component.FindComponent<FluentAutocomplete<SelectOption<string>, string>>().Instance;
        var args = new OptionsSearchEventArgs<SelectOption<string>> { Text = "P" };

        // Act
        await component.InvokeAsync(() => autocomplete.OnOptionsSearch.InvokeAsync(args));

        // Assert - the three P cities, not the whole list
        args.Items.ShouldNotBeNull();
        args.Items!.Select(i => i.Label).ShouldBe(["Paris", "Prague", "Porto"]);
    }

    [Fact]
    public async Task Selecting_An_Option_Should_Write_Its_Value_To_The_Model()
    {
        // Arrange
        var model = new CityModel();
        var component = RenderCityField(model);
        var autocomplete = component.FindComponent<FluentAutocomplete<SelectOption<string>, string>>();

        // Act
        await component.InvokeAsync(() =>
            autocomplete.Instance.SelectedItemChanged.InvokeAsync(new SelectOption<string>("Porto", "Porto")));

        // Assert
        model.City.ShouldBe("Porto");
    }

    [Fact]
    public void A_Required_Autocomplete_Should_Announce_Itself()
    {
        // Arrange & Act - every new field type carries aria-required explicitly (#199, #260)
        var component = RenderCityField(configure: f => f
            .WithLabel("City")
            .WithPlaceholder("Start typing...")
            .Required("City is required")
            .AsAutocomplete(SearchCitiesAsync));

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void An_Optional_Autocomplete_Should_Not_Announce_Itself_As_Required()
    {
        // Act
        var component = RenderCityField();

        // Assert
        component.FindAll("[aria-required='true']").ShouldBeEmpty();
    }

    private static Task<IEnumerable<SelectOption<string>>> SearchCitiesAsync(string text, CancellationToken token)
        => Task.FromResult(Cities
            .Where(c => c.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            .Select(c => new SelectOption<string>(c, c)));

    private IRenderedComponent<FormCraftComponent<CityModel>> RenderCityField(
        CityModel? model = null,
        Action<FieldBuilder<CityModel, string>>? configure = null)
    {
        configure ??= f => f
            .WithLabel("City")
            .WithPlaceholder("Start typing...")
            .AsAutocomplete(SearchCitiesAsync);

        var config = FormBuilder<CityModel>.Create()
            .AddField(x => x.City, configure)
            .Build();

        return Render<FormCraftComponent<CityModel>>(p => p
            .Add(c => c.Model, model ?? new CityModel())
            .Add(c => c.Configuration, config));
    }

    /// <summary>Model with a single autocompleted string field.</summary>
    public class CityModel
    {
        /// <summary>The autocompleted field.</summary>
        public string City { get; set; } = string.Empty;
    }
}
