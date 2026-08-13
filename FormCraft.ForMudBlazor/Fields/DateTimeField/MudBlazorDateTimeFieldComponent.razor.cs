using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorDateTimeFieldComponent<TModel>
{
    private DateTime? _localValue;

    /// <summary>
    /// The adornment this picker renders: the configured one, otherwise MudDatePicker's own
    /// <see cref="MudBlazor.Adornment.End"/> (#217).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component deliberately took no part in the #184 adornment forward, because MudDatePicker
    /// — unlike MudTextField and MudNumericField — defaults to an End adornment carrying a calendar
    /// icon, and binding an unset <see cref="MudBlazorFieldComponentBase{TModel, TValue}.EffectiveAdornment"/>
    /// (which falls back to <see cref="MudBlazor.Adornment.None"/>) would have erased that icon from
    /// every date field.
    /// </para>
    /// <para>
    /// The cost of abstaining was that <c>.WithAdornment(...)</c> on a date field was accepted and
    /// silently dropped — the same class of silent discard #184, #191 and #192 each closed
    /// elsewhere. Supplying MudDatePicker's own defaults resolves both at once: an unconfigured
    /// field still renders End + the calendar icon, and a configured adornment now wins.
    /// </para>
    /// <para>
    /// #217 fixed exactly this on the imperative collection path. #203 converged the two paths onto
    /// this component, so the fix has to live here or date item fields would silently lose it.
    /// </para>
    /// </remarks>
    private Adornment DateAdornment => GetAttribute<Adornment?>("Adornment") ?? Adornment.End;

    /// <summary>
    /// The icon for <see cref="DateAdornment"/>: the configured one, otherwise MudDatePicker's own
    /// calendar icon.
    /// </summary>
    private string? DateAdornmentIcon =>
        GetAttribute<string?>("AdornmentIcon") ?? Icons.Material.Filled.Event;

    /// <summary>
    /// This component binds an adornment, so the ShrinkLabel diagnostic judges the value it
    /// actually renders (#212) rather than staying silent.
    /// </summary>
    /// <remarks>
    /// An unconfigured field reports <see cref="MudBlazor.Adornment.End"/>, which never conflicts
    /// with a floating label; only a deliberately configured Start adornment warns.
    /// </remarks>
    protected override Adornment? RenderedAdornment => DateAdornment;

    public DateTimeInputMode InputMode { get; set; } = DateTimeInputMode.Date;
    public string? Format { get; set; } = "yyyy-MM-dd";
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool ShowClearButton { get; set; } = true;
    public bool OpenOnFocus { get; set; } = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value
        _localValue = CurrentValue == default ? null : CurrentValue;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#298).
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        // Load configuration from additional attributes
        InputMode = GetAttribute("InputMode", DateTimeInputMode.Date);
        Format = GetAttribute("Format", "yyyy-MM-dd");
        MinDate = GetAttribute<DateTime?>("MinDate");
        MaxDate = GetAttribute<DateTime?>("MaxDate");
        ShowClearButton = GetAttribute("ShowClearButton", true);
        OpenOnFocus = GetAttribute("OpenOnFocus", true);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally
        var currentDateValue = CurrentValue == default ? null : (DateTime?)CurrentValue;
        if (currentDateValue != _localValue)
        {
            _localValue = currentDateValue;
        }
    }


    /// <summary>
    /// Whether the bound model property is a nullable value type. Nullable fields
    /// round-trip a cleared picker as null instead of default (#150).
    /// </summary>
    private bool IsNullableField => Nullable.GetUnderlyingType(Context.ActualFieldType) != null;

    private async Task OnLocalValueChanged()
    {
        var value = _localValue ?? default;
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(IsNullableField ? _localValue : value);
    }
}
