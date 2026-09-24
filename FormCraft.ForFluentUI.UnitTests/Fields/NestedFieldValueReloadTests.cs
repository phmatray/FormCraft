namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// FluentUI parity for the MudBlazor suite of the same name (#437): a nested binding is read and
/// written through its own member, never through a same-named top-level "decoy".
/// </summary>
public class NestedFieldValueReloadTests : FluentUITestBase
{
    [Fact]
    public void A_Nested_Field_Should_Show_The_Nested_Value_After_An_External_Mutation_Not_A_Decoys()
    {
        // Arrange
        var model = new DecoyModel { Amount = "decoy", Billing = new BillingSection { Amount = "first" } };
        var component = Render<FormCraftComponent<DecoyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, FormBuilder<DecoyModel>.Create().AddField(x => x.Billing.Amount).Build()));

        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("first");

        // Act
        model.Billing.Amount = "second";
        component.Render();

        // Assert
        component.FindComponent<FluentTextInput>().Instance.Value.ShouldBe("second");
    }

    [Fact]
    public async Task Editing_A_Nested_Item_Field_Should_Write_The_Nested_Member_Not_A_Decoy()
    {
        // Arrange
        var model = new LinesModel { Lines = { new Line() } };
        var component = Render<FormCraftComponent<LinesModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, FormBuilder<LinesModel>.Create()
                .AddCollectionField(x => x.Lines, collection => collection
                    .WithItemForm(item => item.AddField(x => x.Details.Name)))
                .Build()));

        // Act
        var field = component.FindComponent<FluentUITextFieldComponent<Line>>();
        await component.InvokeAsync(() => field.Instance.Context.OnValueChanged.InvokeAsync("widget"));

        // Assert
        model.Lines[0].Details.Name.ShouldBe("widget");
        model.Lines[0].Name.ShouldBe(string.Empty);
    }

    public class DecoyModel
    {
        public string Amount { get; set; } = string.Empty;

        public BillingSection Billing { get; set; } = new();
    }

    public class BillingSection
    {
        public string Amount { get; set; } = string.Empty;
    }

    public class LinesModel
    {
        public List<Line> Lines { get; set; } = new();
    }

    public class Line
    {
        public string Name { get; set; } = string.Empty;

        public LineDetails Details { get; set; } = new();
    }

    public class LineDetails
    {
        public string Name { get; set; } = string.Empty;
    }
}
