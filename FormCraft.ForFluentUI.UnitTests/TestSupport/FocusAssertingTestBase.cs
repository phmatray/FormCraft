using Microsoft.JSInterop;

namespace FormCraft.ForFluentUI.UnitTests.TestSupport;

/// <summary>
/// Base for suites that assert where keyboard focus went (#337), mirroring
/// <c>FormCraft.ForMudBlazor.UnitTests.TestSupport.FocusAssertingTestBase</c> (#281, #318) for this
/// adapter's different focus mechanism.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this technique differs from MudBlazor's.</b> The MudBlazor base learns a button's bUnit
/// element id by calling its public <c>FocusAsync()</c> and reading the id back off the recorded
/// interop invocation, because <c>MudBaseButton</c>'s own <see cref="ElementReference"/> is a private
/// field. Fluent UI Blazor's <c>FluentButton</c> (the pinned v5 RC) has no focus API and no
/// <see cref="ElementReference"/> at all — confirmed by decompiling the installed assembly — so the
/// Fluent collection field targets a plain wrapping element instead (see
/// <c>FormCraft.ForFluentUI.FocusRestore</c>). Those wrapper references are held in <c>internal</c>
/// fields on <c>FluentUICollectionFieldComponent&lt;TModel, TItem&gt;</c>, reachable from this test
/// project through the adapter's <c>InternalsVisibleTo</c> — so a test can read the exact
/// <see cref="ElementReference"/> the component would itself focus, with no "focus it first to learn
/// its id" indirection needed.
/// </para>
/// <para>
/// <b>Measured against bUnit 2.9.0 / Fluent UI Blazor 5.0.0-rc.5-26219.1</b>, via a throwaway probe
/// (Task 2, #337, deleted once these facts were confirmed):
/// </para>
/// <list type="bullet">
///   <item><description>
///     A plain HTML element's captured <see cref="ElementReference"/> (<c>@ref</c> on a
///     <c>&lt;span&gt;</c>, not a component) renders <c>blazor:elementreference</c> into bUnit's
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

    /// <summary>How many focus requests have been recorded so far.</summary>
    protected int FocusCount() => JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier);

    /// <summary>The <see cref="ElementReference.Id"/> of the most recent focus request.</summary>
    protected string LastFocusedElementId() =>
        ((ElementReference)JSInterop.Invocations
            .Last(i => i.Identifier == FocusIdentifier)
            .Arguments[0]!)
        .Id;

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
}
