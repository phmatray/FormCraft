namespace FormCraft.ForMudBlazor;

public partial class MudBlazorBooleanFieldComponent<TModel>
{
    private bool _localValue;

    /// <summary>
    /// Whether this field renders MudBlazor's native required decoration — the asterisk and the
    /// HTML5 <c>required</c> attribute. Set only by the explicit <c>.WithNativeRequired(...)</c>
    /// opt-in since #263; the announcement lives in <see cref="AriaRequired"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Declared here rather than inherited because this component derives from
    /// <c>FieldComponentBase</c> directly, not from <c>MudBlazorFieldComponentBase</c>. Rebasing it
    /// would drag in the variant cascade and the ShrinkLabel diagnostic, neither of which a checkbox
    /// has any use for.
    /// </para>
    /// <para>
    /// ⚠️ It carries the <b>same name and the same meaning</b> as the base's property on purpose.
    /// This pair was called <c>NativeRequiredValue</c>/<c>AriaRequiredValue</c> until #263 split the
    /// decoration from the announcement — at which point the old name would have meant "explicit
    /// opt-in only" here while <c>MudBlazorFileUploadComponentBase.NativeRequiredValue</c> went on
    /// meaning "is this field required", the full resolve. Two sibling components, one identifier,
    /// opposite answers, and nothing to catch a copy-paste between them.
    /// </para>
    /// </remarks>
    private bool EffectiveNativeRequired =>
        NativeRequired.Resolve(Context.Field.AdditionalAttributes, isRequired: false);

    /// <summary>
    /// <c>aria-required</c> as MudBlazor would spell it, for the <c>UserAttributes</c> splat below.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ Passed explicitly, and never <c>null</c>, because <c>MudCheckBox</c> and <c>MudSwitch</c>
    /// emit <b>no</b> <c>aria-required</c> of their own. Their <c>GetInputAttributes()</c> copies
    /// <c>UserAttributes</c> onto the rendered <c>&lt;input&gt;</c> and nothing downstream re-emits
    /// this key, so there is no fallback to leave in place — omitting it for an optional field would
    /// drop the attribute entirely rather than leave it <c>"false"</c>. The MudInput-backed field
    /// types can return <c>null</c> there; this one cannot.
    /// </para>
    /// <para>
    /// Resolved from <c>IsRequired</c> rather than from <see cref="EffectiveNativeRequired"/> since
    /// #263, which split the announcement from the native decoration. Reading the sibling property
    /// would silently re-couple them and leave a required checkbox unannounced the moment it stopped
    /// setting <c>Required</c>.
    /// </para>
    /// </remarks>
    private string AriaRequired =>
        NativeRequired.Resolve(Context.Field.AdditionalAttributes, IsRequired) ? "true" : "false";

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

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so an instance handed a different field re-reads it rather than
    /// rendering the previous field's settings (#298). Inherited from
    /// <c>FieldComponentBase</c> since #335 — this component derives from it directly, so before the
    /// hook moved into core it had to wire its own.
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        // Checkbox is the default (parity with the legacy render path); a
        // switch can be requested explicitly via the DisplayStyle attribute.
        DisplayStyle = GetAttribute("DisplayStyle", BooleanDisplayStyle.Checkbox);
        TrueText = GetAttribute<string?>("TrueText");
        FalseText = GetAttribute<string?>("FalseText");
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
