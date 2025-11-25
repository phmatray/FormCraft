using Microsoft.AspNetCore.Components;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class GuidelinesTable<TItem>
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    [Parameter, EditorRequired]
    public List<string> Columns { get; set; } = [];

    [Parameter, EditorRequired]
    public List<TItem> Items { get; set; } = [];

    [Parameter, EditorRequired]
    public RenderFragment<TItem> RowTemplate { get; set; } = null!;

    [Parameter]
    public RenderFragment? AdditionalContent { get; set; }
}