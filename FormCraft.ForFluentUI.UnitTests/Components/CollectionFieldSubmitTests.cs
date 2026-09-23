namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>
/// Collection validation participates in submit, now that the Fluent container renders collection
/// fields (#278).
/// </summary>
/// <remarks>
/// <para>
/// This suite previously asserted the opposite, and was right to. Fluent had no collection renderer
/// (blocked on #203), so #279 gave the shared <see cref="DynamicFormValidator{TModel}"/> a
/// <c>ValidateCollections="false"</c> opt-out for this adapter: a collection error would otherwise
/// land on a field identifier nothing rendered, with no <c>FieldValidationMessage</c> to show it
/// and <c>HandleSubmit</c> gating <c>OnValidSubmit</c> on the result — a submit button that simply
/// stopped working, with nothing on screen to explain why.
/// </para>
/// <para>
/// #279's own instruction was to turn it on "once the adapter actually renders collection fields -
/// not before". #278 is that moment: the container renders each collection as a card with its rows,
/// its empty state and its add/remove controls, so a <c>MinItems</c> violation now has both a
/// visible cause and a control that fixes it. The flag is therefore <c>true</c>, and blocking is
/// the correct behaviour rather than a dead end.
/// </para>
/// <para>
/// ⛔ Do not set <c>ValidateCollections</c> back to <c>false</c> while collection fields render: it
/// would accept submissions that violate collection validators, silently.
/// </para>
/// <para>
/// Uses <see cref="NamedOrderModel"/>/<see cref="NamedOrderItem"/> rather than a local model
/// (#343): this suite needs a root-level field (<c>Reference</c>, tested independently of the
/// collection) <i>beside</i> the collection, which is <c>NamedOrderModel</c>'s shape — the fixture's
/// plain <c>OrderModel</c> has no root field to spare.
/// </para>
/// </remarks>
public class CollectionFieldSubmitTests : FluentUITestBase
{
    [Fact]
    public async Task Submitting_Should_Be_Blocked_When_A_Rendered_Collection_Field_Is_Invalid()
    {
        // Arrange - MinItems(1) against an empty collection.
        var model = new NamedOrderModel { Name = "A-1" };
        var config = FormBuilder<NamedOrderModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Reference"))
            .AddCollectionField(x => x.Items, c => c
                .WithLabel("Items")
                .AllowAdd("Add item")
                .WithMinItems(1)
                .WithItemForm(item => item.AddField(x => x.Name)))
            .Build();

        var submitted = false;
        var component = Render<FormCraftComponent<NamedOrderModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config)
            .Add(c => c.OnValidSubmit, _ => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert - blocked, and crucially the user has somewhere to act: the collection is on
        // screen with an add control, which is what makes blocking honest rather than a dead end.
        submitted.ShouldBeFalse();
        component.Find("[data-testid=formcraft-collection-add]").ShouldNotBeNull();
    }

    [Fact]
    public async Task Submitting_Should_Succeed_Once_The_Collection_Satisfies_Its_Rule()
    {
        // Arrange - the same configuration, with the row the rule asks for.
        var model = new NamedOrderModel
        {
            Name = "A-1",
            Items = { new NamedOrderItem { Name = "Widget" } },
        };
        var config = FormBuilder<NamedOrderModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Reference"))
            .AddCollectionField(x => x.Items, c => c
                .WithLabel("Items")
                .AllowAdd("Add item")
                .WithMinItems(1)
                .WithItemForm(item => item.AddField(x => x.Name)))
            .Build();

        var submitted = false;
        var component = Render<FormCraftComponent<NamedOrderModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config)
            .Add(c => c.OnValidSubmit, _ => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        component.WaitForAssertion(() => submitted.ShouldBeTrue());
    }

    [Fact]
    public async Task Submitting_Should_Still_Be_Blocked_By_An_Ordinary_Invalid_Field()
    {
        // Arrange - the other half of the contract: collection validation must not be the only
        // thing that blocks, or a change here could quietly become a hole.
        var model = new NamedOrderModel();
        var config = FormBuilder<NamedOrderModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Reference").Required("Reference is required"))
            .AddCollectionField(x => x.Items, c => c.WithLabel("Items").AllowAdd())
            .Build();

        var submitted = false;
        var component = Render<FormCraftComponent<NamedOrderModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config)
            .Add(c => c.OnValidSubmit, _ => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        submitted.ShouldBeFalse();
        component.Instance.GetEditContext()!.GetValidationMessages()
            .ShouldContain("Reference is required");
    }
}
