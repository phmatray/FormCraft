using FormCraft.ForMudBlazor.UnitTests.TestSupport;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Coverage for <see cref="MudBlazorLovFieldComponent{TModel,TValue,TItem}"/>'s multi-select chip
/// path (issue #461, AC4) — the <c>IsMultiSelect &amp;&amp; SelectedItems.Any()</c> branch in
/// <c>MudBlazorLovFieldComponent.razor</c> (lines 32-42), unexercised before this file: one chip per
/// selected item, and <c>HandleChipClose</c> removing a selection via a chip's close button.
/// </summary>
/// <remarks>
/// Configured with a scalar <c>object</c>-typed field rather than <c>.AsMultiSelectLov(...)</c>
/// (the issue's own suggested entry point): <c>MudBlazorLovFieldComponent.ApplySelection</c>'s
/// multi-select branch does <c>(TValue)(object)values</c> where <c>values</c> is a
/// <c>List&lt;TValue&gt;</c> — that cast only succeeds when <c>TValue</c> is itself something every
/// <c>List&lt;T&gt;</c> satisfies, which a per-item scalar key (an <c>int</c>, as
/// <c>AsMultiSelectLov</c>'s own example selects) is not. <c>object</c> is the smallest field type
/// that reaches the real chip-render/chip-close code path through the library's actual selection
/// flow — production code is out of scope for this test-only issue, so this works around the
/// concern rather than fixing it.
/// </remarks>
/// <remarks>
/// The chip-close path itself carries a second, independent bug — filed as #475 rather than fixed
/// here, since #461 is test-only ("No production changes; additive tests only"). See
/// <see cref="Closing_A_Chip_Currently_Clears_The_Whole_Selection_Bug475"/>.
/// </remarks>
public class MudBlazorLovFieldMultiSelectTests : MudBlazorTestBase
{
    private static readonly List<LovCustomer> Customers =
    [
        new(1, "Acme"),
        new(2, "Globex"),
        new(3, "Initech"),
    ];

    [Fact]
    public async Task Selecting_Multiple_Items_Should_Render_One_Chip_Each()
    {
        var model = new LovModel();
        Services.AddSingleton(StubDialogService.Returning<LovSelectionDialog<LovCustomer, object>>(
            new LovSelectionResult<LovCustomer> { SelectedItems = [Customers[0], Customers[1]] }));

        var component = Render<FormCraftComponent<LovModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, LovConfig()));

        await component.Find("button").ClickAsync(new());

        component.FindComponents<MudChip<LovCustomer>>().Count.ShouldBe(2);

        // The chips are a VIEW of _selectedItems; also prove the actual model write ApplySelection
        // performs (SetValueWithoutNotification/NotifyValueChangedAsync) actually reached the model.
        model.SelectedCustomer.ShouldNotBeNull();
        ((List<object>)model.SelectedCustomer!).Select(v => (int)v).ShouldBe([1, 2]);
    }

    /// <summary>
    /// ⚠️ Pins a KNOWN BUG rather than the intended behaviour — tracked as #475.
    /// <c>HandleChipClose</c> passes the field's own <c>_selectedItems</c> backing list straight
    /// into <c>ApplySelection(List&lt;TItem&gt; items)</c>, which immediately does
    /// <c>_selectedItems.Clear(); _selectedItems.AddRange(items);</c> — since <c>items</c> and
    /// <c>_selectedItems</c> are the SAME list, the <c>Clear()</c> empties <c>items</c> too before
    /// <c>AddRange</c> can read from it, so closing ANY chip empties the WHOLE selection rather than
    /// just the closed item. This issue is test-only (#461's own Spec: "No production changes;
    /// additive tests only"), so the fix belongs in #475, not here. Once #475 lands, flip this
    /// assertion to "one chip remains, for the item not closed."
    /// </summary>
    [Fact]
    public async Task Closing_A_Chip_Currently_Clears_The_Whole_Selection_Bug475()
    {
        var model = new LovModel();
        Services.AddSingleton(StubDialogService.Returning<LovSelectionDialog<LovCustomer, object>>(
            new LovSelectionResult<LovCustomer> { SelectedItems = [Customers[0], Customers[1]] }));

        var component = Render<FormCraftComponent<LovModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, LovConfig()));

        await component.Find("button").ClickAsync(new());
        component.FindComponents<MudChip<LovCustomer>>().Count.ShouldBe(2);

        var chipSet = component.FindComponent<MudChipSet<LovCustomer>>();
        var firstChip = component.FindComponents<MudChip<LovCustomer>>()[0].Instance;
        await component.InvokeAsync(() => chipSet.Instance.OnClose.InvokeAsync(firstChip));

        // BUG (#475): should be 1 (only the closed chip removed); currently both are gone.
        component.FindComponents<MudChip<LovCustomer>>().Count.ShouldBe(0);
    }

    private static IFormConfiguration<LovModel> LovConfig() =>
        FormBuilder<LovModel>.Create()
            .AddField(x => x.SelectedCustomer, field => field
                .WithLabel("Customer")
                .AsLov<LovModel, object, LovCustomer>(lov => lov
                    .WithKey(c => (object)c.Id)
                    .WithDisplay(c => c.Name)
                    .WithDataSource(() => Customers)
                    .AddColumn(c => c.Name, "Name")
                    .AllowMultipleSelection()))
            .Build();

    private class LovModel
    {
        public object? SelectedCustomer { get; set; }
    }

    private record LovCustomer(int Id, string Name);
}
