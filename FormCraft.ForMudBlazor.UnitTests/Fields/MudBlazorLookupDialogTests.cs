using AngleSharp.Dom;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Direct-render coverage for <see cref="MudBlazorLookupDialog"/> — the dialog's own
/// search/select/cancel behaviour, rendered through a real <see cref="MudDialogProvider"/> and
/// <see cref="IDialogService"/> round trip (mirrors <see cref="LovSelectionDialogTests"/>).
/// <see cref="FieldConfigurationParityTests.StubDialogServiceReturning{TDialog}"/> already covers
/// the outer field's reaction to a canned result; this file covers the dialog's own markup.
/// </summary>
public class MudBlazorLookupDialogTests : MudBlazorTestBase
{
    private static readonly List<LookupCity> Items =
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
        await provider.InvokeAsync(() => Task.Delay(350));

        RowCount(provider).ShouldBe(1);
    }

    [Fact]
    public async Task Searching_For_Nothing_Should_Show_The_No_Records_Fallback()
    {
        var (provider, _) = await ShowDialog();

        await provider.InvokeAsync(() => provider.Find("input").Input("nobody"));
        await provider.InvokeAsync(() => Task.Delay(350));

        RowCount(provider).ShouldBe(0);
        provider.Markup.ShouldContain("No matching records found.");
    }

    [Fact]
    public async Task Choosing_A_Row_Then_Select_Should_Resolve_With_That_Item()
    {
        var (provider, reference) = await ShowDialog();

        await provider.InvokeAsync(() => Row(provider, "Bob").ClickAsync(new()));
        await provider.InvokeAsync(() => SelectButton(provider).ClickAsync(new()));

        var result = await reference.Result;
        result!.Canceled.ShouldBeFalse();
        (result.Data as LookupCity).ShouldBe(Items[1]);
    }

    [Fact]
    public async Task Cancel_Should_Resolve_With_A_Cancelled_Result()
    {
        var (provider, reference) = await ShowDialog();

        await provider.InvokeAsync(() => CancelButton(provider).ClickAsync(new()));

        var result = await reference.Result;
        result!.Canceled.ShouldBeTrue();
    }

    private static int RowCount(IRenderedComponent<MudDialogProvider> provider) =>
        provider.FindAll("tbody tr.mud-table-row").Count;

    private static IElement Row(IRenderedComponent<MudDialogProvider> provider, string displayText) =>
        provider.FindAll("tbody tr.mud-table-row").First(r => r.TextContent.Contains(displayText));

    private static IElement SelectButton(IRenderedComponent<MudDialogProvider> provider) =>
        provider.FindAll("button").First(b => b.TextContent.Trim() == "Select");

    private static IElement CancelButton(IRenderedComponent<MudDialogProvider> provider) =>
        provider.FindAll("button").First(b => b.TextContent.Trim() == "Cancel");

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowDialog()
    {
        Func<LookupQuery, Task<LookupResult<LookupCity>>> dataProvider = query =>
        {
            var items = Items.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                items = items.Where(c => c.Name.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            var list = items.ToList();
            return Task.FromResult(new LookupResult<LookupCity> { Items = list, TotalCount = list.Count });
        };

        Func<LookupCity, int> valueSelector = c => c.Id;
        Func<LookupCity, string> displaySelector = c => c.Name;
        var columns = new List<LookupColumn<LookupCity>>
        {
            new() { Title = "Name", ValueSelector = item => ((LookupCity)item).Name },
        };

        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters<MudBlazorLookupDialog>
        {
            { x => x.DataProvider, (object)dataProvider },
            { x => x.ValueSelector, (object)valueSelector },
            { x => x.DisplaySelector, (object)displaySelector },
            { x => x.Columns, (object)columns },
            { x => x.FieldLabel, "City" },
        };

        IDialogReference reference = null!;
        await provider.InvokeAsync(async () =>
            reference = await dialogService.ShowAsync<MudBlazorLookupDialog>("Select City", parameters));

        return (provider, reference);
    }

    private record LookupCity(int Id, string Name);
}
