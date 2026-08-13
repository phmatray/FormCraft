namespace FormCraft.ForMudBlazor;

public partial class MudBlazorBooleanFieldComponent<TModel>
{
    private bool _localValue;

    /// <summary>
    /// Whether this field renders MudBlazor's native required decoration (#199), resolved by the
    /// same rule as every other field type — see <see cref="NativeRequired.Resolve"/>.
    /// </summary>
    /// <remarks>
    /// Declared here rather than inherited because this component derives from
    /// <c>FieldComponentBase</c> directly, not from <c>MudBlazorFieldComponentBase</c>, so it has no
    /// <c>EffectiveNativeRequired</c>. Rebasing it would drag in the variant cascade and the
    /// ShrinkLabel diagnostic, neither of which a checkbox has any use for.
    /// </remarks>
    private bool NativeRequiredValue =>
        NativeRequired.Resolve(Context.Field.AdditionalAttributes, IsRequired);

    /// <summary>
    /// <c>aria-required</c> as MudBlazor would spell it, for the <c>UserAttributes</c> splat below.
    /// </summary>
    /// <remarks>
    /// ⚠️ Passed explicitly because <c>MudCheckBox</c> and <c>MudSwitch</c> emit <b>no</b>
    /// <c>aria-required</c> of their own — unlike <c>MudInput</c>, whose own write overrides the
    /// caller's. Their <c>GetInputAttributes()</c> copies <c>UserAttributes</c> onto the rendered
    /// <c>&lt;input&gt;</c> and nothing downstream re-emits this key, so here the splat really does
    /// land. That asymmetry is the whole reason a checkbox can be announced correctly while a text
    /// field cannot (measured on MudBlazor 9.8.0).
    /// </remarks>
    private string AriaRequiredValue => NativeRequiredValue ? "true" : "false";

    public BooleanDisplayStyle DisplayStyle { get; set; } = BooleanDisplayStyle.Checkbox;
    public string? TrueText { get; set; }
    public string? FalseText { get; set; }
    public bool AllowIndeterminate { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value (CurrentValue is bool, not bool? due to TValue? behavior)
        _localValue = CurrentValue is bool val ? val : false;
    }

    /// <summary>
    /// Tracks which field this instance's cached properties were loaded from (#298).
    /// </summary>
    /// <remarks>
    /// Wired locally, for the same reason <c>NativeRequiredValue</c> above is: this component derives
    /// from <c>FieldComponentBase</c> directly, so it inherits neither the hook on
    /// <c>MudBlazorFieldComponentBase</c> nor the one on <c>MudBlazorFileUploadComponentBase</c>. Only
    /// the wiring repeats — <see cref="FieldConfigurationTracker"/> holds the rule and its reasoning.
    /// </remarks>
    private readonly FieldConfigurationTracker _fieldTracker = new();

    private void RefreshFieldConfigurationIfChanged()
    {
        if (!_fieldTracker.HasChanged(Context?.Field))
        {
            return;
        }

        // Moved off OnInitialized so an instance handed a different field re-reads it rather than
        // rendering the previous field's settings (#298).
        //
        // Checkbox is the default (parity with the legacy render path); a
        // switch can be requested explicitly via the DisplayStyle attribute.
        DisplayStyle = GetAttribute("DisplayStyle", BooleanDisplayStyle.Checkbox);
        TrueText = GetAttribute<string?>("TrueText");
        FalseText = GetAttribute<string?>("FalseText");
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        RefreshFieldConfigurationIfChanged();

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
