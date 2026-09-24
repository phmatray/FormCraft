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
    public async Task Selecting_A_Candidate_Should_Write_The_Model()
    {
        var model = new AutocompleteModel();
        var component = RenderAutocomplete(model: model);
        var autocomplete = component.FindComponent<MudAutocomplete<string>>();

        await autocomplete.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync("la"));

        model.City.ShouldBe("la");
    }

    [Fact]
    public async Task Clearing_The_Value_Should_Clear_The_Model()
    {
        var model = new AutocompleteModel { City = "chi" };
        var component = RenderAutocomplete(model: model);
        var autocomplete = component.FindComponent<MudAutocomplete<string>>();

        await autocomplete.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync(null));

        model.City.ShouldBeNull();
    }

    /// <summary>
    /// The <c>IOptionProvider&lt;TModel,TValue&gt;</c> overload of <c>.AsAutocomplete(...)</c> takes
    /// a different path through <c>SearchAsync</c> than the <c>searchFunc</c> overload every other
    /// fact here uses — a reflection lookup of the provider's own <c>SearchAsync</c> method
    /// (<c>MudBlazorAutocompleteFieldComponent.razor.cs</c>'s <c>SearchAsync</c>, the
    /// <c>_optionProvider != null</c> branch) that falls back to an empty result on any lookup
    /// failure. Nothing else in this file reaches it (code review / verification-gap review, #461).
    /// </summary>
    [Fact]
    public async Task Autocomplete_With_An_Option_Provider_Should_Return_Its_Results()
    {
        var component = RenderAutocomplete(field => field
            .WithLabel("City")
            .AsAutocomplete(optionProvider: new StubOptionProvider()));

        var results = await Search(component, "Chi");

        results.ShouldBe(["chi"]);
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
        Action<FieldBuilder<AutocompleteModel, string>>? configure = null,
        AutocompleteModel? model = null)
    {
        configure ??= field => field
            .WithLabel("City")
            .AsAutocomplete(
                searchFunc: (text, _) => Task.FromResult(Cities
                    .Where(c => c.Label.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .AsEnumerable()));

        var config = FormBuilder<AutocompleteModel>.Create()
            .AddField(x => x.City, configure)
            .Build();

        return Render<FormCraftComponent<AutocompleteModel>>(parameters => parameters
            .Add(p => p.Model, model ?? new AutocompleteModel())
            .Add(p => p.Configuration, config));
    }

    private class AutocompleteModel
    {
        public string? City { get; set; }
    }

    private class StubOptionProvider : IOptionProvider<AutocompleteModel, string>
    {
        public Task<IEnumerable<SelectOption<string>>> SearchAsync(
            string searchText,
            AutocompleteModel model,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Cities.Where(c => c.Label.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .AsEnumerable());
    }
}
