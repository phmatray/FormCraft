namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a <see cref="DateTime"/> field as a Fluent UI date picker.
/// </summary>
/// <remarks>
/// The bound value type is non-nullable <see cref="DateTime"/> while the picker itself is closed
/// over <c>DateTime?</c>: an unset date must show an empty picker rather than year 1, so
/// <c>default</c> is mapped to null on the way in and back on the way out. This mirrors how the
/// MudBlazor adapter treats the same field type.
/// </remarks>
public partial class FluentUIDateTimeFieldComponent<TModel>
{
    private DateTime? _localValue;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _localValue = ToNullable(CurrentValue);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        var current = ToNullable(CurrentValue);
        if (current != _localValue)
        {
            _localValue = current;
        }
    }

    private static DateTime? ToNullable(DateTime value) => value == default ? null : value;

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue ?? default);

        // A cleared picker on a nullable property round-trips as null, not default (#150).
        object? notified = _localValue.HasValue
            ? _localValue.Value
            : IsNullableField ? null : default(DateTime);

        await Context.OnValueChanged.InvokeAsync(notified);
    }
}
