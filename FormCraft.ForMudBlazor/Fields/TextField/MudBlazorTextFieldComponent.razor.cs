using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorTextFieldComponent<TModel>
{
    private bool _passwordVisible;
    private string? _localValue;

    public int Lines { get; set; } = 1;

    /// <summary>
    /// The <c>Lines</c> the field asked for, before the password rule in
    /// <see cref="TextInputTypeMap.EffectiveLines"/> may have reduced it to 1 (#207). Kept so the
    /// diagnostic can report what was dropped rather than what was rendered.
    /// </summary>
    internal int ConfiguredLines { get; private set; } = 1;

    public int? MaxLength { get; set; }
    public string InputType { get; set; } = "text";
    public string? Autocomplete { get; set; }
    public string? Mask { get; set; }
    public Adornment? Adornment { get; set; }

    /// <summary>
    /// This component binds <see cref="Adornment"/>, so the ShrinkLabel diagnostic judges that
    /// resolved value (#212).
    /// </summary>
    /// <remarks>
    /// The resolved value, not the configured attribute: <c>.AsPassword()</c> with the visibility
    /// toggle installs a start-agnostic End adornment of its own, and a field that configured none
    /// still renders none. Judging the configured attribute would describe a different field.
    /// </remarks>
    protected override Adornment? RenderedAdornment => Adornment;
    public string? AdornmentIcon { get; set; }
    public Color AdornmentColor { get; set; } = Color.Default;
    public Action<string?>? OnAdornmentClick { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value from CurrentValue
        _localValue = CurrentValue;

        // Load configuration - prioritize field.InputType over AdditionalAttributes
        ConfiguredLines = GetAttribute("Lines", 1);
        MaxLength = GetAttribute<int?>("MaxLength");
        InputType = Context.Field.InputType ?? GetAttribute("InputType", "text") ?? "text";

        // A masked field is forced back to a single line: past Lines > 1 MudBlazor renders a
        // <textarea>, which has no `type` attribute and therefore cannot mask (#207). Resolved from
        // the CONFIGURED input type, so revealing a password with the toggle does not reflow the
        // field from one line to four.
        var configuredInputType = TextInputTypeMap.Resolve(InputType);
        Lines = TextInputTypeMap.EffectiveLines(configuredInputType, ConfiguredLines);

        // Emitted from OnInitialized, so once per component instance: the conflict is a
        // configuration fact and re-reporting it on every keystroke would drown the console.
        //
        // Once per component instance is not once per FIELD inside a collection, though — there is
        // one instance per row, so a 50-item collection would emit 50 identical warnings about a
        // single field's configuration. The scope's latch is what the hand-rolled collection path
        // used to do with a HashSet of its own before #203 routed item fields through here.
        if (MaskedLinesDiagnostic.Applies(configuredInputType, ConfiguredLines)
            && (ItemFieldScope?.ShouldWarnOnce(MaskedLinesDiagnostic.Category, DiagnosticFieldKey) ?? true))
        {
            MaskedLinesDiagnostic.Warn(
                DiagnosticServiceProvider,
                Context.Field.FieldName,
                Label,
                ConfiguredLines);
        }
        Autocomplete = GetAttribute<string?>("autocomplete");
        Mask = GetAttribute<string?>("Mask");

        // Load adornment configuration
        var customAdornment = GetAttribute<Adornment?>("Adornment");
        var customAdornmentIcon = GetAttribute<string?>("AdornmentIcon");
        var customAdornmentColor = GetAttribute("AdornmentColor", Color.Default);

        // Check for password visibility toggle
        var enablePasswordToggle = GetAttribute("EnablePasswordToggle", false);
        if (enablePasswordToggle && InputType.ToLowerInvariant() == "password")
        {
            // Password toggle always goes at the end
            Adornment = MudBlazor.Adornment.End;
            AdornmentIcon = Icons.Material.Filled.Visibility;
            AdornmentColor = Color.Default;
            OnAdornmentClick = TogglePasswordVisibility;

            // A field has one adornment slot and the toggle just took it, so anything configured for
            // that slot is discarded — including a click handler, which #192 made live everywhere
            // else. Say so rather than dropping it silently (#219). This branch is the only place
            // that knows something was displaced.
            var displacedHandler = GetAttribute<Action<string?>?>(
                MudBlazorFieldBuilderExtensions.AdornmentClickAttribute);

            // Latched per field for the same reason the masked-lines warning above is: one component
            // instance per row means an unlatched warning fires once per ROW. This one is newly
            // reachable from a collection since #203 — the hand-rolled path had no password toggle
            // at all, so it could never displace an adornment and never took this branch.
            if ((customAdornment.HasValue || customAdornmentIcon is not null || displacedHandler is not null)
                && (ItemFieldScope?.ShouldWarnOnce(PasswordAdornmentDiagnostic.Category, DiagnosticFieldKey) ?? true))
            {
                PasswordAdornmentDiagnostic.Warn(
                    DiagnosticServiceProvider,
                    Context.Field.FieldName,
                    Label);
            }
        }
        else if (customAdornment.HasValue)
        {
            // Use custom adornment if no password toggle
            Adornment = customAdornment;
            AdornmentIcon = customAdornmentIcon;
            AdornmentColor = customAdornmentColor;

            // Stays null when WithAdornment got no handler, which leaves HandleAdornmentClick a
            // no-op rather than a throw (#192). The password branch above deliberately keeps its
            // own toggle: it owns the adornment slot, so a configured handler must not displace it.
            OnAdornmentClick = GetAttribute<Action<string?>?>(
                MudBlazorFieldBuilderExtensions.AdornmentClickAttribute);
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally (e.g., form reset)
        // Only update if the value actually changed to avoid breaking user input
        if (CurrentValue != _localValue)
        {
            _localValue = CurrentValue;
        }
    }

    private InputType GetInputType()
    {
        // If password toggle is enabled and password is visible, show as text
        if (_passwordVisible && InputType.ToLowerInvariant() == "password")
        {
            return MudBlazor.InputType.Text;
        }

        return TextInputTypeMap.Resolve(InputType);
    }

    private IMask? GetMask()
    {
        if (string.IsNullOrEmpty(Mask))
            return null;

        // For now, return null. In a real implementation,
        // you would parse the mask string and create appropriate IMask
        return null;
    }

    private void TogglePasswordVisibility(string? value = null)
    {
        _passwordVisible = !_passwordVisible;
        AdornmentIcon = _passwordVisible ? Icons.Material.Filled.VisibilityOff : Icons.Material.Filled.Visibility;
        StateHasChanged();
    }

    /// <summary>
    /// The adornment click callback, or <c>default</c> when the field configured no handler (#216).
    /// </summary>
    /// <remarks>
    /// Binding <see cref="HandleAdornmentClick"/> directly would hand MudBlazor a method group, whose
    /// <c>EventCallback.HasDelegate</c> is always <c>true</c> — and MudBlazor draws a real
    /// <c>&lt;button&gt;</c> for that. A decorative icon then becomes a focus stop for keyboard and
    /// screen-reader users, even though clicking it does nothing. Returning <c>default</c> was how
    /// #216 closed the last markup divergence with the imperative collection path, which had always
    /// done it this way; that path is gone since #203, but the accessibility reason stands on its
    /// own and is pinned by
    /// <c>TextField_Adornment_Without_A_Handler_Should_Render_A_Plain_Icon</c>.
    /// </remarks>
    private EventCallback<MouseEventArgs> AdornmentClick =>
        OnAdornmentClick is null
            ? default
            : EventCallback.Factory.Create<MouseEventArgs>(this, HandleAdornmentClick);

    private void HandleAdornmentClick()
    {
        OnAdornmentClick?.Invoke(CurrentValue);
    }

    private async Task OnLocalValueChanged()
    {
        // Sync to base class
        SetValueWithoutNotification(_localValue);

        // Notify parent to update the model
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}

/// <summary>
/// The single implementation of FormCraft's input-type string to <see cref="InputType"/> mapping,
/// used by <see cref="MudBlazorTextFieldComponent{TModel}"/>.
/// </summary>
/// <remarks>
/// Extracted in #189, when there were two render paths and the collection one emitted no input type
/// at all — so a <c>.AsPassword()</c> field inside <c>.WithItemForm(...)</c> rendered its characters
/// in clear text. Duplicating the mapping to fix that would have set up the next divergence, so both
/// paths were pointed here instead.
/// <para>
/// #203 removed the second path entirely, so this is now simply where the text component resolves
/// its input type. It stays a separate type because <c>EffectiveLines</c> below encodes a rule
/// (masking beats a multi-line request, #207) that is worth stating and testing on its own.
/// </para>
/// </remarks>
internal static class TextInputTypeMap
{
    /// <summary>The input type a field renders with when it configures none.</summary>
    internal const string Default = "text";

    /// <summary>
    /// Maps a configured input-type string onto MudBlazor's enum, falling back to
    /// <see cref="InputType.Text"/> for null and for any value this library does not recognise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Recognised: <c>email</c>, <c>password</c>, <c>tel</c>/<c>telephone</c>, <c>url</c>,
    /// <c>search</c>, <c>number</c>, <c>date</c> and <c>time</c>. Anything else — including a typo —
    /// falls back to <see cref="InputType.Text"/> rather than throwing, which is the behaviour
    /// carried over from before #189.
    /// </para>
    /// <para>
    /// <c>number</c>, <c>date</c> and <c>time</c> were added in #210. They previously fell through to
    /// <see cref="InputType.Text"/>, so a field configured with one lost its mobile keypad or native
    /// picker with nothing reported. They were left out of #189 deliberately: that issue moved the
    /// mapping without changing what it renders, and widening the set is a behaviour change.
    /// </para>
    /// <para>
    /// Note on scope: <c>AutoFormBuilderExtensions</c> emits these same three strings, but for
    /// numeric, date and time *properties* — which are rendered by <c>MudNumericField</c>,
    /// <c>MudDatePicker</c> and <c>MudTimePicker</c>, none of which consult this map. It is reached
    /// only from the text path, i.e. <c>string</c> fields, so widening it does not change what an
    /// auto-generated form renders (pinned by a test).
    /// </para>
    /// </remarks>
    internal static InputType Resolve(string? inputType) =>
        (inputType ?? Default).ToLowerInvariant() switch
        {
            "email" => InputType.Email,
            "password" => InputType.Password,
            "tel" or "telephone" => InputType.Telephone,
            "url" => InputType.Url,
            "search" => InputType.Search,
            "number" => InputType.Number,
            "date" => InputType.Date,
            "time" => InputType.Time,
            _ => InputType.Text,
        };

    /// <summary>
    /// The number of lines a field actually renders with: always 1 for a masked field, otherwise
    /// whatever was configured.
    /// </summary>
    /// <remarks>
    /// #207. MudBlazor emits a <c>&lt;textarea&gt;</c> once <c>Lines &gt; 1</c>, and a textarea has
    /// no <c>type</c> attribute — so honouring <c>Lines</c> on a password field silently defeats the
    /// masking and renders the credential in clear text. That happened identically on both render
    /// paths, so it was a shared gap rather than a drift.
    /// <para>
    /// Masking wins because there is no such thing as a masked textarea: the combination can never
    /// be honoured as written, and of the two settings <c>.AsPassword()</c> is an explicit security
    /// request while <c>Lines</c> is presentation. Rejecting the combination at build time was the
    /// alternative, but <c>AsTextArea</c> lives in the core project and <c>AsPassword</c> here, so
    /// neither builder method ever sees both — the check would have to throw from
    /// <c>Build()</c>, turning an insecure-but-working form into a startup crash.
    /// </para>
    /// <para>
    /// Callers must pass the **configured** input type, not the one currently rendered: the
    /// visibility toggle flips a revealed password to <see cref="InputType.Text"/>, and a field
    /// that reflowed from one line to four on reveal would be a second surprise.
    /// </para>
    /// </remarks>
    internal static int EffectiveLines(InputType resolved, int configuredLines) =>
        resolved == InputType.Password ? 1 : configuredLines;
}
