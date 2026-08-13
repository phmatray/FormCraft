using Microsoft.JSInterop;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Shared base for the single- and multiple-file upload components, holding the required-marking
/// rule the two have to apply identically (#262).
/// </summary>
/// <remarks>
/// <para>
/// The two upload components drifting apart is the failure class this library keeps re-filing
/// (#146, #177, #184, #189), so the rule lives here once rather than as a copy in each component.
/// The <i>markup</i> lives once too — see <c>FileUploadRequiredMarker</c> and
/// <c>FileUploadRequiredHint</c> — because centralising only the values still leaves two copies of
/// the thing that can drift.
/// </para>
/// <para>
/// <b>Why not the hidden input.</b> Both components render <c>MudFileUpload</c>'s real
/// <c>&lt;input type="file"&gt;</c> at <c>opacity-0</c> with <c>tabindex="-1"</c> beneath a custom
/// drop zone, deliberately out of the tab order. Binding <c>Required</c> there would emit
/// <c>aria-required</c> on an element no keyboard or screen-reader user ever reaches — the
/// "forwarded but inert" failure this library's parity tests exist to catch — and it is measurably
/// harmful besides (see <c>MudBlazorFileUploadFieldComponent</c>). So the requirement is identified
/// where the user actually is, on two channels: visibly in the field's own <c>&lt;MudText&gt;</c>
/// label, and programmatically via <c>aria-describedby</c> on the <c>MudButton</c> that takes focus.
/// </para>
/// </remarks>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
/// <typeparam name="TValue">The field's value type — one file, or a list of them.</typeparam>
public abstract class MudBlazorFileUploadComponentBase<TModel, TValue> : FieldComponentBase<TModel, TValue>
{
    /// <summary>
    /// Per-render-instance discriminator for <see cref="RequiredDescriptionId"/>.
    /// </summary>
    /// <remarks>
    /// The field name alone is NOT unique in a document. Item fields render through these very
    /// components since #203, so a required upload inside <c>.WithItemForm(...)</c> emits one hint
    /// per row; two forms over the same model on one page collide the same way; and two nested
    /// fields can share a member name (<c>x =&gt; x.Passport.Scan</c> and <c>x =&gt; x.Visa.Scan</c>).
    /// Duplicate ids are invalid HTML and, worse, point every later button at the first row's
    /// description. MudBlazor solves this the same way, with a per-component identifier.
    /// </remarks>
    private readonly string _instanceDiscriminator = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Whether this field is marked as required, resolved by the same rule as every other field
    /// type: an explicit <c>.WithNativeRequired(...)</c> wins over <c>IsRequired</c> in both
    /// directions (#199).
    /// </summary>
    protected bool NativeRequiredValue =>
        NativeRequired.Resolve(Context.Field.AdditionalAttributes, IsRequired);

    /// <summary>
    /// Whether the component renders its own <c>&lt;MudText&gt;</c> label at all.
    /// </summary>
    /// <remarks>
    /// One predicate, used by both the label gate and <see cref="RequiredDescription"/>. They used
    /// to disagree — <c>IsNullOrEmpty</c> in the markup, <c>IsNullOrWhiteSpace</c> in the
    /// description — so a whitespace-only label rendered a bare asterisk with nothing beside it
    /// while the description simultaneously claimed the field had no label.
    /// </remarks>
    protected bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    /// <summary>
    /// The accessible name for the hidden <c>&lt;input type="file"&gt;</c>. Falls back rather than
    /// null-coalescing: a blank label is a configured value, so <c>Label ?? "File upload"</c> would
    /// leave the input with an empty accessible name instead of the fallback.
    /// </summary>
    protected string FileInputAccessibleName => HasLabel ? Label! : "File upload";

    /// <summary>
    /// The id of the requirement hint, unique per rendered field instance.
    /// </summary>
    protected string RequiredDescriptionId =>
        $"formcraft-{Context.Field.FieldName}-required-{_instanceDiscriminator}";

    /// <summary>
    /// The value for the focusable button's <c>aria-describedby</c>: the hint's id when the field is
    /// required, otherwise <c>null</c>, which makes Blazor omit the attribute altogether.
    /// </summary>
    protected string? RequiredDescribedBy => NativeRequiredValue ? RequiredDescriptionId : null;

    /// <summary>
    /// The hint text. Falls back to a label-free wording on purpose: the component renders its
    /// <c>&lt;MudText&gt;</c> label only when one is configured, so a field with a blank label has
    /// the button description as its only remaining channel.
    /// </summary>
    protected string RequiredDescription =>
        HasLabel ? $"{Label} is required." : "This file upload is required.";

