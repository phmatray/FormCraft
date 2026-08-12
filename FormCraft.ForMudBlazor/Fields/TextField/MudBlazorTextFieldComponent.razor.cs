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

    /// <summary>
    /// Whether <see cref="Mask"/> strips its literals out of the value the model receives (#265).
    /// </summary>
    public bool MaskCleanDelimiters { get; set; }

    /// <summary>
    /// A caller-supplied MudBlazor mask, which takes precedence over <see cref="Mask"/> (#265).
    /// </summary>
    public IMask? SuppliedMask { get; set; }

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
        Mask = GetAttribute<string?>(TextMaskMap.AttributeName);
        MaskCleanDelimiters = GetAttribute(TextMaskMap.CleanDelimitersAttribute, false);
        SuppliedMask = GetAttribute<IMask?>(TextMaskMap.MaskInstanceAttribute);

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

    /// <summary>
    /// The mask this field renders with, or <c>null</c> when it configured none (#211).
    /// </summary>
    /// <remarks>
    /// Until #211 this was a stub that returned <c>null</c> under a "For now" comment, and the
    /// <c>.razor</c> never bound it — so it was unreachable as well as unimplemented, and
    /// <c>.WithAttribute("Mask", …)</c> looked supported while doing nothing. The resolution itself
    /// lives in <see cref="TextMaskMap"/> so the collection render path cannot drift from it.
    /// </remarks>
    private IMask? GetMask() => TextMaskMap.Resolve(Mask, MaskCleanDelimiters, SuppliedMask);

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

/// <summary>
/// The single implementation of FormCraft's mask-string to <see cref="IMask"/> mapping, shared by the
/// component render path (<see cref="MudBlazorTextFieldComponent{TModel}"/>) and the imperative
/// RenderTreeBuilder path used for collection item fields.
/// </summary>
/// <remarks>
/// <para>
/// Added in #211. Before it, <c>.WithAttribute("Mask", …)</c> was silently inert on both paths:
/// the component read the string into a property whose only consumer was a <c>GetMask()</c> stub that
/// returned <c>null</c> and that nothing called, and the collection path deliberately did not forward
/// it at all. Masks land through here so the two paths cannot answer the question differently — the
/// same reason #189 moved the input-type mapping into <see cref="TextInputTypeMap"/>.
/// </para>
/// </remarks>
internal static class TextMaskMap
{
    /// <summary>
    /// The attribute key a field configures a mask under: <c>.WithAttribute("Mask", "0000-0000")</c>.
    /// </summary>
    /// <remarks>
    /// Named rather than spelled twice. Both render paths read this attribute, and extracting the
    /// interpretation into <see cref="Resolve"/> while leaving the KEY as a literal in two files
    /// would still allow them to drift: a typo in either one disables masking on that path alone,
    /// which is the divergence class #211 exists to close. Mirrors
    /// <c>MudBlazorFieldBuilderExtensions.AdornmentClickAttribute</c>; it lives here rather than
    /// there because no builder method writes this one — callers pass the string themselves.
    /// </remarks>
    internal const string AttributeName = "Mask";

    /// <summary>
    /// The attribute key <c>MudBlazorFieldBuilderExtensions.WithMask</c> stores its
    /// <c>cleanDelimiters</c> argument under, and which both render paths read it back from (#265).
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="AttributeName"/> this key is written by a builder method rather than spelled
    /// by the caller, and deliberately so: the issue considered exposing a second magic string
    /// alongside <c>"Mask"</c> and rejected it, since two undiscoverable keys to spell correctly is
    /// the situation #204 was filed about. It lives here rather than beside the builder because
    /// <see cref="Resolve"/> is its only reader.
    /// </remarks>
    internal const string CleanDelimitersAttribute = "MaskCleanDelimiters";

    /// <summary>
    /// The attribute key <c>MudBlazorFieldBuilderExtensions.WithMask</c> stores a caller-supplied
    /// <see cref="IMask"/> under, and which both render paths read it back from (#265).
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="AttributeName"/> rather than sharing it, because that key is typed
    /// <c>string?</c> at both readers and an <see cref="IMask"/> written to it fails their
    /// <c>value is T</c> test and reads back as <c>null</c> — which is exactly how
    /// <c>.WithAttribute("Mask", new RegexMask(…))</c> came to compile, render and do nothing.
    /// Widening that key to <c>object?</c> instead would make every existing reader re-test the
    /// type; a second key keeps each one monomorphic.
    /// </remarks>
    internal const string MaskInstanceAttribute = "MaskInstance";

