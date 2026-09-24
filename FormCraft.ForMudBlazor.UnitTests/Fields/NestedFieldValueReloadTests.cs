namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// A field bound through a nested path reloads its value from that nested member after an external
/// model mutation — never from a same-named top-level "decoy" member of the model (#437).
/// </summary>
/// <remarks>
/// <c>FieldComponentBase</c> used to reload through <c>GetProperty(Context.Field.FieldName)</c>. While
/// <c>FieldName</c> was the last member only, that read the decoy; once it became the full dotted path,
/// the flat lookup found nothing and the field stopped picking up external changes at all.
/// </remarks>
public class NestedFieldValueReloadTests : MudBlazorTestBase
{
    [Fact]
    public void A_Nested_Field_Should_Show_The_Nested_Value_After_An_External_Mutation_Not_A_Decoys()
    {
        // Arrange
        var model = new DecoyModel { Amount = "decoy", Billing = new BillingSection { Amount = "first" } };
        var config = FormBuilder<DecoyModel>.Create()
            .AddField(x => x.Billing.Amount)
            .Build();

        var component = Render<FormCraftComponent<DecoyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        component.Find("input").GetAttribute("value").ShouldBe("first");

        // Act - a programmatic change (a dependency callback, application code) followed by a render.
        model.Billing.Amount = "second";
        component.Render();

        // Assert
        component.Find("input").GetAttribute("value").ShouldBe("second");
    }

    public class DecoyModel
    {
        /// <summary>Same name as the nested member the field is bound to; must never be read.</summary>
        public string Amount { get; set; } = string.Empty;

        public BillingSection Billing { get; set; } = new();
    }

    public class BillingSection
    {
        public string Amount { get; set; } = string.Empty;
    }
}
