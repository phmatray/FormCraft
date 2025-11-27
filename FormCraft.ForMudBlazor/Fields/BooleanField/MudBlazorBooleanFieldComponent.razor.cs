namespace FormCraft.ForMudBlazor;

public partial class MudBlazorBooleanFieldComponent<TModel>
{
    private bool _localValue;

    public BooleanDisplayStyle DisplayStyle { get; set; } = BooleanDisplayStyle.Switch;
    public string? TrueText { get; set; }
    public string? FalseText { get; set; }
    public bool AllowIndeterminate { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value (CurrentValue is bool, not bool? due to TValue? behavior)
        _localValue = CurrentValue is bool val ? val : false;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        var currentVal = CurrentValue is bool val ? val : false;
        if (currentVal != _localValue)
        {
            _localValue = currentVal;
        }
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}