namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Regression tests for the runtime DependsOn trigger path: callbacks must fire
/// when the WATCHED field changes, not when the configured field itself changes.
/// </summary>
public class FieldDependencyIntegrationTests : MudBlazorTestBase
{
    [Fact]
    public void DependsOn_Callback_Should_Fire_When_Watched_Field_Changes()
    {
        // Arrange - City depends on Country: changing Country clears City
        var model = new AddressModel { Country = "Belgium", City = "Brussels" };
        var config = FormBuilder<AddressModel>
            .Create()
            .AddField(x => x.Country, field => field.WithLabel("Country"))
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .DependsOn(x => x.Country, (m, _) => m.City = string.Empty))
            .Build();

        var component = Render<FormCraftComponent<AddressModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act - change the watched field (Country is the first rendered input)
        var countryInput = component.FindAll("input")[0];
        countryInput.Input("France");

        // Assert - the dependency callback must have cleared City
        model.Country.ShouldBe("France");
        model.City.ShouldBe(string.Empty);
    }

    [Fact]
    public void DependsOn_Callback_Should_Not_Fire_When_Configured_Field_Itself_Changes()
    {
        // Arrange - the callback watches Country; editing City itself must not trigger it
        var callbackCount = 0;
        var model = new AddressModel { Country = "Belgium", City = "Brussels" };
        var config = FormBuilder<AddressModel>
            .Create()
            .AddField(x => x.Country, field => field.WithLabel("Country"))
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .DependsOn(x => x.Country, (_, _) => callbackCount++))
            .Build();

        var component = Render<FormCraftComponent<AddressModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act - edit the configured (dependent) field itself
        var cityInput = component.FindAll("input")[1];
        cityInput.Input("Antwerp");

        // Assert - the user's input must survive and the callback must not fire
        model.City.ShouldBe("Antwerp");
        callbackCount.ShouldBe(0);
    }

    [Fact]
    public void DependsOn_Should_Support_Multiple_Fields_Watching_Same_Field()
    {
        // Arrange - both City and PostalCode react to Country changes
        var model = new AddressModel { Country = "Belgium", City = "Brussels", PostalCode = "1000" };
        var config = FormBuilder<AddressModel>
            .Create()
            .AddField(x => x.Country, field => field.WithLabel("Country"))
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .DependsOn(x => x.Country, (m, _) => m.City = string.Empty))
            .AddField(x => x.PostalCode, field => field
                .WithLabel("Postal Code")
                .DependsOn(x => x.Country, (m, _) => m.PostalCode = string.Empty))
            .Build();

        var component = Render<FormCraftComponent<AddressModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act
        var countryInput = component.FindAll("input")[0];
        countryInput.Input("France");

        // Assert
        model.City.ShouldBe(string.Empty);
        model.PostalCode.ShouldBe(string.Empty);
    }

    private class AddressModel
    {
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
    }
}
