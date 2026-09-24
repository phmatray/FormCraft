using Microsoft.Extensions.Logging;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// A collection field bound through a nested path whose intermediate is null must not crash on
/// render — the render-path sibling of #408's validation-path guard (#419), mirroring
/// <c>FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemsGetterNullIntermediateTests</c>. Since
/// #203/#278 every field, ordinary or collection, renders through the ordinary render pipeline, and
/// <c>FluentUICollectionFieldComponent&lt;TModel, TItem&gt;.Items</c> reads
/// <c>Configuration.CollectionAccessor(Model)</c> on essentially the first line of its own markup —
/// so an unreadable nested binding crashed the component the instant it rendered, before validation
/// ever ran.
/// </summary>
public class CollectionItemsGetterNullIntermediateTests : FluentUITestBase
{
    [Fact]
    public void Should_Render_As_Empty_When_The_Collections_Nested_Binding_Intermediate_Is_Null()
    {
        // Arrange - the collection is bound through `x => x.Details!.Items`, whose intermediate
        // (Details) is null. Before this fix, the Items getter called
        // Configuration.CollectionAccessor(Model) unguarded and threw a NullReferenceException.
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .WithEmptyText("No nested items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = null };

        // Act - must not throw.
        var component = Render<FormCraftComponent<ParentModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));

        // Assert - must not throw, and (#433) shows the distinct unreadable-binding message rather
        // than the configured EmptyText: a broken binding and a genuinely empty collection must not
        // look the same to the user.
        component.Markup.ShouldContain("could not be read from the model");
        component.Markup.ShouldNotContain("No nested items");
        component.FindComponents<FluentUITextFieldComponent<ChildItem>>().ShouldBeEmpty();
    }

    /// <summary>
    /// Models a nested collection scenario. <see cref="Details"/> is nullable to test the render
    /// guard for null-intermediate bindings. The structure mirrors
    /// <c>CollectionFieldValidatorTests.OrderModel</c> (#408).
    /// </summary>
    public class ParentModel
    {
        public DetailModel? Details { get; set; }
    }

    public class DetailModel
    {
        public List<ChildItem> Items { get; set; } = new();
    }

    /// <remarks>
    /// Carries a second property beyond <c>ProductName</c> deliberately —
    /// <c>CollectionItemShapeGuardTests</c> fails the build for a nested type whose shape duplicates
    /// a <c>CollectionItemFixture</c> item (a bare <c>string</c> is <see cref="OrderItem"/>'s exact
    /// shape). <c>Quantity</c> is otherwise unused by this suite.
    /// </remarks>
    public class ChildItem
    {
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }

    /// <summary>
    /// #433: Add and Remove must be inert (not just silently no-op) while the binding is unreadable —
    /// neither control renders at all, matching the existing HasReachedMax/HasReachedMin precedent for
    /// "not actionable right now".
    /// </summary>
    [Fact]
    public void Add_And_Remove_Controls_Should_Not_Render_When_The_Binding_Is_Unreadable()
    {
        // Arrange - same unreadable binding as the render-crash test above, but with Add/Remove
        // explicitly allowed so there is something for the gate to hide.
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .AllowAdd()
                .AllowRemove()
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = null };

        // Act
        var component = Render<FormCraftComponent<ParentModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));

        // Assert - neither control is in the render tree at all (AC1/AC2).
        component.FindAll("[data-testid=formcraft-collection-add]").ShouldBeEmpty();
        component.FindAll("[data-testid=formcraft-collection-remove]").ShouldBeEmpty();
    }

    /// <summary>
    /// #433 AC3: the diagnostic fires once for the field, not once per render.
    /// </summary>
    [Fact]
    public void A_Warning_Should_Be_Logged_Exactly_Once_For_An_Unreadable_Binding()
    {
        // Arrange
        var logs = new CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = null };

        // Act - three renders of the same still-unreadable binding.
        var component = Render<FormCraftComponent<ParentModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));
        component.Render();
        component.Render();

        // Assert - one warning, not three.
        logs.Warnings.Count(w => w.Contains("could not read its bound collection from the model")).ShouldBe(1);
    }

    /// <summary>
    /// #433 AC5: a binding that recovers re-enables Add/Remove with no stale disabled state, and a
    /// click after recovery reaches the real model list rather than a throwaway one.
    /// </summary>
    [Fact]
    public async Task Recovering_The_Binding_Should_Re_Enable_Add_And_Let_It_Reach_The_Real_List()
    {
        // Arrange - starts unreadable.
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .WithEmptyText("No nested items")
                .AllowAdd()
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = null };

        var component = Render<FormCraftComponent<ParentModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));
        component.FindAll("[data-testid=formcraft-collection-add]").ShouldBeEmpty();

        // Act - the intermediate becomes readable again, the way #419/#433's scenario describes.
        model.Details = new DetailModel();
        component.Render();

        // Assert - EmptyText is back (not the unreadable message), and Add works normally again.
        component.Markup.ShouldContain("No nested items");
        component.Markup.ShouldNotContain("could not be read from the model");
        await component.InvokeAsync(() =>
            component.Find("[data-testid=formcraft-collection-add]").Click());
        model.Details!.Items.Count.ShouldBe(1);
    }

    /// <summary>
    /// #433 edge case: a genuinely empty but readable collection must keep rendering exactly as it
    /// does today — the two states must not merge into one bucket.
    /// </summary>
    [Fact]
    public void A_Genuinely_Empty_Readable_Collection_Should_Still_Show_EmptyText()
    {
        // Arrange - Details is non-null but its Items list is empty: readable, just empty.
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Details!.Items, collection => collection
                .WithLabel("Items")
                .WithEmptyText("No nested items")
                .WithItemForm(item => item
                    .AddField(x => x.ProductName, field => field.WithLabel("Product"))))
            .Build();
        var model = new ParentModel { Details = new DetailModel() };

        // Act
        var component = Render<FormCraftComponent<ParentModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));

        // Assert
        component.Markup.ShouldContain("No nested items");
        component.Markup.ShouldNotContain("could not be read from the model");
    }

    /// <summary>
    /// Collects warning-level log messages so the unreadable-binding diagnostic (#433) can be
    /// asserted on. Local rather than shared: mirrors <c>CustomTemplateTests.CapturingLoggerProvider</c>
    /// exactly, since this project still has no shared <c>TestSupport</c> type for it.
    /// </summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<(string Category, string Message)> _entries = [];

        public IReadOnlyList<string> Warnings
        {
            get
            {
                lock (_entries)
                {
                    return _entries.Select(entry => entry.Message).ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(string category, List<(string Category, string Message)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (logLevel < LogLevel.Warning)
                {
                    return;
                }

                lock (entries)
                {
                    entries.Add((category, formatter(state, exception)));
                }
            }
        }
    }
}
