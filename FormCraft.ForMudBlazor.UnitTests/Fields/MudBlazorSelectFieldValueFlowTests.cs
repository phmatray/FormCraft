namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Select-value write-flow coverage for <see cref="MudBlazorSelectFieldComponent{TModel,TValue}"/>
/// and <see cref="MudBlazorMultiSelectFieldComponent{TModel,TItem}"/> (issue #461, AC7). Before this
/// file, both were exercised only by
/// <see cref="FieldConfigurationParityTests.SelectField_Row"/>/<see cref="FieldConfigurationParityTests.MultiSelectField_Row"/>
/// — a config-swap-refresh guard that never chooses an option or writes the model.
/// </summary>
public class MudBlazorSelectFieldValueFlowTests : MudBlazorTestBase
{
    [Fact]
    public void Select_Should_Render_The_Configured_Options()
    {
        var component = RenderSelect();

        var options = component.FindComponents<MudSelectItem<string>>().Select(i => i.Instance.Value);
        options.ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task Choosing_An_Option_Should_Write_The_Model()
    {
        var model = new SelectModel();
        var component = RenderSelect(model);
        var select = component.FindComponent<MudSelect<string>>();

        await select.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync("b"));

        model.Value.ShouldBe("b");
    }

    [Fact]
    public async Task MultiSelect_Choosing_Two_Options_Should_Write_Both_Into_The_Models_Collection()
    {
        var model = new MultiSelectModel();
        var component = RenderMultiSelect(model);
        var select = component.FindComponent<MudSelect<string>>();

        await select.InvokeAsync(() => select.Instance.SelectedValuesChanged.InvokeAsync(["a", "b"]));

        model.Values.ShouldBe(["a", "b"], ignoreOrder: true);
    }

    [Fact]
    public async Task MultiSelect_Deselecting_One_Option_Should_Leave_The_Other()
    {
        var model = new MultiSelectModel();
        var component = RenderMultiSelect(model);
        var select = component.FindComponent<MudSelect<string>>();

        await select.InvokeAsync(() => select.Instance.SelectedValuesChanged.InvokeAsync(["a", "b"]));
        await select.InvokeAsync(() => select.Instance.SelectedValuesChanged.InvokeAsync(["b"]));

        model.Values.ShouldBe(["b"]);
    }

    private IRenderedComponent<FormCraftComponent<SelectModel>> RenderSelect(SelectModel? model = null)
    {
        var config = FormBuilder<SelectModel>.Create()
            .AddField(x => x.Value, field => field.WithLabel("Value").WithOptions(("a", "A"), ("b", "B")))
            .Build();

        return Render<FormCraftComponent<SelectModel>>(parameters => parameters
            .Add(p => p.Model, model ?? new SelectModel())
            .Add(p => p.Configuration, config));
    }

    private IRenderedComponent<FormCraftComponent<MultiSelectModel>> RenderMultiSelect(
        MultiSelectModel? model = null)
    {
        var config = FormBuilder<MultiSelectModel>.Create()
            .AddField(x => x.Values, field => field
                .WithLabel("Values")
                .AsMultiSelect(("a", "A"), ("b", "B"), ("c", "C")))
            .Build();

        return Render<FormCraftComponent<MultiSelectModel>>(parameters => parameters
            .Add(p => p.Model, model ?? new MultiSelectModel())
            .Add(p => p.Configuration, config));
    }

    private class SelectModel
    {
        public string Value { get; set; } = string.Empty;
    }

    private class MultiSelectModel
    {
        public IEnumerable<string>? Values { get; set; }
    }
}
