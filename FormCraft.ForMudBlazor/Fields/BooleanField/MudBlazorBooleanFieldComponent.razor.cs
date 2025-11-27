namespace FormCraft.ForMudBlazor;

public partial class MudBlazorBooleanFieldComponent<TModel>
{
    public BooleanDisplayStyle DisplayStyle { get; set; } = BooleanDisplayStyle.Switch;
    public string? TrueText { get; set; }
    public string? FalseText { get; set; }
    public bool AllowIndeterminate { get; set; }

    private async Task HandleValueChanged(bool value)
    {
        // Update local state FIRST to prevent race condition during parent re-render
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }
}