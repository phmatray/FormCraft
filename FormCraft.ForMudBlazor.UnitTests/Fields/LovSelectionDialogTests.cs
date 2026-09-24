using FormCraft.ForMudBlazor.UnitTests.TestSupport;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Direct-render coverage for <see cref="LovSelectionDialog{TItem,TValue}"/> — the dialog's own
/// search/select/cancel/multi-select behaviour, rendered on its own rather than only observed
/// through the outer field's stubbed <see cref="IDialogService"/>
/// (<see cref="FieldConfigurationParityTests.LovField_Row_Keeps_Its_Display_Text_After_A_Configuration_Swap"/>
/// covers that outer reaction already). Shown through a real <see cref="MudDialogProvider"/> and
/// <see cref="IDialogService"/> round trip — the same shape <see cref="MudBlazorLookupDialogTests"/>
/// uses — because <c>MudDialog</c> (the root element of this dialog's own markup) only renders its
/// content in that "shown via the provider" mode; a bare cascaded <see cref="IMudDialogInstance"/>
/// fake left it permanently invisible (`Visible` defaults to `false` for the OTHER, inline-dialog
/// mode `MudDialog` also supports), measured while writing this file.
/// </summary>
public class LovSelectionDialogTests : MudBlazorTestBase
{
    private static readonly List<LovCustomer> Items =
    [
        new(1, "Alice"),
        new(2, "Bob"),
        new(3, "Charlie"),
    ];

    [Fact]
    public async Task Dialog_Should_Render_A_Row_Per_Item()
    {
        var (provider, _) = await ShowDialog();

        DialogTestHelpers.RowCount(provider).ShouldBe(3);
    }

    [Fact]
    public async Task Searching_Should_Narrow_The_Rendered_Rows()
    {
        var (provider, _) = await ShowDialog();

        await provider.InvokeAsync(() => provider.Find("input").Input("ali"));

        DialogTestHelpers.RowCount(provider).ShouldBe(1);
    }

    [Fact]
    public async Task Choosing_A_Row_Then_Select_Should_Resolve_With_That_Item()
    {
        var (provider, reference) = await ShowDialog();

        await provider.InvokeAsync(() => DialogTestHelpers.Row(provider, "Bob").ClickAsync(new()));
        await provider.InvokeAsync(() => DialogTestHelpers.Button(provider, "Select").ClickAsync(new()));

        var result = await reference.Result;
        ResolvedWith(result!, Items[1]).ShouldBeTrue();
    }

    /// <summary>
    /// Single-select mode: clicking a second row REPLACES the first selection rather than adding to
    /// it (mirrors <c>AllowMultipleSelection</c>'s absence, per the issue's own Edge cases).
    /// </summary>
    [Fact]
    public async Task Single_Select_Choosing_A_Second_Row_Should_Replace_The_First()
    {
        var (provider, reference) = await ShowDialog();

        await provider.InvokeAsync(() => DialogTestHelpers.Row(provider, "Alice").ClickAsync(new()));
        await provider.InvokeAsync(() => DialogTestHelpers.Row(provider, "Bob").ClickAsync(new()));
        await provider.InvokeAsync(() => DialogTestHelpers.Button(provider, "Select").ClickAsync(new()));

        var result = await reference.Result;
        ResolvedWith(result!, Items[1]).ShouldBeTrue();
    }

    [Fact]
    public async Task Cancel_Should_Resolve_With_A_Cancelled_Result()
    {
        var (provider, reference) = await ShowDialog();

        await provider.InvokeAsync(() => DialogTestHelpers.Button(provider, "Cancel").ClickAsync(new()));

        var result = await reference.Result;
        result!.Canceled.ShouldBeTrue();
    }

