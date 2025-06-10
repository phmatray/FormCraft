namespace FormCraft.ForMudBlazor;

public partial class MudBlazorColorPickerComponent<TModel>
{
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Ensure we have a default value
        if (string.IsNullOrEmpty(CurrentValue))
        {
            CurrentValue = "#000000";
        }
    }
}