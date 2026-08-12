namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a <see cref="DateOnly"/> field as a Fluent UI date picker.
/// </summary>
/// <remarks>
/// Unlike the MudBlazor adapter, which has to convert <see cref="DateOnly"/> to
/// <see cref="DateTime"/> because <c>MudDatePicker</c> only speaks the latter, Fluent's picker is
/// generic and binds <see cref="DateOnly"/> directly. Only the unset-is-null mapping remains.
/// </remarks>
public partial class FluentUIDateOnlyFieldComponent<TModel>
{
    private DateOnly? _localValue;

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

    private static DateOnly? ToNullable(DateOnly value) => value == default ? null : value;

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue ?? default);

        // A cleared picker on a nullable property round-trips as null, not default (#150).
        object? notified = _localValue.HasValue
            ? _localValue.Value
            : IsNullableField ? null : default(DateOnly);

        await Context.OnValueChanged.InvokeAsync(notified);
    }
}
