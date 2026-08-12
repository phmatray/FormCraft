namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>Model covering a non-nullable and a nullable boolean property.</summary>
public class BooleanTestModel
{
    /// <summary>A non-nullable boolean field.</summary>
    public bool IsActive { get; set; }

    /// <summary>A nullable boolean field.</summary>
    public bool? HasConsented { get; set; }
}

/// <summary>
/// Covers boolean rendering, the checkbox/switch choice, and required announcement.
/// </summary>
public class FluentUIBooleanFieldComponentTests : FluentUITestBase
{
    private IRenderedComponent<FormCraftComponent<BooleanTestModel>> Render(
        BooleanTestModel model, IFormConfiguration<BooleanTestModel> config) =>
        Render<FormCraftComponent<BooleanTestModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));

    [Fact]
    public void Boolean_Field_Should_Render_A_Checkbox_By_Default()
    {
        // Arrange
        var config = FormBuilder<BooleanTestModel>.Create()
            .AddField(x => x.IsActive, f => f.WithLabel("Active"))
            .Build();

        // Act
        var component = Render(new BooleanTestModel(), config);

        // Assert
        component.FindComponents<FluentCheckbox>().ShouldNotBeEmpty();
        component.FindComponents<FluentSwitch>().ShouldBeEmpty();
    }

    [Fact]
    public void Boolean_Field_Should_Render_A_Switch_When_Asked()
    {
        // Arrange - the core BooleanDisplayStyle contract, identical on both adapters
        var config = FormBuilder<BooleanTestModel>.Create()
            .AddField(x => x.IsActive, f => f
                .WithLabel("Active")
                .WithAttribute("DisplayStyle", BooleanDisplayStyle.Switch))
            .Build();

        // Act
        var component = Render(new BooleanTestModel(), config);

        // Assert
        component.FindComponents<FluentSwitch>().ShouldNotBeEmpty();
        component.FindComponents<FluentCheckbox>().ShouldBeEmpty();
    }

    [Fact]
    public void Boolean_Field_Should_Load_The_Existing_Model_Value()
    {
        // Arrange
        var config = FormBuilder<BooleanTestModel>.Create()
            .AddField(x => x.IsActive, f => f.WithLabel("Active"))
            .Build();

        // Act
        var component = Render(new BooleanTestModel { IsActive = true }, config);

        // Assert
        component.FindComponent<FluentCheckbox>().Instance.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task Toggling_The_Checkbox_Should_Write_Back_To_The_Model()
    {
        // Arrange
        var model = new BooleanTestModel();
        var config = FormBuilder<BooleanTestModel>.Create()
            .AddField(x => x.IsActive, f => f.WithLabel("Active"))
            .Build();
        var component = Render(model, config);
        var checkbox = component.FindComponent<FluentCheckbox>();

        // Act
        await component.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(true));

        // Assert
        model.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Nullable_Boolean_Field_Should_Render_As_Unchecked()
    {
        // Arrange - null is shown as unchecked, matching the MudBlazor adapter rather than
        // adopting Fluent's three-state checkbox and diverging from it
        var config = FormBuilder<BooleanTestModel>.Create()
            .AddField(x => x.HasConsented, f => f.WithLabel("Consent"))
            .Build();

        // Act
        var component = Render(new BooleanTestModel { HasConsented = null }, config);

        // Assert
        component.FindComponent<FluentCheckbox>().Instance.Value.ShouldBeFalse();
    }

    [Fact]
    public void Required_Boolean_Field_Should_Announce_Itself()
    {
        // Arrange
        var config = FormBuilder<BooleanTestModel>.Create()
            .AddField(x => x.IsActive, f => f.WithLabel("Active").Required("Must accept"))
            .Build();

        // Act
        var component = Render(new BooleanTestModel(), config);

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }
}
