using AngleSharp.Dom;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Direct-render coverage for <see cref="LovSelectionDialog{TItem,TValue}"/> — the dialog's own
/// search/select/cancel/multi-select behaviour, rendered on its own rather than only observed
/// through the outer field's stubbed <c>IDialogService</c>
/// (<see cref="FieldConfigurationParityTests.StubDialogServiceReturning{TDialog}"/> covers that
/// outer reaction already). A fake <see cref="IMudDialogInstance"/> is cascaded in directly, the
/// same shape <see cref="MudDialogProvider"/> itself cascades to a real dialog, so no
/// <c>MudDialogProvider</c>/<c>IDialogService</c> round trip is needed to exercise the dialog's own
/// markup (issue #461's own Assumptions section allows either mechanism).
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

        RowCount(provider).ShouldBe(3);
    }

    [Fact]
    public async Task Searching_Should_Narrow_The_Rendered_Rows()
    {
        var (provider, _) = await ShowDialog();

        await provider.InvokeAsync(() => provider.Find("input").Input("ali"));

        RowCount(provider).ShouldBe(1);
    }

    [Fact]
    public async Task Choosing_A_Row_Then_Select_Should_Resolve_With_That_Item()
    {
        var (provider, reference) = await ShowDialog();

        var grid = provider.FindComponent<MudDataGrid<LovCustomer>>();
        await provider.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(
            new HashSet<LovCustomer> { Items[1] }));

        await provider.InvokeAsync(() => SelectButton(provider).ClickAsync(new()));

        var result = await reference.Result;
        ResolvedWith(result!, Items[1]).ShouldBeTrue();
    }

    [Fact]
    public async Task Cancel_Should_Resolve_With_A_Cancelled_Result()
    {
        var (provider, reference) = await ShowDialog();

        await provider.InvokeAsync(() => CancelButton(provider).ClickAsync(new()));

        var result = await reference.Result;
        result!.Canceled.ShouldBeTrue();
    }

    [Fact]
    public async Task MultiSelect_Should_Allow_Choosing_More_Than_One_Row()
    {
        var (provider, reference) = await ShowDialog(multiSelect: true);

        var grid = provider.FindComponent<MudDataGrid<LovCustomer>>();
        grid.Instance.MultiSelection.ShouldBeTrue();

        await provider.InvokeAsync(() => grid.Instance.SelectedItemsChanged.InvokeAsync(
            new HashSet<LovCustomer> { Items[0], Items[2] }));

        await provider.InvokeAsync(() => SelectButton(provider).ClickAsync(new()));

        var result = await reference.Result;
        SelectionCount(result!).ShouldBe(2);
    }

    private static bool ResolvedWith(DialogResult result, LovCustomer expected)
    {
        var items = ItemsOf(result);
        return !result.Canceled && items.Count == 1 && items[0] == expected;
    }

    private static int SelectionCount(DialogResult result) => ItemsOf(result).Count;

    private static List<LovCustomer> ItemsOf(DialogResult result) =>
        (result.Data as LovSelectionResult<LovCustomer>)?.SelectedItems ?? [];

    private static int RowCount(IRenderedComponent<MudDialogProvider> provider) =>
        provider.FindAll("tbody tr.mud-table-row").Count;

    private static IElement SelectButton(IRenderedComponent<MudDialogProvider> provider) =>
        provider.FindAll("button").First(b => b.TextContent.Contains("Select"));

    private static IElement CancelButton(IRenderedComponent<MudDialogProvider> provider) =>
        provider.FindAll("button").First(b => b.TextContent.Trim() == "Cancel");

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowDialog(
        bool multiSelect = false)
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
            { x => x.SelectedItems, new List<LovCustomer>() },
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
