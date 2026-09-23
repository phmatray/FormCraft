namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Rendering half of the collection-item fixture. It is an extension on <see cref="BunitContext"/>
/// rather than a member of <c>MudBlazorTestBase</c> on purpose: the base class is about how to render
/// anything, and pushing collection-specific helpers into it would burden every MudBlazor test that
/// has nothing to do with collections.
/// </summary>
/// <remarks>
/// The models, factories and item-form builders this renders moved to
/// <c>FormCraft.TestSupport.CollectionItemFixture</c> in #343, so the Fluent UI adapter's test suite
/// can share them too. This half stays here because it is bUnit/MudBlazor-flavoured —
/// <c>FormCraftComponent</c> plus this assembly's own render pipeline — and each adapter suite keeps
/// its own render helper rather than sharing one. <c>global using FormCraft.TestSupport;</c>
/// (<c>GlobalUsings.cs</c>) keeps every existing call site in this assembly resolving unchanged.
/// </remarks>
internal static class CollectionItemFixtureRenderExtensions
{
    /// <summary>
    /// Renders <paramref name="model"/> through <c>FormCraftComponent</c> with the given configuration,
    /// plus any extra component parameters <paramref name="configure"/> adds.
    /// This is the shape every collection-item suite had open-coded per test.
    /// </summary>
    /// <remarks>
    /// <paramref name="configure"/> exists because Model and Configuration are not quite all a suite
    /// ever needs: some also pass <c>DefaultShrinkLabel</c> (the form-level cascade) or
    /// <c>OnEditContextCreated</c> (to capture the form's <c>EditContext</c>). Without a way to add
    /// those, every such test re-opened <c>Render&lt;FormCraftComponent&lt;T&gt;&gt;</c> by hand and
    /// re-implemented the Model/Configuration wiring this helper owns — so the "shape every suite
    /// shares" was in fact shared by only some of them. It runs after the two required parameters, so
    /// existing call sites are unaffected.
    /// </remarks>
    internal static IRenderedComponent<FormCraftComponent<TModel>> RenderItemForm<TModel>(
        this BunitContext context,
        TModel model,
        IFormConfiguration<TModel> configuration,
        Action<ComponentParameterCollectionBuilder<FormCraftComponent<TModel>>>? configure = null)
        where TModel : new() =>
        context.Render<FormCraftComponent<TModel>>(parameters =>
        {
            parameters
                .Add(p => p.Model, model)
                .Add(p => p.Configuration, configuration);
            configure?.Invoke(parameters);
        });
}
