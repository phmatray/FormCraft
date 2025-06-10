namespace FormCraft.ForMudBlazor;

public partial class MudBlazorSelectFieldComponent<TModel, TValue>
{
    public IEnumerable<SelectOption<TValue>> Options { get; set; } = new List<SelectOption<TValue>>();
    public bool AllowMultiple { get; set; }
    public bool IsSearchable { get; set; }
    public new string? Placeholder { get; set; }
    public bool ShowClearButton { get; set; }
    public int? MaxSelections { get; set; }
    public bool GroupOptions { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        var options = GetAttribute<IEnumerable<SelectOption<TValue>>>("Options");
        if (options != null)
        {
            Options = options;
        }
    }
}