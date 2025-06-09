namespace FormCraft.ForMudBlazor;

public partial class MudBlazorNumericFieldComponent<TModel, TValue>
{
    public TValue? Min { get; set; }
    public TValue? Max { get; set; }
    public TValue? Step { get; set; }
    public string? Format { get; set; }
    public bool ShowSpinButtons { get; set; } = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Min = GetAttribute<TValue?>("Min");
        Max = GetAttribute<TValue?>("Max");
        Step = GetAttribute<TValue?>("Step") ?? GetDefaultStep();
        Format = GetAttribute<string>("Format");
        ShowSpinButtons = GetAttribute("ShowSpinButtons", true);
    }

    private TValue GetDefaultStep()
    {
        // Default step values based on type
        if (typeof(TValue) == typeof(decimal) || typeof(TValue) == typeof(decimal?))
            return (TValue)(object)0.01m;
        if (typeof(TValue) == typeof(double) || typeof(TValue) == typeof(double?) ||
            typeof(TValue) == typeof(float) || typeof(TValue) == typeof(float?))
            return (TValue)(object)0.1;
        return (TValue)(object)1;
    }
}