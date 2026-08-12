using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that collection item fields honor the configurable ShrinkLabel (#177).
/// </summary>
/// <remarks>
/// Written when item fields rendered through CollectionFieldComponent's imperative
/// RenderTreeBuilder path, which resolved ShrinkLabel in its own <c>GetItemFieldShrinkLabel</c>
/// rather than through <c>MudBlazorFieldComponentBase.EffectiveShrinkLabel</c> — a duplicate
/// resolver, hence duplicate coverage.
/// <para>
/// #203 removed that duplicate: item fields resolve ShrinkLabel through the one component property,
/// including the form-level cascade fallback these tests exercise. They pass unmodified and are kept
/// as the guard that the item placement keeps inheriting it.
/// </para>
/// <para>
/// Models and the item-form builder come from <see cref="CollectionItemFixture"/> (#205). Every test
/// renders the <c>"Widget"</c> seed the local model used to hard-code, so a populated field is what
/// the ShrinkLabel assertions see — the same thing they saw before the migration.
/// </para>
/// </remarks>
public class CollectionShrinkLabelTests : MudBlazorTestBase
{
    [Fact]
    public void ItemField_Should_Default_To_ShrinkLabel_True()
    {
        // Arrange & Act
        var component = this.RenderItemForm(NewOrder("Widget"), TextItemForm());

        // Assert - unchanged from before #177
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    [Fact]
    public void ItemField_Should_Honor_FieldLevel_WithShrinkLabel()
    {
        // Arrange & Act
        var component = this.RenderItemForm(
            NewOrder("Widget"), TextItemForm(field => field.WithShrinkLabel(false)));

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void ItemField_Should_Honor_FormLevel_DefaultShrinkLabel()
    {
        // Arrange & Act - the cascade has to reach into the collection component too
        var component = RenderWithFormDefault(TextItemForm(), defaultShrinkLabel: false);

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeFalse();
    }

    [Fact]
    public void ItemField_FieldLevel_Should_Override_FormLevel()
    {
        // Arrange & Act
        var component = RenderWithFormDefault(
            TextItemForm(field => field.WithShrinkLabel(true)), defaultShrinkLabel: false);

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.ShrinkLabel.ShouldBeTrue();
    }

    /// <summary>
    /// The two form-level tests need a <c>DefaultShrinkLabel</c> parameter that the fixture's
    /// <c>RenderItemForm</c> does not set — it renders Model and Configuration only, deliberately, so
    /// it stays the shape every suite shares. This local helper adds just that one parameter; the
    /// model and the item form still come from the fixture.
    /// </summary>
    private IRenderedComponent<FormCraftComponent<OrderModel>> RenderWithFormDefault(
        IFormConfiguration<OrderModel> config, bool defaultShrinkLabel) =>
        Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, NewOrder("Widget"))
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultShrinkLabel, defaultShrinkLabel));
}
