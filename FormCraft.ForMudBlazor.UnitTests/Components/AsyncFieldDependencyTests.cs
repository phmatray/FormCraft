namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Tests for the first-class async DependsOn overload (#93): async callbacks must be awaited
/// by the dependency dispatch and the UI must re-render after they settle, without the
/// callback needing manual StateHasChanged plumbing.
/// </summary>
public class AsyncFieldDependencyTests : MudBlazorTestBase
{
    [Fact]
    public void Async_DependsOn_Callback_Should_Update_Model_And_UI_After_Settling()
    {
        // Arrange - City reacts asynchronously to Country changes (simulated API call)
        var model = new AddressModel { Country = "Belgium", City = "Brussels" };
        var config = FormBuilder<AddressModel>
            .Create()
            .AddField(x => x.Country, field => field.WithLabel("Country"))
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .DependsOn(x => x.Country, async (m, country) =>
                {
                    await Task.Delay(50);
                    m.City = $"Capital of {country}";
                }))
            .Build();

        var component = Render<FormCraftComponent<AddressModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act - change the watched field (Country is the first rendered input)
        component.FindAll("input")[0].Input("France");

        // Assert - after the async callback settles, both the model and the UI
        // reflect the cascaded mutation without any manual StateHasChanged call
        component.WaitForAssertion(() =>
        {
            model.City.ShouldBe("Capital of France");
            component.FindAll("input")[1].GetAttribute("value").ShouldBe("Capital of France");
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Async_DependsOn_Callback_Should_Not_Fire_When_Configured_Field_Itself_Changes()
    {
        // Arrange - the async callback watches Country; editing City must not trigger it
        var callbackCount = 0;
        var model = new AddressModel { Country = "Belgium", City = "Brussels" };
        var config = FormBuilder<AddressModel>
            .Create()
            .AddField(x => x.Country, field => field.WithLabel("Country"))
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .DependsOn(x => x.Country, (_, _) =>
                {
                    callbackCount++;
                    return Task.CompletedTask;
                }))
            .Build();

        var component = Render<FormCraftComponent<AddressModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Act - edit the configured (dependent) field itself
        component.FindAll("input")[1].Input("Antwerp");

        // Assert
        model.City.ShouldBe("Antwerp");
        callbackCount.ShouldBe(0);
    }

    private class AddressModel
    {
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
