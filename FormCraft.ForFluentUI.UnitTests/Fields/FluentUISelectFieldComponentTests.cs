namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>Model with a string and an int property, both usable as selects.</summary>
public class SelectTestModel
{
    /// <summary>A string field, selected from options.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>An int field, selected from options.</summary>
    public int Rating { get; set; }
}

/// <summary>
/// Covers configuration-driven select dispatch and option binding.
/// </summary>
public class FluentUISelectFieldComponentTests : FluentUITestBase
{
    private IRenderedComponent<FormCraftComponent<SelectTestModel>> Render(
        SelectTestModel model, IFormConfiguration<SelectTestModel> config) =>
        Render<FormCraftComponent<SelectTestModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));

    private static IFormConfiguration<SelectTestModel> CountryConfig() =>
        FormBuilder<SelectTestModel>.Create()
            .AddField(x => x.Country, f => f
                .WithLabel("Country")
                .WithOptions(("BE", "Belgium"), ("FR", "France"), ("NL", "Netherlands")))
            .Build();

    [Fact]
    public void String_Field_With_Options_Should_Render_A_Select_Not_A_Text_Input()
    {
        // Arrange & Act - the select renderer is registered ahead of the text one precisely so
        // this field does not match text first
        var component = Render(new SelectTestModel(), CountryConfig());

        // Assert
        component.FindComponents<FluentSelect<SelectOption<string>, string>>().ShouldNotBeEmpty();
        component.FindComponents<FluentTextInput>().ShouldBeEmpty();
    }

    [Fact]
    public void Select_Should_Carry_Every_Configured_Option()
    {
        // Arrange & Act
        var component = Render(new SelectTestModel(), CountryConfig());

        // Assert
        var select = component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance;
        select.Items!.Select(o => o.Label).ShouldBe(["Belgium", "France", "Netherlands"]);
    }

    [Fact]
    public void Select_Should_Load_The_Existing_Model_Value()
    {
        // Arrange & Act
        var component = Render(new SelectTestModel { Country = "FR" }, CountryConfig());

        // Assert
        component.FindComponent<FluentSelect<SelectOption<string>, string>>().Instance.Value
            .ShouldBe("FR");
    }

    [Fact]
    public async Task Choosing_An_Option_Should_Write_Back_To_The_Model()
    {
        // Arrange
        var model = new SelectTestModel();
        var component = Render(model, CountryConfig());
        var select = component.FindComponent<FluentSelect<SelectOption<string>, string>>();

        // Act
        await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync("NL"));

        // Assert
        model.Country.ShouldBe("NL");
    }

    [Fact]
    public void A_String_Field_Without_Options_Should_Still_Be_A_Text_Input()
    {
        // Arrange - the select renderer is configuration-driven, so it must decline this field
        var config = FormBuilder<SelectTestModel>.Create()
            .AddField(x => x.Country, f => f.WithLabel("Country"))
            .Build();

        // Act
        var component = Render(new SelectTestModel(), config);

        // Assert
        component.FindComponents<FluentTextInput>().ShouldNotBeEmpty();
        component.FindComponents<FluentSelect<SelectOption<string>, string>>().ShouldBeEmpty();
    }

    [Fact]
    public void Non_String_Options_Should_Preserve_Their_Value_Type()
    {
        // Arrange - an int-valued select must not be flattened to strings
        var config = FormBuilder<SelectTestModel>.Create()
            .AddField(x => x.Rating, f => f
                .WithLabel("Rating")
                .WithOptions((1, "One"), (2, "Two")))
            .Build();

        // Act
        var component = Render(new SelectTestModel { Rating = 2 }, config);

        // Assert
        var select = component.FindComponent<FluentSelect<SelectOption<int>, int>>().Instance;
        select.Value.ShouldBe(2);
        select.Items!.Select(o => o.Value).ShouldBe([1, 2]);
    }

    [Fact]
    public void Required_Select_Should_Announce_Itself()
    {
        // Arrange
        var config = FormBuilder<SelectTestModel>.Create()
            .AddField(x => x.Country, f => f
                .WithLabel("Country")
                .WithOptions(("BE", "Belgium"))
                .Required("Pick one"))
            .Build();

        // Act
        var component = Render(new SelectTestModel(), config);

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }
}
