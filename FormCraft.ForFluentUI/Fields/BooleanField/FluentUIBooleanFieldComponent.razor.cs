namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a boolean field as a Fluent UI checkbox, or as a switch when the field asks for one.
/// </summary>
public partial class FluentUIBooleanFieldComponent<TModel>
{
    private bool _localValue;

    /// <summary>
    /// How the field is presented. Read from the <c>"DisplayStyle"</c> attribute, the same core
    /// <see cref="BooleanDisplayStyle"/> contract the MudBlazor adapter honours, so a form
    /// configuration selects a switch identically on either adapter.
    /// </summary>
    private BooleanDisplayStyle DisplayStyle { get; set; } = BooleanDisplayStyle.Checkbox;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _localValue = CurrentValue;

        // Checkbox is the default; a switch is opt-in, matching the MudBlazor adapter.
        DisplayStyle = GetAttribute("DisplayStyle", BooleanDisplayStyle.Checkbox);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (CurrentValue != _localValue)
        {
            _localValue = CurrentValue;
        }
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}
