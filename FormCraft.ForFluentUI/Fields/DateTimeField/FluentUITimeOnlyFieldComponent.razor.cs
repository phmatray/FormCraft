namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a <see cref="TimeOnly"/> field as a Fluent UI time picker.
/// </summary>
/// <remarks>
/// <c>default(TimeOnly)</c> is midnight, which is a legitimate value a user may want to pick, so
/// unlike the date components this one cannot treat <c>default</c> as "unset" - doing so would make
/// 00:00 unselectable. The trade is that an unset time field shows midnight rather than an empty
/// picker; a field that needs to distinguish the two should be declared <c>TimeOnly?</c>.
/// </remarks>
public partial class FluentUITimeOnlyFieldComponent<TModel>
{
    private TimeOnly? _localValue;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _localValue = CurrentValue;
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
        var value = _localValue ?? default;
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }
}