    [Fact]
    public async Task MultiSelect_Should_Allow_Choosing_More_Than_One_Row()
    {
        var (provider, reference) = await ShowDialog(multiSelect: true);

        var grid = provider.FindComponent<MudDataGrid<LovCustomer>>();
        grid.Instance.MultiSelection.ShouldBeTrue();

        await provider.InvokeAsync(() => DialogTestHelpers.Row(provider, "Alice").ClickAsync(new()));
        await provider.InvokeAsync(() => DialogTestHelpers.Row(provider, "Charlie").ClickAsync(new()));
        await provider.InvokeAsync(() => DialogTestHelpers.Button(provider, "Select").ClickAsync(new()));

        var result = await reference.Result;
        SelectionCount(result!).ShouldBe(2);
    }

    /// <summary>
    /// Reopening the dialog with items already selected (the field's own reopen path —
    /// <c>MudBlazorLovFieldComponent.OpenLovDialog</c> passes its current <c>_selectedItems</c> as
    /// <c>SelectedItems</c>) must show them as already chosen, not force a fresh pick. Exercises
    /// <c>LovSelectionDialog.OnInitialized</c>'s seeding of <c>_selectedItemsSet</c> from a non-empty
    /// <c>SelectedItems</c> parameter — every other fact in this file starts from an empty list.
    /// </summary>
    [Fact]
    public async Task Reopening_With_A_Prior_Selection_Should_Preselect_It()
    {
        var (provider, reference) = await ShowDialog(preSelected: [Items[2]]);

        // The Select button is disabled until something is selected (Disabled="@(!_selectedItemsSet.Any())");
        // a pre-seeded selection must already have it enabled, with no row click at all.
        DialogTestHelpers.Button(provider, "Select").GetAttribute("disabled").ShouldBeNull();

        await provider.InvokeAsync(() => DialogTestHelpers.Button(provider, "Select").ClickAsync(new()));

        var result = await reference.Result;
        ResolvedWith(result!, Items[2]).ShouldBeTrue();
    }

    private static bool ResolvedWith(DialogResult result, LovCustomer expected)
    {
        var items = ItemsOf(result);
        return !result.Canceled && items.Count == 1 && items[0] == expected;
    }

    private static int SelectionCount(DialogResult result) => ItemsOf(result).Count;

    private static List<LovCustomer> ItemsOf(DialogResult result) =>
        (result.Data as LovSelectionResult<LovCustomer>)?.SelectedItems ?? [];

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowDialog(
        bool multiSelect = false,
        List<LovCustomer>? preSelected = null)
    {
        var formConfig = FormBuilder<LovModel>.Create()
            .AddField(x => x.CustomerId, field => field
                .WithLabel("Customer")
                .AsLov<LovModel, int?, LovCustomer>(lov =>
                {
                    lov.WithKey(c => (int?)c.Id)
                        .WithDisplay(c => c.Name)
                        .WithDataSource(() => Items)
                        .AddColumn(c => c.Name, "Name")
                        .WithSearchDebounce(0);

                    if (multiSelect)
                    {
                        lov.AllowMultipleSelection();
                    }
                }))
            .Build();

        var lovConfig = (ILovConfiguration<LovCustomer, int?>)formConfig.Fields[0]
            .AdditionalAttributes["LovConfiguration"];

        var dataProvider = LambdaLovDataProvider<LovCustomer>.FromCollection(
            () => Items,
            c => c.Id,
            (item, search) => item.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters<LovSelectionDialog<LovCustomer, int?>>
        {
            { x => x.LovConfig, lovConfig },
            { x => x.DataProvider, dataProvider },
            { x => x.SelectedItems, preSelected ?? [] },
            { x => x.ServiceProvider, Services },
        };

        IDialogReference reference = null!;
        await provider.InvokeAsync(async () =>
            reference = await dialogService.ShowAsync<LovSelectionDialog<LovCustomer, int?>>(
                "Select", parameters));

        return (provider, reference);
    }

    private class LovModel
    {
        public int? CustomerId { get; set; }
    }

    private record LovCustomer(int Id, string Name);
}
