using FormCraft.ForMudBlazor.UnitTests.TestSupport;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Coverage for <see cref="MudBlazorLovFieldComponent{TModel,TValue,TKey,TItem}"/>'s multi-select
/// path, driven end to end through <c>.AsMultiSelectLov(...)</c> and the component's own dialog
/// selection flow: one chip per selected item (#461), every selected key written to the model's
/// <c>IEnumerable&lt;TKey&gt;</c> property (#480), and closing a chip removing only that item (#475).
/// </summary>
/// <remarks>
/// Before #480 the renderer closed the component's <c>TValue</c> over the configuration's scalar key
/// type, so selecting rows in an <c>AsMultiSelectLov</c> field threw <see cref="InvalidCastException"/>
/// — which is why #461 had to reach this path through an <c>object</c>-typed <c>AsLov</c> field instead.
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
    public async Task Selecting_Multiple_Items_Should_Render_One_Chip_Each_And_Write_Every_Key()
    {
        var model = new LovModel();
        var component = RenderSelecting(model, Customers[0], Customers[1]);

        await component.Find("button").ClickAsync(new());

        component.FindComponents<MudChip<LovCustomer>>().Count.ShouldBe(2);
        model.CustomerIds.ShouldBe([1, 2]);
    }

    [Fact]
    public async Task Closing_A_Chip_Should_Remove_Only_That_Selection()
    {
        var model = new LovModel();
        var component = RenderSelecting(model, Customers[0], Customers[1]);
        await component.Find("button").ClickAsync(new());

        await CloseChipAsync(component, 0);

        var chips = component.FindComponents<MudChip<LovCustomer>>();
        chips.Count.ShouldBe(1);
        chips[0].Instance.Value.ShouldBe(Customers[1]);
        model.CustomerIds.ShouldBe([2]);
    }

    [Fact]
    public async Task Closing_The_Last_Chip_Should_Empty_The_Selection()
    {
        var model = new LovModel();
        var component = RenderSelecting(model, Customers[0]);
        await component.Find("button").ClickAsync(new());

        await CloseChipAsync(component, 0);

        component.FindComponents<MudChip<LovCustomer>>().ShouldBeEmpty();
        model.CustomerIds.ShouldBeEmpty();
    }

    private IRenderedComponent<FormCraftComponent<LovModel>> RenderSelecting(LovModel model, params LovCustomer[] selected)
    {
        Services.AddSingleton(StubDialogService.Returning<LovSelectionDialog<LovCustomer, int>>(
            new LovSelectionResult<LovCustomer> { SelectedItems = [.. selected] }));

        return Render<FormCraftComponent<LovModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, LovConfig()));
    }

    private static async Task CloseChipAsync(IRenderedComponent<FormCraftComponent<LovModel>> component, int index)
    {
        var chipSet = component.FindComponent<MudChipSet<LovCustomer>>();
        var chip = component.FindComponents<MudChip<LovCustomer>>()[index].Instance;
        await component.InvokeAsync(() => chipSet.Instance.OnClose.InvokeAsync(chip));
    }

    private static IFormConfiguration<LovModel> LovConfig() =>
        FormBuilder<LovModel>.Create()
            .AddField(x => x.CustomerIds, field => field
                .WithLabel("Customers")
                .AsMultiSelectLov<LovModel, int, LovCustomer>(lov => lov
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)
                    .WithDataSource(() => Customers)
                    .AddColumn(c => c.Name, "Name")))
            .Build();

    private class LovModel
    {
        public IEnumerable<int> CustomerIds { get; set; } = [];
    }

    private record LovCustomer(int Id, string Name);
}
