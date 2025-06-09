namespace FormCraft.ForMudBlazor;

public partial class MudBlazorRatingComponent<TModel>
{
    private int MaxValue { get; set; } = 5;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        MaxValue = GetAttribute<int>("MaxRating", 5);
    }
}