    /// <summary>
    /// Tracks which field this instance's cached properties were loaded from (#298).
    /// </summary>
    /// <remarks>
    /// The upload components sit on their own base rather than
    /// <see cref="MudBlazorFieldComponentBase{TModel, TValue}"/>, but they cache configuration in
    /// exactly the same way — <c>Accept</c>, <c>MaxFileSize</c>, <c>UploadMode</c> and the rest, read
    /// once in <c>OnInitialized</c> — so they have the same staleness bug and need the same hook. The
    /// shared piece is the tracker; only this wiring is repeated.
    /// </remarks>
    private readonly FieldConfigurationTracker _fieldTracker = new();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RefreshFieldConfigurationIfChanged();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        RefreshFieldConfigurationIfChanged();
    }

    private void RefreshFieldConfigurationIfChanged()
    {
        if (_fieldTracker.HasChanged(Context?.Field))
        {
            OnFieldConfigurationChanged();
        }
    }

    /// <summary>
    /// Reads everything this component caches from <c>Context.Field</c>. Called once per field (#298).
    /// </summary>
    /// <remarks>
    /// Override this instead of loading configuration in <c>OnInitialized</c>, and assign every cached
    /// property on every call — including back to its default. The override is a reload, not a patch:
    /// a property left untouched because the new field does not declare that attribute keeps the
    /// <i>previous</i> field's value, which is the same bug in a smaller box.
    /// </remarks>
    protected virtual void OnFieldConfigurationChanged()
    {
    }

    /// <summary>
    /// The field's <b>Browse</b> button, captured by <c>@ref</c> in both upload components.
    /// </summary>
    /// <remarks>
    /// Shared here rather than declared twice for the reason the rest of this class exists: the
    /// single- and multiple-file components drifting apart is the failure class this library keeps
    /// re-filing (#146, #177, #184, #189).
    /// </remarks>
    protected MudButton? BrowseButton { get; set; }

    /// <summary>
    /// Moves keyboard focus to this field's <b>Browse</b> button (#281).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this is needed at all.</b> Both components gate their <b>Clear</b> button on an
    /// <c>@if</c> over the very value the button's own handler removes, so activating Clear unmounts
    /// the element the keyboard user is standing on and focus falls to <c>&lt;body&gt;</c> — the
    /// next <kbd>Tab</kbd> restarts from the top of the document. WCAG 2.1 <b>2.4.3 Focus Order</b>
    /// (Level A) expects focus to move in an order that preserves meaning and operability.
    /// </para>
    /// <para>
    /// <b>Why Browse specifically.</b> It is the affordance that resolves the state the user has
    /// just created, it sits where Clear used to be in the tab order, and it carries the
    /// <c>aria-describedby</c> requirement description from #262 — so focusing it announces the
    /// requirement at the exact moment clearing makes the field unsatisfied.
    /// </para>
    /// <para>
    /// <b>Failures are swallowed on purpose, and the catch list is deliberately wide.</b> The clear
    /// itself has already succeeded by the time this runs — the value is gone and the model is
    /// notified — so moving focus is the courtesy on top. Letting it throw would turn a working
    /// clear into an unhandled exception, which on Blazor Server tears down the circuit and takes
    /// every other field's state with it: far worse than the focus bug being fixed.
    /// </para>
    /// <para>
    /// ⛔ <b>Do not narrow this to the "obvious" cases.</b> The likely failure is
    /// <see cref="JSException"/> — <c>domWrapper.focus</c> raises <i>"Unable to focus an invalid
    /// element"</i> when the button has left the DOM, which is reachable here: assigning
    /// <c>CurrentValue</c> raises <c>OnValueChanged</c>, and a parent that hides the field
    /// (<c>WithVisibilityProvider</c>) or drops the collection row can unmount Browse before the
    /// awaited interop call reaches the browser. The rest cover a dropped circuit
    /// (<see cref="JSDisconnectedException"/>), the interop timeout
    /// (<see cref="OperationCanceledException"/>), a component disposed mid-clear
    /// (<see cref="ObjectDisposedException"/>), and interop issued before the client is connected —
    /// prerender/SSR — where <c>RemoteJSRuntime</c> raises
    /// <see cref="InvalidOperationException"/>. That last clause is broader than its one named
    /// cause and knowingly so; the null check below does <b>not</b> cover prerender, because
    /// component reference captures are assigned while the render batch is applied and so run on
    /// the server pass too.
    /// </para>
    /// </remarks>
    protected async Task FocusBrowseAsync()
    {
        if (BrowseButton is null)
        {
            return;
        }

        try
        {
            await BrowseButton.FocusAsync();
        }
        catch (JSException)
        {
            // The element is no longer focusable — typically gone from the DOM.
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone; there is nothing left to focus.
        }
        catch (OperationCanceledException)
        {
            // The interop call timed out or was cancelled.
        }
        catch (ObjectDisposedException)
        {
            // The component was torn down mid-clear.
        }
        catch (InvalidOperationException)
        {
            // No usable JS runtime behind the reference yet — the prerender/SSR pass.
        }
    }
}
