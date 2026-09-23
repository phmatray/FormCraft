using Microsoft.JSInterop;

namespace FormCraft.ForFluentUI.UnitTests.TestSupport;

/// <summary>
/// Base for suites that assert where keyboard focus went (#337), mirroring
/// <c>FormCraft.ForMudBlazor.UnitTests.TestSupport.FocusAssertingTestBase</c> (#281, #318) for this
/// adapter's focus mechanisms.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two mechanisms, because Fluent has no single button-focus API (#337, #383).</b> Fluent UI
/// Blazor's <c>FluentButton</c> (the pinned v5 RC) has no focus API and no
/// <see cref="ElementReference"/> at all — confirmed by decompiling the installed assembly — so this
/// adapter's collection field uses two different routes depending on the target:
/// </para>
/// <list type="bullet">
///   <item><description>
///     The collection header and per-row header <b>fallbacks</b> are plain, non-interactive
///     <c>&lt;div&gt;</c> wrappers carrying their own <c>@ref</c> (unchanged since #337): see
///     <see cref="FocusCount"/> / <see cref="LastFocusedElementId"/>, which read the same
///     <see cref="ElementReference.FocusAsync()"/> interop MudBlazor's buttons use.
///   </description></item>
///   <item><description>
///     The four <c>FluentButton</c> controls themselves (Add, Remove, Move up, Move down) are
///     focused by DOM id through a small JS module (<c>collectionFocus.js</c>) since #383 — a
///     throwaway bUnit probe (Task 1 Step 1) found <c>FluentButton</c> renders as a native custom
///     element (<c>&lt;fluent-button&gt;</c>) with no static CSS class list, ruling out reproducing
///     it with a bare <c>&lt;button&gt;</c>, but confirming its <c>Id</c> parameter renders straight
///     through to the element's <c>id</c> attribute. See <see cref="FocusByIdCount"/> /
///     <see cref="LastFocusedElementIdViaModule"/>, and
///     <c>FluentUICollectionFieldComponent&lt;TModel, TItem&gt;</c>'s <c>internal</c>
///     <c>*TargetId</c> members (reachable from this test project through the adapter's
///     <c>InternalsVisibleTo</c>) for the exact id each control would be focused by.
///   </description></item>
/// </list>
/// <para>
/// <b>Measured against bUnit 2.9.0 / Fluent UI Blazor 5.0.0-rc.5-26219.1</b>, via throwaway probes
/// (#337 Task 2, #383 Task 1 Step 1, both deleted once their findings were confirmed):
/// </para>
/// <list type="bullet">
///   <item><description>
///     A plain HTML element's captured <see cref="ElementReference"/> (<c>@ref</c> on a
///     <c>&lt;div&gt;</c>, not a component) renders <c>blazor:elementreference</c> into bUnit's
///     markup <b>empty</b> — the same as MudBlazor's buttons. DOM inspection cannot name a target
///     here either; the internal-field route above is what makes it possible instead.
///   </description></item>
///   <item><description>
///     <c>ElementReference.FocusAsync()</c> on that captured reference records exactly one invocation
///     of <see cref="FocusIdentifier"/>, whose <c>Arguments[0]</c> is an <see cref="ElementReference"/>
///     with the <b>same <see cref="ElementReference.Id"/></b> as the one that was focused — so
///     comparing <see cref="LastFocusedElementId"/> against an internal field's <c>.Id</c> directly is
///     a sound before/after comparison, exactly as MudBlazor's learned id is.
///   </description></item>
///   <item><description>
///     Invoking a method on an <c>IJSObjectReference</c> returned by bUnit's Loose-mode "import"
///     auto-mock records that invocation into the same <c>JSInterop.Invocations</c> list as any other
///     call, with <c>Identifier</c> set to the invoked JS function name (<c>"focusById"</c>) and
///     <c>Arguments[0]</c> the plain <see cref="string"/> id passed to it — no module-specific
///     plumbing needed to observe it.
///   </description></item>
/// </list>
/// <para>
/// Shared rather than copied per suite, for the same reason as the MudBlazor original: every
/// collection-focus test needs identical helpers.
/// </para>
/// </remarks>
public abstract class FocusAssertingTestBase : FluentUITestBase
{
    /// <summary>
    /// The interop identifier <see cref="ElementReference.FocusAsync()"/> resolves to.
    /// </summary>
    protected const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

    /// <summary>The JS function <c>collectionFocus.js</c> exports (#383).</summary>
    protected const string FocusByIdIdentifier = "focusById";

    /// <summary>How many focus requests have been recorded so far.</summary>
    protected int FocusCount() => JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier);

    /// <summary>The <see cref="ElementReference.Id"/> of the most recent focus request.</summary>
    protected string LastFocusedElementId() =>
        ((ElementReference)JSInterop.Invocations
            .Last(i => i.Identifier == FocusIdentifier)
            .Arguments[0]!)
        .Id;

    /// <summary>
    /// How many <c>focusById</c> requests have been recorded so far (#383) — the mechanism the
    /// collection field's four <c>FluentButton</c> controls use, distinct from <see cref="FocusCount"/>.
    /// </summary>
    protected int FocusByIdCount() => JSInterop.Invocations.Count(i => i.Identifier == FocusByIdIdentifier);

    /// <summary>The DOM id argument of the most recent <c>focusById</c> request (#383).</summary>
    protected string LastFocusedElementIdViaModule() =>
        (string)JSInterop.Invocations.Last(i => i.Identifier == FocusByIdIdentifier).Arguments[0]!;

    /// <summary>
    /// Makes the focus interop throw the way a real browser does when the target has left the DOM.
    /// </summary>
    /// <remarks>
    /// <c>FluentUITestBase</c> runs JSInterop in <c>Loose</c> mode, where focus always succeeds — so
    /// without this, no test exercises <c>FocusRestore</c>'s catch block at all, the same gap
    /// documented on the MudBlazor original.
    /// </remarks>
    protected void FailTheFocusInterop() =>
        JSInterop
            .SetupVoid(FocusIdentifier, _ => true)
            .SetException(new JSException("Unable to focus an invalid element."));

    /// <summary>
    /// Makes the collection field's <c>focusById</c> JS call throw, mirroring
    /// <see cref="FailTheFocusInterop"/> for the mechanism #383 added. Without this, Loose-mode
    /// JSInterop lets every <c>import</c>/<c>focusById</c> call auto-succeed, so no test exercises
    /// <see cref="FormCraft.ForFluentUI.FocusRestore"/>'s catch block for this route at all.
    /// </summary>
    protected void FailTheFocusByIdInterop() =>
        JSInterop
            .SetupVoid(FocusByIdIdentifier, _ => true)
            .SetException(new JSException("Unable to focus an invalid element."));
}
