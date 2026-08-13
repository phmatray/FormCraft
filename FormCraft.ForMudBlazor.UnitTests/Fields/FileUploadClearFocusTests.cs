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

    [Fact]
    public async Task Clearing_A_File_Upload_Should_Move_Focus_To_That_Fields_Browse_Button()
    {
        // Arrange - rendered through FormCraftComponent, the path a real application uses
        var model = new TestModel { Upload = new StubBrowserFile() };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var browseId = await LearnElementIdAsync(component, component.FindComponents<MudButton>()[0].Instance);
        var focusesBeforeClear = FocusCount();

        // Act - a file is present, so the toolbar carries Browse then Clear
        var buttons = component.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert - the Clear button really did unmount itself, which is what loses focus...
        component.FindAll(".mud-toolbar button").Count.ShouldBe(1);

        // ...so exactly one focus request must have been issued, and to THIS field's Browse button
        FocusCount().ShouldBe(focusesBeforeClear + 1);
        LastFocusedElementId().ShouldBe(browseId);
    }

    [Fact]
    public async Task Clearing_A_Standalone_Upload_Should_Focus_Browse_Without_Throwing()
    {
        // Arrange - standalone, with no cascaded EditContext. #262 found this to be the risky render
        // path (it is where MudBlazor's own RequiredError surfaced), and it is the one a bare
        // IFieldRendererService.RenderField produces, so the focus move has to hold here too.
        var model = new TestModel { Upload = new StubBrowserFile() };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IBrowserFile),
            CurrentValue = model.Upload,
        };

        var component = Render<MudBlazorFileUploadFieldComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));

        var browseId = await LearnElementIdAsync(component, component.FindComponents<MudButton>()[0].Instance);
        var focusesBeforeClear = FocusCount();

        // Act
        var buttons = component.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert - no throw, and focus still lands on Browse
        FocusCount().ShouldBe(focusesBeforeClear + 1);
        LastFocusedElementId().ShouldBe(browseId);
    }

    [Fact]
    public async Task Clearing_A_Multiple_File_Upload_Should_Move_Focus_To_That_Fields_Browse_Button()
    {
        // Arrange - the multiple-file component has the same defect for the same reason: "Clear All"
        // is gated on `CurrentValue?.Any() == true`, which its own handler falsifies. The two
        // components drifting apart is the failure class this library keeps re-filing (#146, #177,
        // #184, #189), so it is asserted here rather than assumed to follow from the shared base.
        var model = new TestModel { Uploads = new List<IBrowserFile> { new StubBrowserFile() } };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Uploads, f => f.WithLabel("Certificates").Required("A certificate is required"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var field = component.FindComponent<MudBlazorMultipleFileUploadComponent<TestModel>>();
        var browseId = await LearnElementIdAsync(component, field.FindComponents<MudButton>()[0].Instance);
        var focusesBeforeClear = FocusCount();

        // Act
        var buttons = field.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert - "Clear All" unmounted itself, and focus was moved rather than dropped
        field.FindAll(".mud-toolbar button").Count.ShouldBe(1);
        FocusCount().ShouldBe(focusesBeforeClear + 1);
        LastFocusedElementId().ShouldBe(browseId);
    }

    [Fact]
    public async Task Clearing_A_Standalone_Multiple_File_Upload_Should_Focus_Browse_Without_Throwing()
    {
        // Arrange - the standalone path, mirroring the single-file component's coverage exactly
        var model = new TestModel { Uploads = new List<IBrowserFile> { new StubBrowserFile() } };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Uploads, f => f.WithLabel("Certificates").Required("A certificate is required"))
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IReadOnlyList<IBrowserFile>),
            CurrentValue = model.Uploads,
        };

        var component = Render<MudBlazorMultipleFileUploadComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));

        var browseId = await LearnElementIdAsync(component, component.FindComponents<MudButton>()[0].Instance);
        var focusesBeforeClear = FocusCount();

        // Act
        var buttons = component.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert
        FocusCount().ShouldBe(focusesBeforeClear + 1);
        LastFocusedElementId().ShouldBe(browseId);
    }

    [Fact]
    public async Task Clearing_The_Second_Of_Two_Upload_Fields_Should_Focus_That_Fields_Own_Browse()
    {
        // Arrange - the edge case that a single-field test cannot catch: focus must land on the
        // CLEARED field's Browse button, not on the first one in the document. The per-instance
        // @ref is what makes this hold; a static or form-level reference would pass every other
        // test in this file and fail here.
        var model = new TestModel
        {
            Upload = new StubBrowserFile(),
            SecondUpload = new StubBrowserFile(),
        };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .AddField(x => x.SecondUpload, f => f.WithLabel("Visa scan").Required("A scan is required"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Scoped per field rather than by a flat index, so an unrelated button elsewhere in the
        // form (the submit button, another field) cannot silently shift what is being asserted.
        var fields = component.FindComponents<MudBlazorFileUploadFieldComponent<TestModel>>();
        fields.Count.ShouldBe(2);

        var firstBrowseId = await LearnElementIdAsync(component, fields[0].FindComponents<MudButton>()[0].Instance);
        var secondBrowseId = await LearnElementIdAsync(component, fields[1].FindComponents<MudButton>()[0].Instance);
        firstBrowseId.ShouldNotBe(secondBrowseId);
        var focusesBeforeClear = FocusCount();

        // Act - clear the SECOND field
        var secondFieldButtons = fields[1].FindAll(".mud-toolbar button");
        secondFieldButtons.Count.ShouldBe(2);
        await component.InvokeAsync(() => secondFieldButtons[1].Click());

        // Assert - focus went to the second field's Browse, and the first field was left alone
        FocusCount().ShouldBe(focusesBeforeClear + 1);
        LastFocusedElementId().ShouldBe(secondBrowseId);
        LastFocusedElementId().ShouldNotBe(firstBrowseId);
        fields[0].FindAll(".mud-toolbar button").Count.ShouldBe(2);
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

    private sealed class TestModel
    {
        public IBrowserFile? Upload { get; set; }

        /// <summary>
        /// Exactly <c>IReadOnlyList&lt;IBrowserFile&gt;</c> — that is what
        /// <c>MudBlazorMultipleFileUploadRenderer</c> matches on, so a different list type would
        /// silently render the single-file component instead.
        /// </summary>
        public IReadOnlyList<IBrowserFile>? Uploads { get; set; }

        public IBrowserFile? SecondUpload { get; set; }
    }

    private sealed class StubBrowserFile : IBrowserFile
    {
        public string Name => "passport.png";

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => 1024;

        public string ContentType => "image/png";

        public Stream OpenReadStream(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default) => new MemoryStream();
    }
}
