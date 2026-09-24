namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Interaction coverage for <see cref="MudBlazorAutocompleteFieldComponent{TModel,TValue}"/>
/// (issue #461, AC3) — only 28% merged-line covered before this file, and only through
/// <see cref="FieldConfigurationParityTests.AutocompleteField_Row"/>'s config-swap guard, which
/// never drives a search or a selection. Asserted through the rendered
/// <see cref="MudAutocomplete{T}"/> instance rather than its popover DOM, per the issue's own
/// guidance: bUnit's typed component API already reaches <c>SearchFunc</c>/<c>ValueChanged</c>
/// without needing a live popup portal.
/// </summary>
public class MudBlazorAutocompleteFieldComponentTests : MudBlazorTestBase
{
    private static readonly SelectOption<string>[] Cities =
    [
        new("ny", "New York"),
        new("la", "Los Angeles"),
        new("chi", "Chicago"),
    ];

    [Fact]
    public async Task Typing_Should_Narrow_The_Candidates()
    {
        var component = RenderAutocomplete();

        var results = await Search(component, "New");

        results.ShouldBe(["ny"]);
    }

    [Fact]
    public async Task An_Empty_Search_Should_Return_Every_Candidate()
    {
        var component = RenderAutocomplete();

        var results = await Search(component, string.Empty);

        results.ShouldBe(["ny", "la", "chi"]);
    }

    [Fact]
    public async Task Selecting_A_Candidate_Should_Write_The_Model()
    {
        var model = new AutocompleteModel();
        var component = RenderAutocomplete(model);
        var autocomplete = component.FindComponent<MudAutocomplete<string>>();

        await autocomplete.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("la"));

        model.City.ShouldBe("la");
    }

    [Fact]
    public async Task Clearing_The_Value_Should_Clear_The_Model()
    {
        var model = new AutocompleteModel { City = "chi" };
        var component = RenderAutocomplete(model);
        var autocomplete = component.FindComponent<MudAutocomplete<string>>();

        await autocomplete.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync(null));

        model.City.ShouldBeNull();
    }

    private static Task<IEnumerable<string>> Search(
        IRenderedComponent<FormCraftComponent<AutocompleteModel>> component,
        string text)
    {
        var searchFunc = component.FindComponent<MudAutocomplete<string>>().Instance.SearchFunc
            ?? throw new InvalidOperationException("SearchFunc was not bound.");
        return searchFunc(text, CancellationToken.None)!;
    }

    private IRenderedComponent<FormCraftComponent<AutocompleteModel>> RenderAutocomplete(
        AutocompleteModel? model = null)
    {
        var config = FormBuilder<AutocompleteModel>.Create()
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .AsAutocomplete(
                    searchFunc: (text, _) => Task.FromResult(Cities
                        .Where(c => string.IsNullOrEmpty(text)
                                    || c.Label.Contains(text, StringComparison.OrdinalIgnoreCase))
                        .AsEnumerable()),
                    debounceMs: 0))
            .Build();

        return Render<FormCraftComponent<AutocompleteModel>>(parameters => parameters
            .Add(p => p.Model, model ?? new AutocompleteModel())
            .Add(p => p.Configuration, config));
    }

    private class AutocompleteModel
    {
        public string? City { get; set; }
    }
}