    /// <summary>
    /// Maps a configured mask pattern onto MudBlazor's <see cref="IMask"/>, or <c>null</c> when the
    /// field configured none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="PatternMask"/> is the implementation because its constructor takes exactly what
    /// FormCraft stores — a pattern string — so nothing has to be invented to bridge the two. Its
    /// pattern characters are <c>0</c> (digit), <c>a</c> (letter) and <c>*</c> (letter or digit);
    /// every other character is a literal that the mask inserts as the user types.
    /// </para>
    /// <para>
    /// A field that configures no mask must resolve to <c>null</c>, not to an empty
    /// <c>PatternMask("")</c>. <c>MudTextField</c> renders a <c>MudMask</c> instead of its usual
    /// input as soon as <c>Mask</c> is non-null, and that swap also makes it ignore <c>MaxLines</c>
    /// and <c>Sizing</c> — so an empty-but-present mask would quietly reroute every unmasked text
    /// field in the library through a different component.
    /// </para>
    /// <para>
    /// The blank test is <see cref="string.IsNullOrWhiteSpace"/> rather than
    /// <see cref="string.IsNullOrEmpty"/>, because a whitespace-only pattern is the same mistake
    /// wearing a different hat and has a worse outcome. <c>" "</c> — easy to arrive at from
    /// configuration binding or a trimmed-to-blank setting — is not "no mask": it is a mask whose
    /// single position is a literal space, so the field takes the <c>MudMask</c> path AND accepts no
    /// input at all. Treating it as unconfigured is the only reading that leaves the field usable.
    /// </para>
    /// <para>
    /// Returns a fresh instance per call, deliberately. A mask is not a value: <see cref="BaseMask"/>
    /// carries the live <c>Text</c>, <c>CaretPos</c> and <c>Selection</c> of the input it is attached
    /// to, so one cached instance shared between two fields — or between two rows of the same
    /// collection — would have them overwrite each other's editing state.
    /// </para>
    /// <para>
    /// Fresh-per-render is what MudBlazor expects in return, and it is safe for a specific reason
    /// worth stating, because a future change here could break it silently. <c>MudMask.SetMask</c>
    /// keeps the instance it already owns and copies into it — <c>_mask.UpdateFrom(other)</c>,
    /// preserving the user's text and caret — but <b>only when the supplied mask is of the same
    /// type</b>; otherwise it adopts the new instance outright. Since a render happens on every
    /// keystroke (<c>Immediate="true"</c>), a resolver that returned different <see cref="IMask"/>
    /// implementations for different patterns would swap the mask out mid-edit. Always returning
    /// <see cref="PatternMask"/> is therefore load-bearing, not incidental, and is pinned across
    /// several patterns by <c>RenderPipelineParityTests.Mask_Should_Resolve_Identically_On_Both_Paths</c>.
    /// </para>
    /// </remarks>
    /// <param name="mask">The configured pattern, or <c>null</c>/blank for no mask.</param>
    /// <param name="cleanDelimiters">
    /// Whether the resolved <see cref="PatternMask"/> strips the pattern's literals out of the value
    /// it reports (#265). Meaningless without a pattern, and ignored there rather than allowed to
    /// resurrect a mask the blank rule above suppressed.
    /// </param>
    /// <param name="suppliedMask">
    /// <para>
    /// A caller-supplied <see cref="IMask"/> (#265), which wins over <paramref name="mask"/> when
    /// both are configured: it is the more specific instruction, and a deterministic rule is what
    /// stops the answer depending on the order the two attributes happen to be read in.
    /// </para>
    /// <para>
    /// Returned as-is rather than copied, which is the one place this method does not hand out a
    /// fresh instance per call. That is deliberate but narrow: MudBlazor treats the value as a
    /// <b>prototype</b> it reads settings out of, so the caller must not mutate it after
    /// configuring the field. Type stability is preserved — the same instance is by definition the
    /// same type on every render — which is the property <c>MudMask.SetMask</c> actually tests.
    /// </para>
    /// </param>
    internal static IMask? Resolve(string? mask, bool cleanDelimiters = false, IMask? suppliedMask = null)
    {
        if (suppliedMask is not null)
        {
            return suppliedMask;
        }

        return string.IsNullOrWhiteSpace(mask)
            ? null
            : new PatternMask(mask) { CleanDelimiters = cleanDelimiters };
    }
}
