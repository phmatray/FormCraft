

namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>
/// A field configured one way must present the same way whether it sits at the top level of a form
/// or inside <c>.WithItemForm(...)</c> (#278).
/// </summary>
/// <remarks>
/// <para>
/// The MudBlazor suite of this name exists because those two placements were once rendered by two
/// different implementations, and it repeatedly caught them disagreeing - #146 (Variant), #177
/// (ShrinkLabel), #184 (adornments), #190 (Required). Since #203 there is one implementation there,
/// and this adapter was built on that single path from the start, so these comparisons are close to
/// a tautology by construction.
/// </para>
/// <para>
/// That is the intended end state rather than a reason to skip them: they pin the <i>wiring</i>, not
/// the attributes. A regression that gave collection item fields their own render path - the exact
/// shortcut #203 removed - would surface here first, before it had grown its own list of
/// known divergences.
/// </para>
/// </remarks>
public class RenderPipelineParityTests : FluentUITestBase
{
    /// <summary>The presentation facts compared across the two placements.</summary>
    private sealed record Presentation(
        string? Label,
        string? Placeholder,
        string? AriaRequired,
        bool? Disabled,
        bool? ReadOnly);

    [Fact]
    public void A_Plain_Field_Should_Present_Identically_In_Both_Placements()
    {
        // Arrange & Act
        var standalone = RenderStandalone(f => f.WithLabel("Product").WithPlaceholder("e.g. widget"));
        var inCollection = RenderInCollection(f => f.WithLabel("Product").WithPlaceholder("e.g. widget"));

        // Assert
        inCollection.ShouldBe(standalone);
        standalone.Label.ShouldBe("Product");
        standalone.Placeholder.ShouldBe("e.g. widget");
    }

    [Fact]
    public void A_Required_Field_Should_Announce_Itself_In_Both_Placements()
    {
        // Arrange & Act - the accessibility guarantee must not depend on placement (#199)
        var standalone = RenderStandalone(f => f.WithLabel("Product").Required("Product is required"));
        var inCollection = RenderInCollection(f => f.WithLabel("Product").Required("Product is required"));

        // Assert
        standalone.AriaRequired.ShouldBe("true");
        inCollection.ShouldBe(standalone);
    }

    [Fact]
    public void An_Optional_Field_Should_Omit_AriaRequired_In_Both_Placements()
    {
        // Arrange & Act
        var standalone = RenderStandalone(f => f.WithLabel("Product"));
        var inCollection = RenderInCollection(f => f.WithLabel("Product"));

        // Assert
        standalone.AriaRequired.ShouldBeNull();
        inCollection.ShouldBe(standalone);
    }

    [Fact]
    public void The_Native_Required_Opt_Out_Should_Apply_In_Both_Placements()
    {
        // Arrange & Act - the raw form of .WithNativeRequired(false), which must win in both
        // directions and in both placements (#199, #204)
        var standalone = RenderStandalone(f => f
            .WithLabel("Product").Required("Product is required").WithAttribute("Required", false));
        var inCollection = RenderInCollection(f => f
            .WithLabel("Product").Required("Product is required").WithAttribute("Required", false));

        // Assert
        standalone.AriaRequired.ShouldBeNull();
        inCollection.ShouldBe(standalone);
    }

    [Fact]
    public void A_Disabled_Field_Should_Present_Identically_In_Both_Placements()
    {
        // Arrange & Act
        var standalone = RenderStandalone(f => f.WithLabel("Product").Disabled());
        var inCollection = RenderInCollection(f => f.WithLabel("Product").Disabled());

        // Assert
        standalone.Disabled.ShouldBe(true);
        inCollection.ShouldBe(standalone);
    }

    [Fact]
    public void A_ReadOnly_Field_Should_Present_Identically_In_Both_Placements()
    {
        // Arrange & Act
        var standalone = RenderStandalone(f => f.WithLabel("Product").ReadOnly());
        var inCollection = RenderInCollection(f => f.WithLabel("Product").ReadOnly());

        // Assert
        standalone.ReadOnly.ShouldBe(true);
        inCollection.ShouldBe(standalone);
    }

    // -------------------------------------------------------------------------------------------
    // The two placements. Both funnel into the same Read(), so the comparison cannot drift by
    // reading one placement differently from the other - which would turn a real divergence into a
    // passing test.
    // -------------------------------------------------------------------------------------------

    private Presentation RenderStandalone(Action<FieldBuilder<ParityLine, string>> configure)
    {
        var config = FormBuilder<ParityLine>.Create()
            .AddField(x => x.Product, configure)
            .Build();

        var component = Render<FormCraftComponent<ParityLine>>(p => p
            .Add(c => c.Model, new ParityLine())
            .Add(c => c.Configuration, config));

        return Read(component);
    }

    private Presentation RenderInCollection(Action<FieldBuilder<ParityLine, string>> configure)
    {
        var config = FormBuilder<ParityOrder>.Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithLabel("Lines")
                .AllowAdd()
                .AllowRemove()
                .WithItemForm(item => item.AddField(x => x.Product, configure)))
            .Build();

        var component = Render<FormCraftComponent<ParityOrder>>(p => p
            .Add(c => c.Model, new ParityOrder { Lines = { new ParityLine() } })
            .Add(c => c.Configuration, config));

        return Read(component);
    }

    /// <summary>
    /// Reads the compared facts from the one <c>FluentTextInput</c> the render produced.
    /// </summary>
    /// <remarks>
    /// Two sources, deliberately. <c>Label</c>, <c>Placeholder</c>, <c>Disabled</c> and
    /// <c>ReadOnly</c> are Fluent parameters that do not surface as attributes on the
    /// <c>fluent-text-input</c> element, so they are read off the component instance;
    /// <c>aria-required</c> is splatted straight onto the DOM by this library and is read from
    /// there — which is also the only place a screen reader would find it.
    /// <para>
    /// <c>FluentTextInput</c> is not generic, so one type serves both placements even though the
    /// FormCraft components around it close over different model types.
    /// </para>
    /// </remarks>
    private static Presentation Read(IRenderedComponent<IComponent> rendered)
    {
        var input = rendered.FindComponent<FluentTextInput>().Instance;
        return new Presentation(
            input.Label,
            input.Placeholder,
            rendered.Find("fluent-text-input").GetAttribute("aria-required"),
            input.Disabled,
            input.ReadOnly);
    }

    /// <summary>Parent model owning the collection placement.</summary>
    public class ParityOrder
    {
        /// <summary>The collection whose item form holds the compared field.</summary>
        public List<ParityLine> Lines { get; set; } = new();
    }

    /// <summary>Serves as both the item type and the standalone model, so the field is identical.</summary>
    public class ParityLine
    {
        /// <summary>The field compared across placements.</summary>
        public string Product { get; set; } = string.Empty;
    }
}
