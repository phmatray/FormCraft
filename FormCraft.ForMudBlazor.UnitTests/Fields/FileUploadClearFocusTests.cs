namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests that clearing a file-upload field moves keyboard focus to that field's Browse button
/// (#281), rather than letting it fall to <c>&lt;body&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect.</b> Both upload components render Clear inside an <c>@if</c> gated on the very
/// value the button's own handler removes, so activating Clear unmounts the element the user is
/// standing on. Focus falls to the document body: the next <kbd>Tab</kbd> restarts from the top,
/// and #262's <c>aria-describedby</c> requirement description — which lives on Browse — goes
/// unheard at the exact moment the field becomes unsatisfied. WCAG 2.1 <b>2.4.3 Focus Order</b>
/// (Level A) is the criterion in play.
/// </para>
/// <para>
/// <b>The assertion technique, established by measurement rather than assumption (#281 Task 1).</b>
/// bUnit does not model real DOM focus, so there is no "which element is focused" state to assert.
/// What it does do is record JS interop. Probed against <b>bUnit 2.9.0 / MudBlazor 9.8.0</b>:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>MudButton.FocusAsync()</c> resolves to <c>ElementReference.FocusAsync()</c> and records
///     exactly one invocation of <c>Blazor._internal.domWrapper.focus</c>, whose
///     <c>Arguments[0]</c> is the target <see cref="ElementReference"/> and whose
///     <c>Arguments[1]</c> is the <c>preventScroll</c> flag.
///   </description></item>
///   <item><description>
///     <c>MudButton</c> exposes <b>no public</b> <see cref="ElementReference"/> — MudBlazor keeps it
///     in a private <c>MudBaseButton._elementReference</c> field — and bUnit renders
///     <c>blazor:elementReference</c> into the markup <b>empty</b>. So neither the component API nor
///     the DOM can say which button an id belongs to.
///   </description></item>
///   <item><description>
///     Therefore a button's id is learned <b>through the public API</b>: call
///     <c>FocusAsync()</c> on it deliberately and read the id back off the recorded invocation. The
///     id is stable across the clear re-render (measured: Blazor preserves the Browse
///     <c>&lt;button&gt;</c> element and the <c>MudButton</c> instance, so the reference does not
///     change when Clear unmounts beside it), which is what makes a before/after comparison sound.
///   </description></item>
/// </list>
/// <para>
/// ⛔ Do not "simplify" these tests by reflecting into <c>MudBaseButton._elementReference</c>. It is
/// a private field of a third-party library and would break on any MudBlazor patch;
/// <see cref="LearnElementIdAsync"/> gets the same answer from supported API.
/// </para>
/// </remarks>
public class FileUploadClearFocusTests : MudBlazorTestBase
{
    /// <summary>
    /// The interop identifier <see cref="ElementReference.FocusAsync()"/> resolves to. Pinned as a
    /// constant because every assertion below keys off it.
    /// </summary>
    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

    [Fact]
    public async Task Focusing_A_MudButton_Should_Record_The_Interop_Call_These_Tests_Assert_On()
    {
        // Arrange - the canary for the technique documented on this class. If MudBlazor or bUnit
        // ever change how a focus request surfaces, this fails first and explains why every other
        // test in this file went quiet, instead of leaving them silently unable to observe focus.
        var button = Render<MudButton>(parameters => parameters.AddChildContent("Browse"));

        // Act
        await button.InvokeAsync(async () => await button.Instance.FocusAsync());

        // Assert - one focus request, carrying an ElementReference and the preventScroll flag
        var invocation = JSInterop.Invocations.ShouldHaveSingleItem();
        invocation.Identifier.ShouldBe(FocusIdentifier);
        invocation.Arguments.Count.ShouldBe(2);
        invocation.Arguments[0].ShouldBeOfType<ElementReference>();
    }

    /// <summary>
    /// How many focus requests have been recorded so far.
    /// </summary>
    private int FocusCount() => JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier);

    /// <summary>
    /// The <see cref="ElementReference.Id"/> of the most recent focus request.
    /// </summary>
    private string LastFocusedElementId() =>
        ((ElementReference)JSInterop.Invocations
            .Last(i => i.Identifier == FocusIdentifier)
            .Arguments[0]!)
        .Id;

    /// <summary>
    /// Learns a button's element id the only way the public API allows: focus it deliberately and
    /// read the id back off the recorded invocation. See the class remarks for why reflection is
    /// not used instead.
    /// </summary>
    private async Task<string> LearnElementIdAsync<TComponent>(
        IRenderedComponent<TComponent> host,
        MudButton button)
        where TComponent : IComponent
    {
        await host.InvokeAsync(async () => await button.FocusAsync());
        return LastFocusedElementId();
    }
}
