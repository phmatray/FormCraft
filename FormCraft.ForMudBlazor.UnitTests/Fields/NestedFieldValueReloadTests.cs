namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// A field bound through a nested path is read, written, grouped and depended on through that
/// nested member — never through a same-named top-level "decoy" member of the model (#437).
/// </summary>
/// <remarks>
/// <c>FieldName</c> became the full dotted path (<c>"Billing.Amount"</c>) under #437. Every consumer
/// that used to look the member up by a bare name had to move with it: <c>FieldComponentBase</c>'s
/// reload read the decoy, then nothing; the collection write path dropped nested item edits; and the
/// adapters' group and dependency lookups key on <c>FieldName</c>.
/// </remarks>
public class NestedFieldValueReloadTests : MudBlazorTestBase
{
    [Fact]
    public void A_Nested_Field_Should_Show_The_Nested_Value_After_An_External_Mutation_Not_A_Decoys()
    {
        // Arrange
        var model = new DecoyModel { Amount = "decoy", Billing = new BillingSection { Amount = "first" } };
        var component = RenderForm(model, FormBuilder<DecoyModel>.Create().AddField(x => x.Billing.Amount).Build());

        component.Find("input").GetAttribute("value").ShouldBe("first");

        // Act - a programmatic change (a dependency callback, application code) followed by a render.
        model.Billing.Amount = "second";
        component.Render();

        // Assert
        component.Find("input").GetAttribute("value").ShouldBe("second");
    }

    [Fact]
    public void A_Nested_Field_Whose_Intermediate_Becomes_Null_Should_Keep_Rendering()
    {
        // Arrange
        var model = new DecoyModel { Billing = new BillingSection { Amount = "first" } };
        var component = RenderForm(model, FormBuilder<DecoyModel>.Create().AddField(x => x.Billing.Amount).Build());

        // Act - the reload's read now goes through the compiled getter, which must treat a null
        // intermediate as "unreachable" (fall back) rather than throw out of the render.
        model.Billing = null!;

        // Assert
        Should.NotThrow(() => component.Render());
        component.FindAll("input").Count.ShouldBe(1);
    }

    [Fact]
    public void A_Nested_Field_In_A_Group_Should_Render_Inside_That_Group()
    {
        // Arrange - the group records the field by the name the adapter matches on (f.FieldName).
        var config = FormBuilder<DecoyModel>.Create()
            .AddFieldGroup(group => group.WithGroupName("Billing").AddField(x => x.Billing.Amount))
            .Build();

        // Act
        var component = RenderForm(new DecoyModel(), config);

        // Assert - inside the group's MudGrid item, not the ungrouped section's plain div.
        component.FindAll(".mud-grid-item input").Count.ShouldBe(1);
        component.FindAll("input").Count.ShouldBe(1);
    }

    [Fact]
    public void A_DependsOn_A_Nested_Field_Should_Fire_When_That_Field_Is_Edited()
    {
        // Arrange
        var model = new DecoyModel();
        var config = FormBuilder<DecoyModel>.Create()
            .AddField(x => x.Billing.Amount)
            .AddField(x => x.Note, field => field.DependsOn(x => x.Billing.Amount, (m, amount) => m.Note = $"saw {amount}"))
            .Build();
        var component = RenderForm(model, config);

        // Act
        component.FindAll("input")[0].Input("42");

        // Assert
        component.WaitForAssertion(() => model.Note.ShouldBe("saw 42"));
    }

    [Fact]
    public void Editing_A_Nested_Item_Field_Should_Write_The_Nested_Member_And_Validate_Its_Cell()
    {
        // Arrange
        var model = new LinesModel { Lines = { new Line() } };
        EditContext? editContext = null;
        var config = FormBuilder<LinesModel>.Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithItemForm(item => item
                    .AddField(x => x.Details.Name, field => field.WithValidator(v => v != "bad", "Name is bad"))))
            .Build();
        var component = this.RenderItemForm(model, config,
            parameters => parameters.Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        // Act
        component.Find("input").Input("bad");

        // Assert - written through the binding, not to the same-named top-level decoy, and the
        // per-keystroke validation reached the cell under its full nested identifier.
        component.WaitForAssertion(() =>
        {
            model.Lines[0].Details.Name.ShouldBe("bad");
            model.Lines[0].Name.ShouldBe(string.Empty);
            editContext!.GetValidationMessages(new FieldIdentifier(model, "Lines[0].Details.Name"))
                .ShouldContain("Name is bad");
        });
    }

    private IRenderedComponent<FormCraftComponent<DecoyModel>> RenderForm(DecoyModel model, IFormConfiguration<DecoyModel> config)
        => Render<FormCraftComponent<DecoyModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

    public class DecoyModel
    {
        /// <summary>Same name as the nested member the field is bound to; must never be read.</summary>
        public string Amount { get; set; } = string.Empty;

        public BillingSection Billing { get; set; } = new();

        public string Note { get; set; } = string.Empty;
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
        /// <summary>Same name as the nested item member; must never be written.</summary>
        public string Name { get; set; } = string.Empty;

        public LineDetails Details { get; set; } = new();
    }

    public class LineDetails
    {
        public string Name { get; set; } = string.Empty;
    }
}
