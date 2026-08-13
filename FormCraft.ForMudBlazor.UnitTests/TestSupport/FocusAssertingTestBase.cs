using Microsoft.JSInterop;

namespace FormCraft.ForMudBlazor.UnitTests.TestSupport;

/// <summary>
/// Base for suites that assert where keyboard focus went (#281, #318).
/// </summary>
/// <remarks>
/// <para>
/// <b>The technique, established by measurement rather than assumption.</b> bUnit models no real DOM
/// focus, so there is no "which element is focused" state to assert. What it does record is JS
/// interop. Probed against <b>bUnit 2.9.0 / MudBlazor 9.8.0</b>:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>MudBaseButton.FocusAsync()</c> resolves to <c>ElementReference.FocusAsync()</c> and records
///     exactly one invocation of <see cref="FocusIdentifier"/>, whose <c>Arguments[0]</c> is the
///     target <see cref="ElementReference"/> and whose <c>Arguments[1]</c> is <c>preventScroll</c>.
///   </description></item>
///   <item><description>
///     MudBlazor's buttons expose <b>no public</b> <see cref="ElementReference"/> — it lives in a
///     private <c>MudBaseButton._elementReference</c> — and bUnit renders
///     <c>blazor:elementReference</c> into the markup <b>empty</b>. So neither the component API nor
///     the DOM can say which button an id belongs to.
///   </description></item>
///   <item><description>
///     Therefore a button's id is learned <b>through the public API</b>:
///     <see cref="LearnElementIdAsync"/> focuses it deliberately and reads the id back off the
///     recording. The id survives a re-render that leaves the element in place, which is what makes
///     a before/after comparison sound.
///   </description></item>
/// </list>
/// <para>
/// ⛔ Do not "simplify" this by reflecting into <c>MudBaseButton._elementReference</c>. It is a
/// private field of a third-party library and would break on any MudBlazor patch;
/// <see cref="LearnElementIdAsync"/> gets the same answer from supported API.
/// </para>
/// <para>
/// Shared rather than copied per suite: the upload and collection suites need identical helpers, and
/// per-suite copies of shared test support are the pattern #305 exists to clean up.
/// </para>
/// </remarks>
public abstract class FocusAssertingTestBase : MudBlazorTestBase
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
    /// Learns a button's element id the only way the public API allows: focus it deliberately and
    /// read the id back off the recorded invocation. See the class remarks for why not reflection.
    /// </summary>
    /// <remarks>
    /// Takes <see cref="MudBaseButton"/> rather than <c>MudButton</c> so it covers the collection
    /// field's <c>MudIconButton</c> reorder and delete controls too.
    /// </remarks>
    protected async Task<string> LearnElementIdAsync<TComponent>(
        IRenderedComponent<TComponent> host,
        MudBaseButton button)
        where TComponent : IComponent
    {
        await host.InvokeAsync(async () => await button.FocusAsync());
        return LastFocusedElementId();
    }

    /// <summary>
    /// Makes the focus interop throw the way a real browser does when the target has left the DOM.
    /// </summary>
    /// <remarks>
    /// <c>MudBlazorTestBase</c> runs JSInterop in <c>Loose</c> mode, where focus always succeeds — so
    /// without this, no test exercises <c>FocusRestore</c>'s catch block at all. That gap is how a
    /// missing <c>catch (JSException)</c> shipped once under #281: the action worked in every test
    /// and still tore down the circuit in production.
    /// </remarks>
    protected void FailTheFocusInterop() =>
        JSInterop
            .SetupVoid(FocusIdentifier, _ => true)
            .SetException(new JSException("Unable to focus an invalid element."));
}
